namespace Wordle
{
    public class CompletedGame
    {
        public ulong UserId { get; set; }
        public string Result { get; set; }

        public CompletedGame(ulong userId, string result)
        {
            UserId = userId;
            Result = result;
        }
    }
}