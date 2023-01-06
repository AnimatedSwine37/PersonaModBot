using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PersonaModBot
{
    public class Program
    {
        private readonly IServiceProvider _serviceProvider;
        
        public Program()
        {
            _serviceProvider = CreateServices();
        }

        static void Main(string[] args)
            => new Program().RunAsync(args).GetAwaiter().GetResult();

        static IServiceProvider CreateServices()
        {
            var collection = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<InteractionHandler>();

            return collection.BuildServiceProvider();
        }

        async Task RunAsync(string[] args)
        {
            var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
            var interactionHandler = _serviceProvider.GetRequiredService<InteractionHandler>();

            client.Log += async (msg) =>
            {
                await Task.CompletedTask;
                Console.WriteLine(msg);
            };

            client.Ready += interactionHandler.InitialiseAsync;


            // TODO remove before committing!
            var token = File.ReadAllText("token.txt");
            
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }
    }
}