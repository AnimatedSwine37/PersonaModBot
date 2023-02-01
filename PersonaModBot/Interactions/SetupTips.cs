using Discord;
using Discord.Interactions;
using EdgeDB;
using Microsoft.VisualBasic;
using PersonaModBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot.Interactions
{
    public class SetupTips : InteractionModuleBase
    {
        private readonly EdgeDBClient _db;
        private readonly InteractionHelper _helper;
        private readonly DbUtils _dbUtils;

        public SetupTips(EdgeDBClient db, InteractionHelper helper, DbUtils dbUtils)
        {
            _db = db;
            _helper = helper;
            _dbUtils = dbUtils;
        }

        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("setup-tips", "Setup forum tips")]
        public async Task Setup()
        {
            List<SelectMenuOptionBuilder> options = new();
            var channels = await Context.Guild.GetChannelsAsync();
            foreach (var forum in channels.Where(channel => channel.GetChannelType() == ChannelType.Forum))
                options.Add(new SelectMenuOptionBuilder() { Label = forum.Name, Value = forum.Id.ToString() });


            var channelSelect = new SelectMenuBuilder()
            {
                CustomId = "setup-tips-forums",
                Options = options,
                MinValues = 1,
                MaxValues = options.Count,
            };

            var components = new ComponentBuilder().WithSelectMenu(channelSelect);
            await RespondAsync("Firstly please select the channels that you would like to setup to have tips automatically posted in.\n" +
                "\nNote that all of these will be setup identically, if you want different ones per channel set them up separately.",
                components: components.Build(), ephemeral: true);
        }

        [ComponentInteraction("setup-tips-forums")]
        public async Task SelectForums(string[] selectedForums)
        {
            IComponentInteraction interaction = (IComponentInteraction)Context.Interaction;

            _helper.SetupTips.Add(Context.User.Id, selectedForums.Select(f => ulong.Parse(f)).ToArray());

            var guildConfig = await _dbUtils.GetGuildConfig(Context.Guild.Id);
            string tipMessage = "";
            if(guildConfig != null)
            {
                var forumConfig = guildConfig.ForumConfigs.FirstOrDefault(x => x.ForumId == ulong.Parse(selectedForums[0]));
                if (forumConfig != null && forumConfig.TipConfig != null)
                    tipMessage = forumConfig.TipConfig.TipMessage;
            }

            var modal = new ModalBuilder()
                .WithTitle("Set Tip Message")
                .WithCustomId("setup-tips-message")
                .AddTextInput("Message", "message", style: TextInputStyle.Paragraph, placeholder: "The message to display when a post is created.", value: tipMessage, required: true);

            await RespondWithModalAsync(modal.Build());
        }

        [ModalInteraction("setup-tips-message")]
        public async Task SetMessage(TipMessageModal modal)
        {
            var selectedForums = _helper.SetupTips[Context.User.Id];
            
            foreach (var forum in selectedForums)
            {
                await _db.QueryAsync<ForumConfig>(
                        "with forumTipConfig := (\n" +
                        "   insert ForumTipConfig {\n" +
                        "       postTip := true,\n" +
                        "       tipMessage := <str>$tipMessage\n" +
                        "   }\n" +
                        ")\n" +
                        "insert ForumConfig {\n" +
                        "   forumId := <int64>$forumId,\n" +
                        "   tipConfig := forumTipConfig\n" +
                        "}\n" +
                        "unless conflict on .forumId \n" +
                        "else(\n" +
                        "   update ForumConfig set { tipConfig := forumTipConfig }\n" +
                        ")",
                        new Dictionary<string, object?>()
                        {
                        { "tipMessage", modal.Message },
                        { "forumId", (long)forum},
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
                    { "selectedForums", selectedForums.Select(x => (long)x).ToArray() },
                    { "guildId", (long)Context.Guild.Id }
                });

            _helper.SetupTips.Remove(Context.User.Id);
            await RespondAsync($"Done setting up forum tips. The chosen message will now automatically be sent whenever a post is made in one of those forums.", ephemeral: true);
        }
    }

    public class TipMessageModal : IModal
    {
        public string Title => "Set Message";

        [ModalTextInput("message")]
        public string Message { get; set; }
    }
}
