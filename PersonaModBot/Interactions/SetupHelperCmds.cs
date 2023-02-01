using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EdgeDB;
using Microsoft.VisualBasic;
using PersonaModBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonaModBot.Interactions
{
    public class SetupHelperCmds : InteractionModuleBase
    {
        private readonly EdgeDBClient _db;
        private readonly InteractionHelper _helper;

        public SetupHelperCmds(EdgeDBClient db, InteractionHelper helper)
        {
            _db = db;
            _helper = helper;
        }

        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("setup-helper", "Setup stuff for forum helpers")]
        public async Task Setup()
        {
            List<SelectMenuOptionBuilder> options = new();
            var channels = await Context.Guild.GetChannelsAsync();
            foreach (var forum in channels.Where(channel => channel.GetChannelType() == ChannelType.Forum))
                options.Add(new SelectMenuOptionBuilder() { Label = forum.Name, Value = forum.Id.ToString() });


            var channelSelect = new SelectMenuBuilder()
            {
                CustomId = "setup-helper-forums",
                Options = options,
                MinValues = 1,
                MaxValues = options.Count,
            };

            var components = new ComponentBuilder().WithSelectMenu(channelSelect);
            await RespondAsync("Firstly please select the channels that you would like to setup for use with the solved, rename, and tag commands.\n" +
                "\nNote that all of these will be setup identically (same role access, same solved tag, etc), if you want different ones per channel set them up separately.",
                components: components.Build(), ephemeral: true);
        }

        [ComponentInteraction("setup-helper-forums")]
        public async Task SelectForums(string[] selectedForums)
        {
            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;

            if (_helper.SetupHelperForums.ContainsKey(interaction.Message.Id)) _helper.SetupHelperForums.Remove(interaction.Message.Id);
            _helper.SetupHelperForums.Add(interaction.Message.Id, selectedForums.Select(f => (ulong.Parse(f), (ulong)0)).ToArray());

            var channels = (await Context.Guild.GetChannelsAsync()).Where(x => selectedForums.Contains(x.Id.ToString()));

            List<SelectMenuOptionBuilder> options = new();
            foreach (var channel in channels)
            {
                foreach (var tag in ((IForumChannel)channel).Tags)
                {
                    if (!options.Any(x => x.Label == tag.Name))
                    {
                        string? emojiId = tag.Emoji?.Name;
                        if (tag.Emoji is Emote)
                            emojiId = ((Emote)tag.Emoji).Id.ToString();
                        options.Add(new SelectMenuOptionBuilder() { Label = tag.Name, Value = $"{tag.Name},{emojiId}", Emote = tag.Emoji });
                    }
                }
            }

            var tagSelect = new SelectMenuBuilder()
            {
                CustomId = $"setup-helper-solved-tag",
                Options = options,
            };

            await interaction.UpdateAsync(x =>
            {
                x.Content = "Please select the tag that will be assigned when a post is marked as solved.\n\n" +
                "If any selected forum does not have a tag with the same name a new one will be created with the shown name and emoji.";
                x.Components = new ComponentBuilder().WithSelectMenu(tagSelect).Build();
            });
        }

        [ComponentInteraction("setup-helper-solved-tag")]
        public async Task SetSolvedTag(string tagStr)
        {
            string tagName = tagStr.Substring(0, tagStr.LastIndexOf(','));
            string tagEmojiId = tagStr.Substring(tagStr.LastIndexOf(',') + 1);
            IEmote? tagEmoji = null;
            if (ulong.TryParse(tagEmojiId, out var emojiId))
                tagEmoji = await Context.Guild.GetEmoteAsync(emojiId);
            else
                tagEmoji = new Emoji(tagEmojiId);

            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;

            var forums = _helper.SetupHelperForums[interaction.Message.Id];
            for (int i = 0; i < forums.Length; i++)
            {
                IForumChannel forum = (IForumChannel)await Context.Guild.GetChannelAsync(forums[i].forum);
                if (!forum.Tags.Any(t => t.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var newTag = new ForumTagBuilder().WithName(tagName).WithEmoji(tagEmoji).Build();
                    IEnumerable<ForumTagProperties> tags = forum.Tags.Select(tag => tag.ToForumTagBuilder().Build()).Append(newTag);
                    await forum.ModifyAsync(f => f.Tags = new Discord.Optional<IEnumerable<ForumTagProperties>>(tags));
                }

                forums[i].solvedTag = forum.Tags.First(t => t.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase)).Id;
            }

            List<SelectMenuOptionBuilder> options = new();
            foreach (var role in Context.Guild.Roles)
                options.Add(new SelectMenuOptionBuilder() { Label = role.Name, Value = role.Id.ToString() });

            var roleSelect = new SelectMenuBuilder()
            {
                CustomId = $"setup-helper-roles",
                Options = options,
                MinValues = 1,
                MaxValues = options.Count,
            };

            await interaction.UpdateAsync(x =>
            {
                x.Content = $"Now please select the roles that you want to setup permissions for.\n" +
                $"\nNote that all selected roles will be given the same permissions. You will have a chance to setup roles again if you want to give different roles different permissions.";
                x.Components = new ComponentBuilder().WithSelectMenu(roleSelect).Build();
            });
        }

        [ComponentInteraction("setup-helper-roles")]
        public async Task SetupRoles(string[] selectedRoles)
        {
            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;

            if (_helper.SetupHelperRoles.ContainsKey(interaction.Message.Id))
                _helper.SetupHelperRoles[interaction.Message.Id] = _helper.SetupHelperRoles[interaction.Message.Id].Concat(selectedRoles.Select(role => new RoleConfig(ulong.Parse(role), false, false, false))).ToArray();
            else
                _helper.SetupHelperRoles.Add(interaction.Message.Id, selectedRoles.Select(role => new RoleConfig(ulong.Parse(role), false, false, false)).ToArray());

            var roles = Context.Guild.Roles.Where(role => selectedRoles.Contains(role.Id.ToString()));

            var selectedForums = _helper.SetupHelperForums[interaction.Message.Id];
            var channels = (await Context.Guild.GetChannelsAsync()).Where(x => selectedForums.Any(f => f.forum.Equals(x.Id)));

            var permSelect = new SelectMenuBuilder()
                .AddOption("Mark Solved", "solve")
                .AddOption("Edit Tags", "tag")
                .AddOption("Rename Post", "rename")
                .WithCustomId("setup-helper-perms")
                .WithMaxValues(3)
                .WithMinValues(1);

            await interaction.UpdateAsync(x =>
            {
                x.Content = $"Please select the permissions that you want to give to users with the role{(selectedRoles.Length > 1 ? 's' : "")} {string.Join(',', roles.Select(role => role.Name))} \n" +
                $"for {string.Join(',', channels.Select(channel => channel.Name))}.";
                x.Components = new ComponentBuilder().WithSelectMenu(permSelect).Build();
            });
        }

        [ComponentInteraction("setup-helper-perms")]
        public async Task SetupPerms(string[] permissions)
        {
            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;

            var selectedRoles = _helper.SetupHelperRoles[interaction.Message.Id];

            bool allowSolve = permissions.Contains("solve");
            bool allowTag = permissions.Contains("tag");
            bool allowRename = permissions.Contains("rename");
            foreach (var role in selectedRoles)
            {
                role.AllowTag = allowTag;
                role.AllowSolve = allowSolve;
                role.AllowRename = allowRename;
            }

            var buttons = new ComponentBuilder()
                .WithButton("I'm Done", "setup-helper-done")
                .WithButton("Setup More Roles", "setup-helper-solved-tag");

            await interaction.UpdateAsync(x =>
            {
                x.Content = $"Done seting up permissions. Do you want to setup permissions for any other roles?";
                x.Components = buttons.Build();
            });
        }

        [ComponentInteraction("setup-helper-done")]
        public async Task SetupDone()
        {
            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;

            var selectedForums = _helper.SetupHelperForums[interaction.Message.Id];
            var selectedRoles = _helper.SetupHelperRoles[interaction.Message.Id];

            var roleData = new List<Dictionary<string, object>>();
            foreach (var role in _helper.SetupHelperRoles[interaction.Message.Id])
                roleData.Add(new Dictionary<string, object> { { "roleId", role.RoleId }, { "allowSolve", role.AllowSolve }, { "allowTag", role.AllowTag }, { "allowRename", role.AllowRename } });

            var roleDataJson = new EdgeDB.DataTypes.Json(JsonSerializer.Serialize(roleData));

            await _db.EnsureConnectedAsync();

            foreach (var forum in selectedForums)
            {
                await _db.QueryAsync<ForumConfig>(
                        "with roles := (\n" +
                        "   for item in json_array_unpack(<json>$roleData) union (\n" +
                        "       insert RoleConfig {\n" +
                        "           roleId := <int64>item['roleId'],\n" +
                        "           allowSolve := <bool>item['allowSolve'],\n" +
                        "           allowTag := <bool>item['allowTag'],\n" +
                        "           allowRename := <bool>item['allowRename']\n" +
                        "       }\n" +
                        "   )\n" +
                        "),\n" +
                        "forumHelperConfig := (\n" +
                        "   insert ForumHelperConfig {\n" +
                        "       solvedTag := <int64>$solvedTag,\n" +
                        "       solvedMessage := <str>$solvedMessage,\n" +
                        "       allowedRoles := roles \n" +
                        "   }\n" +
                        ")\n" +
                        "insert ForumConfig {\n" +
                        "   forumId := <int64>$forumId,\n" +
                        "   helperConfig := forumHelperConfig\n" +
                        "}\n" +
                        "unless conflict on .forumId \n" +
                        "else(\n" +
                        "   update ForumConfig set { helperConfig := forumHelperConfig }\n" +
                        ")",
                        new Dictionary<string, object?>()
                        {
                        { "forumId", (long)forum.forum },
                        { "solvedTag", (long)forum.solvedTag },
                        { "solvedMessage", "This post has been marked as solved." },
                        { "roleData", roleDataJson },
                        });
            }

            await _db.QueryAsync<GuildConfig>(
                "with forums := (select ForumConfig filter contains(<array<int64>>$selectedForums, .forumId)) \n" +
                "insert GuildConfig {\n" +
                "   guildId := <int64>$guildId,\n" +
                "   forumConfigs := forums\n" +
                "}\n" +
                "unless conflict on .guildId\n" +
                "else(\n" +
                "   update GuildConfig set { forumConfigs := distinct (forums union .forumConfigs) }\n" +
                ")",
                new Dictionary<string, object?>()
                {
                    { "selectedForums", selectedForums.Select(x => (long)x.forum).ToArray() },
                    { "guildId", (long)Context.Guild.Id }
                });

            _helper.SetupHelperForums.Remove(interaction.Message.Id);

            await interaction.UpdateAsync(x =>
            {
                x.Content = "Done setting up channels. Have fun!";
                x.Components = null;
            });
        }

    }
}
