using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PersonaModBot
{
    internal class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private readonly DbUtils _dbUtils;

        private List<ulong> _doneTips = new();

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services, DbUtils dbUtils)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _dbUtils = dbUtils;
        }

        public async Task InitialiseAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            if (IsDebug())
                // Id of the test guild can be provided from the Configuration object
                await _commands.RegisterCommandsToGuildAsync(1008753061335945257, true);
            else
                await _commands.RegisterCommandsGloballyAsync(true);


            _client.InteractionCreated += HandleInteraction;
            _client.ThreadCreated += HandleThreadCreated;
        }

        private async Task HandleThreadCreated(SocketThreadChannel channel)
        {
            if (channel.ParentChannel.GetChannelType() != ChannelType.Forum)
                return;

            var forum = (IForumChannel)channel.ParentChannel;
            var guildConfig = await _dbUtils.GetGuildConfig(forum.GuildId);
            var forumConfig = guildConfig?.ForumConfigs.FirstOrDefault(x => x.ForumId == forum.Id);
            if (forumConfig == null || forumConfig.TipConfig == null || _doneTips.Contains(channel.Id))
                return;

            await channel.SendMessageAsync(forumConfig.TipConfig.TipMessage);
            if (_doneTips.Count > 100) _doneTips.Clear(); // This is totall how memory management works :D
            _doneTips.Add(channel.Id);
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private bool IsDebug()
        {
#if DEBUG
            return false;
#else
            return true;
#endif
        }
    }
}
