using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EdgeDB;
using PersonaModBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        [SlashCommand("solved", "Mark the current post as solved")]
        public async Task Solved()
        {
            SocketThreadChannel channel = (SocketThreadChannel)Context.Channel;
            var parent = channel.ParentChannel;
            if (parent.GetChannelType() != ChannelType.Forum || Context.Channel.GetChannelType() != ChannelType.PublicThread)
            {
                await RespondAsync("You can only solve posts in forums.", ephemeral: true);
                return;
            }

            SocketForumChannel forum = (SocketForumChannel)parent;

            var user = (IGuildUser)Context.User;

            var query = "select GuildConfig { guildId, forumConfigs: { forumId, solvedTag, solvedMessage, allowedRoles: { allowRename, allowSolve, allowTag, roleId } } } filter .guildId = <int64>$guildId";
            await _db.EnsureConnectedAsync();
            var configRes = await _db.QueryAsync<GuildConfig>(query, new Dictionary<string, object?>() { { "guildId", (long)Context.Guild.Id } }, Capabilities.All);
                
            if (configRes.Count == 0)
            {
                await RespondAsync("The server has not been configured yet. Please get an admin to do so with the `/setup` command");
                return;
            }
            
            GuildConfig guildConfig = configRes.First()!;
            ForumConfig? config = guildConfig.ForumConfigs.FirstOrDefault(x => x.ForumId == forum.Id);

            if(config == null)
            {
                await RespondAsync("This channel has not been configured yet. Please get an admin to do so with the `/setup` command");
                return;
            }

            if (channel.Owner.Id != Context.User.Id && !user.GetPermissions((IGuildChannel)Context.Channel).ManageThreads && !config.AllowedRoles.Any(role => user.RoleIds.Contains(role.RoleId) && role.AllowSolve))
            {
                await RespondAsync("You do not have permission to mark a post as solved.", ephemeral: true);
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

            await RespondAsync("This issue has been solved! (This is hopefully true)");
        }
    }
}
