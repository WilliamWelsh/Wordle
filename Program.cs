using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Wordle
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync(args.Length == 0 ? "" : args[0]).GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private InteractionService _commands;

        private IServiceProvider _services;

        public async Task MainAsync(string parameter)
        {
            // Create word of the day file if it doesn't exist
            if (!File.Exists("word_of_the_day.txt"))
                File.WriteAllText("word_of_the_day.txt", Words.WordList[new Random().Next(0, Words.WordList.Length)].ToUpper());

            // Create log file of user IDs that have played today already
            if (!File.Exists("played_today.json"))
                File.WriteAllText("played_today.json", "[]");

            // If this program is opened with this parameter, it will change the word of the day
            // and clear the list of user IDs that have played today
            if (parameter == "change")
            {
                File.WriteAllText("word_of_the_day.txt", Words.WordList[new Random().Next(0, Words.WordList.Length)].ToUpper());
                File.WriteAllText("played_today.json", "[]");
            }

            _services = ConfigureServices();

            _client = _services.GetRequiredService<DiscordSocketClient>();

            _commands = _services.GetRequiredService<InteractionService>();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            await _client.LoginAsync(TokenType.Bot, System.Environment.GetEnvironmentVariable("WordleBotToken"));
            await _client.StartAsync();

            _client.InteractionCreated += OnInteractionCreated;
            _client.Ready += OnReady;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;

            await Task.Delay(Timeout.Infinite);
        }

        private async Task UpdateBotStatus() => await _client.SetGameAsync($"/wordle on {_client.Guilds.Count} servers");

        private async Task OnInteractionCreated(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of
                // the InteractionModuleBase<T> module
                var ctx = new SocketInteractionContext(_client, interaction);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task OnLeftGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnJoinedGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnReady()
        {
            // Uncomment this to register commands (should only be run once, not everytime it starts)
            // Comment it out again after registering the commands
            // await _commands.RegisterCommandsGloballyAsync();

            await UpdateBotStatus();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>(x => new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    GatewayIntents = GatewayIntents.Guilds
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton(x => new GameManager(x.GetRequiredService<DiscordSocketClient>()))
                .BuildServiceProvider();
        }
    }
}