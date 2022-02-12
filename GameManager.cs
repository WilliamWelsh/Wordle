using System.Net.Http.Headers;
using System.Text;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Wordle
{
    public class GameManager
    {
        public List<Game> Games;

        private HttpClient _http;

        private DiscordSocketClient _client;

        private List<CompletedGame> _playersThatHaveAlreadyPlayedToday;

        private string WORD_OF_THE_DAY;

        public GameManager(DiscordSocketClient client)
        {
            Games = new List<Game>();

            _http = new HttpClient();

            _client = client;

            _playersThatHaveAlreadyPlayedToday = JsonConvert.DeserializeObject<List<CompletedGame>>(File.ReadAllText("played_today.json"));

            WORD_OF_THE_DAY = File.ReadAllText("word_of_the_day.txt");
        }

        /// <summary>
        /// Show the initial menu that shows you how to play
        /// </summary>
        public async Task ShowHowToPlay(SocketInteraction interaction)
        {
            if (_playersThatHaveAlreadyPlayedToday.Any(g => g.UserId == interaction.User.Id))
            {
                var user = (SocketGuildUser)interaction.User;

                await interaction.RespondAsync(embed: new EmbedBuilder()
                    .WithColor(Colors.DiscordGreen)
                    .WithDescription(_playersThatHaveAlreadyPlayedToday.Where(g => g.UserId == interaction.User.Id).First().Result)
                    .WithAuthor($"{user.Nickname ?? user.Username}'s Results for Today", _client.CurrentUser.GetAvatarUrl())
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .Build());
                return;
            }

            await interaction.RespondAsync(embed: new EmbedBuilder()
                .WithAuthor("How to Play", _client.CurrentUser.GetAvatarUrl())
                .WithImageUrl("https://cdn.discordapp.com/attachments/933774122155122819/938472695723622420/unknown.png")
                .WithColor(Colors.DiscordGreen)
                .Build(), ephemeral: true, components: new ComponentBuilder().WithButton("Start Game", "start-game", ButtonStyle.Secondary).Build());
        }

        /// <summary>
        /// "Start Game" button
        /// </summary>
        public async Task StartGame(SocketMessageComponent interaction)
        {
            await interaction.DeferAsync();

            var game = new Game(interaction.User, interaction, WORD_OF_THE_DAY);

            Games.Add(game);

            // Update the message
            await UpdateGame(game, interaction, "", Colors.DiscordGreen);
        }

        /// <summary>
        /// Letter button
        /// </summary>
        public async Task PlayLetter(SocketMessageComponent interaction, string letter)
        {
            await interaction.DeferAsync();

            var game = Games.Where(x => x.User.Id == interaction.User.Id).First();

            game.PlacedLetters.Add(new Letter(letter));
            game.CurrentGuessedWord += letter;

            // Update the message
            await UpdateGame(game, interaction, "", Colors.DiscordGreen);
        }

        /// <summary>
        /// "Enter" button
        /// </summary>
        public async Task PressEnter(SocketMessageComponent interaction)
        {
            await interaction.DeferAsync();

            var game = Games.Where(x => x.User.Id == interaction.User.Id).First();

            // Get the 5 letters and analzye them
            var letters = game.PlacedLetters.Skip(Math.Max(0, game.PlacedLetters.Count() - 5));

            var wordOfTheDay = WORD_OF_THE_DAY;

            var guessedWord = letters.Aggregate("", (current, letter) => current + letter.ActualLetter).ToLower();

            if (!Words.AllowedGuesses.Contains(guessedWord.ToLower()) && !Words.WordList.Contains(guessedWord.ToLower()))
            {
                await UpdateGame(game, interaction, "Your guess is not in the word list. Try again.", Colors.DiscordRed);
                return;
            }

            // This is to combat letters that appear twice
            var temp = new StringBuilder(wordOfTheDay);

            for (int i = 0; i < 5; i++)
            {
                var letter = letters.ElementAt(i);

                if (wordOfTheDay[i].ToString() == letter.ActualLetter)
                {
                    letter.UpdateLetter(LetterType.CorrectSpot);
                    game.Letters.Where(x => x.ActualLetter == letter.ActualLetter).First().UpdateLetter(LetterType.CorrectSpot);
                    temp[i] = '_';
                }
                else if (wordOfTheDay[i].ToString() != letter.ActualLetter && wordOfTheDay.Contains(letter.ActualLetter))
                {
                    // Check if any other correct appearances of this letter are here
                    // I'm sure this can look better lol
                    var otherCorrectAppearance = false;
                    if (wordOfTheDay.Count(c => (c.ToString() == letter.ActualLetter)) > 2)
                        for (int j = i; j < 5; j++)
                            if (letters.ElementAt(j).ActualLetter == temp[j].ToString())
                                otherCorrectAppearance = true;

                    if (otherCorrectAppearance)
                    {
                        if (temp.ToString().Count(c => c.ToString() == letter.ActualLetter) == 1)
                        {
                            letter.UpdateLetter(LetterType.NoSpot);
                            game.Letters.Where(x => x.ActualLetter == letter.ActualLetter).First().UpdateLetter(LetterType.NoSpot);
                        }
                        else
                        {
                            letter.UpdateLetter(LetterType.IncorrectSpot);
                            game.Letters.Where(x => x.ActualLetter == letter.ActualLetter).First().UpdateLetter(LetterType.IncorrectSpot);
                            temp[i] = '_';
                        }
                    }
                    else
                    {
                        letter.UpdateLetter(LetterType.IncorrectSpot);
                        game.Letters.Where(x => x.ActualLetter == letter.ActualLetter).First().UpdateLetter(LetterType.IncorrectSpot);

                        if (!WORD_OF_THE_DAY.Contains(temp[i]))
                            temp[i] = '_';
                    }
                }
                else
                {
                    letter.UpdateLetter(LetterType.NoSpot);
                    game.Letters.Where(x => x.ActualLetter == letter.ActualLetter).First().UpdateLetter(LetterType.NoSpot);
                }

                // This is to combat letters that appear twice
                wordOfTheDay = temp.ToString();
            }

            // The guessed the right word
            if (game.CurrentGuessedWord == WORD_OF_THE_DAY)
            {
                await CompleteGame(interaction, game, "You got it, awesome job 😄", false);
                return;
            }

            game.CurrentRow++;
            game.CurrentGuessedWord = "";

            // Game is over (ran out of rows)
            if (game.CurrentRow == 7)
            {
                await CompleteGame(interaction, game, $"You lost, better luck next time 😢\n\nThe word was **{WORD_OF_THE_DAY}**", true);
                return;
            }

            // Update the message
            await UpdateGame(game, interaction, "", Colors.DiscordGreen);
        }

        /// <summary>
        /// "Backspace" button
        /// </summary>
        public async Task PressBackspace(SocketMessageComponent interaction)
        {
            await interaction.DeferAsync();

            var game = Games.Where(x => x.User.Id == interaction.User.Id).First();

            game.PlacedLetters.RemoveAt(game.PlacedLetters.Count - 1);
            game.CurrentGuessedWord = game.CurrentGuessedWord.Remove(game.CurrentGuessedWord.Length - 1);

            // Update the message
            await UpdateGame(game, interaction, "", Colors.DiscordGreen);
        }

        /// <summary>
        /// "Cancel Game" button
        /// </summary>
        public async Task CancelGame(SocketMessageComponent interaction)
        {
            var game = Games.Where(x => x.User.Id == interaction.User.Id).First();

            await interaction.UpdateAsync(x =>
            {
                x.Components = null;
                x.Content = "You cancelled the game. Do `/wordle` if you want to play again.";
            });

            Games.Remove(game);
        }

        /// <summary>
        /// Remove the buttons, delete the game, display the results, and save the results
        /// </summary>
        private async Task CompleteGame(SocketInteraction interaction, Game game, string description, bool didTheyLose)
        {
            var result = new StringBuilder().AppendLine($"Discord Wordle {(didTheyLose ? "X" : game.CurrentRow)}/6").AppendLine();
            for (var i = 1; i < game.PlacedLetters.Count + 1; i++)
            {
                switch (game.PlacedLetters.ElementAt(i - 1).Type)
                {
                    case LetterType.CorrectSpot:
                        result.Append("🟩");
                        break;

                    case LetterType.IncorrectSpot:
                        result.Append("🟨");
                        break;

                    case LetterType.NoSpot:
                        result.Append("⬛");
                        break;
                }

                if (i % 5 == 0)
                    result.AppendLine();
            }

            await UpdateGame(game, interaction, $"{description}\n\n{result}", didTheyLose ? Colors.DiscordRed : Colors.DiscordGreen, removeButtons: true);

            _playersThatHaveAlreadyPlayedToday.Add(new CompletedGame(game.User.Id, result.ToString()));
            File.WriteAllText("played_today.json", JsonConvert.SerializeObject(_playersThatHaveAlreadyPlayedToday));
            Games.Remove(game);
        }

        /// <summary>
        /// Remove the old image and replace it with a new one
        /// </summary>
        public async Task UpdateGame(Game game, SocketInteraction interaction, string description, Color color, bool removeButtons = false)
        {
            // Make the Wordle chart
            var fileName = await game.MakeImageAndReturnFileName();

            // Convert buttons to this dynamic crap
            // Unfortunately at this time the library doesn't support replacing
            // attachments, and I think they're waiting until v10
            // to support (in v10 this method will be easier)
            game.UpdateButtonPermissions();
            var components = new dynamic[5];
            var currentRow = new dynamic[5];
            var row = 0;
            var count = 0;
            foreach (var letter in game.Letters)
            {
                currentRow[count] = new
                {
                    type = 2,
                    label = letter.ActualLetter,
                    custom_id = $"playletter-{letter.ActualLetter}",
                    row,
                    disabled = !game.CanIClickALetter ? true : false,
                    style = letter.ButtonColor
                };

                count++;
                if (count == 5)
                {
                    components[row] = new
                    {
                        type = 1,
                        components = currentRow
                    };
                    currentRow = new dynamic[5];
                    row++;
                    count = 0;
                }
            }

            // W, Y, Backspace, Enter, & Cancel games
            components[4] = new
            {
                type = 1,
                components = new[]
                {
                    new
                    {
                        type = 2,
                        label = "W",
                        custom_id = "playletter-W",
                        row = 4,
                        disabled = !game.CanIClickALetter ? true : false,
                        style = game.Letters.Where(l => l.ActualLetter == "W").First().ButtonColor
                    },
                    new
                    {
                        type = 2,
                        label = "Y",
                        custom_id = "playletter-Y",
                        row = 4,
                        disabled = !game.CanIClickALetter ? true : false,
                        style = game.Letters.Where(l => l.ActualLetter == "Y").First().ButtonColor
                    },
                    new
                    {
                        type = 2,
                        label = "Backspace",
                        custom_id = "backspace",
                        row = 4,
                        disabled = !game.CanIPressBackspace ? true : false,
                        style = 4
                    },
                    new
                    {
                        type = 2,
                        label = "Enter",
                        custom_id = "enter",
                        row = 4,
                        disabled = !game.CanIPressEnter ? true : false,
                        style = 3
                    },
                    new
                    {
                        type = 2,
                        label = "Cancel",
                        custom_id = "cancel",
                        row = 4,
                        disabled = false,
                        style = 4,
                    },
                }
            };

            // Replace the attachment on the message
            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new
                        {
                            name = "Wordle",
                            icon_url = _client.CurrentUser.GetAvatarUrl()
                        },
                        description = description,
                        color = Convert.ToInt32($"{color.R:X2}{color.G:X2}{color.B:X2}", 16).ToString(),
                        image = new {  url = $"attachment://{fileName}" }
                    }
                },
                attachments = new[] { new { id = "0" } },
                components = removeButtons ? new dynamic[0] : components
            };

            var requestContent = new MultipartFormDataContent
            {
                { new StringContent(JsonConvert.SerializeObject(payload)), "\"payload_json\""},
                { new StringContent(fileName), "\"files[0]\""},
            };

            using (var Stream = new MemoryStream(File.ReadAllBytes(fileName)))
            {
                var filePart = new StreamContent(Stream);
                filePart.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                filePart.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    FileName = fileName,
                    Name = "\"files[0]\""
                };

                requestContent.Add(filePart);

                await _http.PatchAsync($"https://discord.com/api/v9/webhooks/{_client.CurrentUser.Id}/{interaction.Token}/messages/@original", requestContent);
            }

            File.Delete(fileName);
        }
    }
}