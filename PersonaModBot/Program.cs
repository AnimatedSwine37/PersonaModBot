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

            var logger = LoggerFactory.Create(config => config.AddConsole()
            .SetMinimumLevel(ToLogLevel(Environment.GetEnvironmentVariable("EDGEDB_LOG_LEVEL"))))
            .CreateLogger("EdgeDB");

            EdgeDBConnection dbConnection;

            var dsn = Environment.GetEnvironmentVariable("EDGEDB_DSN");
            if (dsn == null)
            {
                dbConnection = EdgeDBConnection.ResolveEdgeDBTOML();
                logger.LogDebug("Using EdgeDB toml file for connection as no EDGEDB_DSN was specified");
            }
            else
            {
                logger.LogDebug($"Using dsn: {dsn} for connection");
                dbConnection = EdgeDBConnection.FromDSN(dsn);
                dbConnection.TLSSecurity = TLSSecurityMode.Insecure;
            }

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

            var logLevel = ToLogLevel(Environment.GetEnvironmentVariable("BOT_LOG_LEVEL"));
            var logger = LoggerFactory.Create(config => { config.AddConsole(); config.SetMinimumLevel(logLevel); }).CreateLogger("Discord");

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
            switch (severity)
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

        private static LogLevel ToLogLevel(string? severity)
        {
            switch (severity)
            {
                case "CRITICAL": return LogLevel.Critical;
                case "ERROR": return LogLevel.Error;
                case "WARNING": return LogLevel.Warning;
                case "INFO": return LogLevel.Information;
                case "DEBUG": return LogLevel.Debug;
                case "TRACE": return LogLevel.Trace;
                default: return LogLevel.Warning;
            }
        }
    }
}