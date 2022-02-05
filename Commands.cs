using Discord.Interactions;
using Discord.WebSocket;

namespace Wordle
{
    public class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        public GameManager GameManager { get; set; }

        [SlashCommand("wordle", "Play wordle!")]
        public async Task ShowHowToPlay()
        {
            await GameManager.ShowHowToPlay(Context.Interaction);
        }

        [ComponentInteraction("start-game")]
        public async Task StartInitialGame() => await GameManager.StartGame((SocketMessageComponent)Context.Interaction);

        [ComponentInteraction("playletter-*")]
        public async Task PlayLetter(string letter) => await GameManager.PlayLetter((SocketMessageComponent)Context.Interaction, letter);

        [ComponentInteraction("enter")]
        public async Task Enter() => await GameManager.PressEnter((SocketMessageComponent)Context.Interaction);

        [ComponentInteraction("backspace")]
        public async Task Backspace() => await GameManager.PressBackspace((SocketMessageComponent)Context.Interaction);

        [ComponentInteraction("cancel")]
        public async Task Cancel() => await GameManager.CancelGame((SocketMessageComponent)Context.Interaction);
    }
}