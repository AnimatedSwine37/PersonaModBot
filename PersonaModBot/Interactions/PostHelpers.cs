using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EdgeDB;
using PersonaModBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonaModBot.Interactions
{
    public class PostHelpers : InteractionModuleBase
    {
        private readonly EdgeDBClient _db;

        public PostHelpers(EdgeDBClient db)
        {
            _db = db;
        }

        private async Task<(bool IsValid, SocketForumChannel? Forum, ForumConfig? Config)> ValidatePermissions(IMessageChannel channel, IUser user, IGuild guild, string action)
        {
            if(channel.GetChannelType() != ChannelType.PublicThread)
            {
                await RespondAsync($"You can only {action} in forums.", ephemeral: true);
                return (false, null, null);
            }
            
            SocketThreadChannel thread = (SocketThreadChannel)channel;
            var parent = thread.ParentChannel;
            if (parent.GetChannelType() != ChannelType.Forum || channel.GetChannelType() != ChannelType.PublicThread)
            {
                await RespondAsync($"You can only {action} in forums.", ephemeral: true);
                return (false, null, null);
            }

            SocketForumChannel forum = (SocketForumChannel)parent;

            var guildUser = (IGuildUser)user;

            var query = "select GuildConfig { guildId, forumConfigs: { forumId, solvedTag, solvedMessage, allowedRoles: { allowRename, allowSolve, allowTag, roleId } } } filter .guildId = <int64>$guildId";
            await _db.EnsureConnectedAsync();
            var configRes = await _db.QueryAsync<GuildConfig>(query, new Dictionary<string, object?>() { { "guildId", (long)guild.Id } }, Capabilities.All);

            if (configRes.Count == 0)
            {
                await RespondAsync("The server has not been configured yet. Please get an admin to do so with the `/setup` command");
                return (false, null, null);
            }

            GuildConfig guildConfig = configRes.First()!;
            ForumConfig? config = guildConfig.ForumConfigs.FirstOrDefault(x => x.ForumId == forum.Id);

            if (config == null)
            {
                await RespondAsync("This forum has not been configured yet. Please get an admin to do so with the `/setup` command");
                return (false, null, null);
            }
            return (true, forum, config);
        }

        [SlashCommand("solved", "Mark the current post as solved")]
        public async Task Solved()
        {
            var res = await ValidatePermissions(Context.Channel, Context.User, Context.Guild, "solve posts");
            if (!res.IsValid)
                return;
            
            IThreadChannel channel = (IThreadChannel)Context.Channel;
            ForumConfig config = res.Config!;
            IGuildUser user = (IGuildUser)Context.User;
            
            if (channel.OwnerId != Context.User.Id && !user.GetPermissions(channel).ManageThreads && !config.AllowedRoles.Any(role => user.RoleIds.Contains(role.RoleId) && role.AllowSolve))
            {
                await RespondAsync("You do not have permission to mark this post as solved.", ephemeral: true);
                return;
            }

            var currentTags = channel.AppliedTags;

            if(currentTags.Contains(config.SolvedTag))
            {
                await RespondAsync("This post is already tagged as solved.", ephemeral: true);
                return;
            }

            await channel.ModifyAsync(x =>
            {
                    x.AppliedTags = Discord.Optional.Create(currentTags.Append(config.SolvedTag));
            });

            await RespondAsync(config.SolvedMessage);
        }

        [SlashCommand("tag", "Edit the tags of the current post")]
        public async Task Tag()
        {
            var res = await ValidatePermissions(Context.Channel, Context.User, Context.Guild, "edit the tags of posts");
            if (!res.IsValid)
                return;

            IThreadChannel channel = (IThreadChannel)Context.Channel;
            ForumConfig config = res.Config!;
            IGuildUser user = (IGuildUser)Context.User;
            IForumChannel forum = res.Forum!;

            if (channel.OwnerId != Context.User.Id && !user.GetPermissions(channel).ManageThreads && !config.AllowedRoles.Any(role => user.RoleIds.Contains(role.RoleId) && role.AllowTag))
            {
                await RespondAsync("You do not have permission to change the tags of this post.", ephemeral: true);
                return;
            }

            List<SelectMenuOptionBuilder> options = forum.Tags.Select(tag => new SelectMenuOptionBuilder(tag.Name, tag.Id.ToString(), emote: tag.Emoji, isDefault: channel.AppliedTags.Contains(tag.Id))).ToList();
            var tagSelect = new SelectMenuBuilder()
            {
                CustomId = "tag-selected",
                Options = options,
                MinValues = 0,
                MaxValues = options.Count,
            };

            await RespondAsync("Please select the tags that you want the post to have applied.", ephemeral: true, components: new ComponentBuilder().WithSelectMenu(tagSelect).Build());
        }

        [ComponentInteraction("tag-selected")]
        public async Task TagSelected(string[] selectedTags)
        {
            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;
            IThreadChannel channel = (IThreadChannel)Context.Channel;
            IEnumerable<ulong> tags = selectedTags.Select(tag => ulong.Parse(tag));
            
            await channel.ModifyAsync(x => x.AppliedTags = new Discord.Optional<IEnumerable<ulong>>(tags));

            await interaction.UpdateAsync(x =>
            {
                x.Content = $"Applied tag changes.";
                x.Components = null;
            });

            IForumChannel forum = (IForumChannel)((SocketThreadChannel)channel).ParentChannel;
            
            await channel.SendMessageAsync($"{Context.User.Mention} changed the applied tags to {(selectedTags.Length == 0 ? "none" : string.Join(", ", forum.Tags.Where(t => tags.Contains(t.Id)).Select(t => t.Name)))}.");
        }

        [SlashCommand("rename", "Rename the current post")]
        public async Task Rename()
        {
            var res = await ValidatePermissions(Context.Channel, Context.User, Context.Guild, "rename posts");
            if (!res.IsValid)
                return;

            IThreadChannel channel = (IThreadChannel)Context.Channel;
            ForumConfig config = res.Config!;
            IGuildUser user = (IGuildUser)Context.User;
            IForumChannel forum = res.Forum!;

            if (channel.OwnerId != Context.User.Id && !user.GetPermissions(channel).ManageThreads && !config.AllowedRoles.Any(role => user.RoleIds.Contains(role.RoleId) && role.AllowRename))
            {
                await RespondAsync("You do not have permission to rename this post.", ephemeral: true);
                return;
            }

            var modal = new ModalBuilder()
                .WithTitle($"Rename {channel.Name}")
                .WithCustomId("rename-modal")
                .AddTextInput("New Name", "new-name", placeholder: "Post Name", minLength: 2, maxLength: 100, required: true, value: channel.Name);

            await RespondWithModalAsync(modal.Build());
        }

        [ModalInteraction("rename-modal")]
        public async Task RenameModal(RenameModal modal)
        {
            string newName = modal.NewName;
            IThreadChannel channel = (IThreadChannel)Context.Channel;
            string oldName = channel.Name;

            await channel.ModifyAsync(x => x.Name = newName);   
            await RespondAsync($"Renamed post to {newName}", ephemeral:true);
            await channel.SendMessageAsync($"{Context.User.Mention} changed the post's title from \"{oldName}\" to \"{newName}\".");
        }
    }
    
    public class RenameModal : IModal
    {
        public string Title => "Rename";
        
        [ModalTextInput("new-name")]        
        public string NewName { get; set; }
    }
}
