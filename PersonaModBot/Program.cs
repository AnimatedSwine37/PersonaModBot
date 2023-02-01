using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using EdgeDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PersonaModBot.Interactions;
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
            var logger = LoggerFactory.Create(config => config.AddConsole().SetMinimumLevel(LogLevel.Trace)).CreateLogger("EdgeDB");

#if DEBUG
            var dbConnection = EdgeDBConnection.ResolveEdgeDBTOML();
#else
            var dsn = Environment.GetEnvironmentVariable("EDGEDB_DSN");
            if (dsn == null) dsn = "edgedb://";
            var dbConnection = EdgeDBConnection.FromDSN(dsn);
            dbConnection.TLSSecurity = TLSSecurityMode.Insecure;
#endif

            var collection = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<DbUtils>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<InteractionHelper>()
                .AddEdgeDB(dbConnection, config => config.Logger = logger);

            return collection.BuildServiceProvider();
        }

        async Task RunAsync(string[] args)
        {
            var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
            var interactionHandler = _serviceProvider.GetRequiredService<InteractionHandler>();

            var logger = LoggerFactory.Create(config => { config.AddConsole(); config.SetMinimumLevel(LogLevel.Trace); } ).CreateLogger("Discord");

            client.Log += async (msg) =>
            {
                await Task.CompletedTask;
                logger.Log(ToLogLevel(msg.Severity), msg.Message);
            };

            client.Ready += interactionHandler.InitialiseAsync;

            string token = "";
            if (Environment.GetEnvironmentVariable("BOT_TOKEN") != null)
            {
                token = Environment.GetEnvironmentVariable("BOT_TOKEN")!;
            }
            else
            {
                token = File.ReadAllText("token.txt");
            }
                        
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private static LogLevel ToLogLevel(LogSeverity severity)
        {
            switch(severity)
            {
                case LogSeverity.Critical: return LogLevel.Critical;
                case LogSeverity.Error: return LogLevel.Error;
                case LogSeverity.Warning: return LogLevel.Warning;
                case LogSeverity.Info: return LogLevel.Information;
                case LogSeverity.Debug: return LogLevel.Debug;
                case LogSeverity.Verbose: return LogLevel.Trace;
            }
            return LogLevel.Information;
        }
    }
}