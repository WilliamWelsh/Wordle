using Discord.WebSocket;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Wordle
{
    public class Game
    {
        public SocketUser User { get; set; }

        public List<Letter> Letters { get; set; }

        public List<Letter> PlacedLetters { get; set; }

        public SocketUserMessage GameMessage { get; set; }

        public string WORD_OF_THE_DAY { get; set; }

        // Image Stuff
        private Font _font;
        private RendererOptions _rendererOptions;
        private DrawingOptions _drawingOptions;

        // Button Stuff
        public bool CanIClickALetter = true;
        public bool CanIPressEnter = false;
        public bool CanIPressBackspace = false;

        public int CurrentRow = 1;

        public string CurrentGuessedWord = "";

        public Game(SocketUser user, SocketMessageComponent interaction, string wordOfTheDay)
        {
            User = user;

            Letters = new List<Letter>();

            PlacedLetters = new List<Letter>();

            // No Q, Z, X, or V to make room for the other buttons
            // (Discord has a max of 25 buttons)
            foreach (var letter in "ABCDEFGHIJKLMNOPRSTUWY")
                Letters.Add(new Letter(letter.ToString()));

            GameMessage = interaction.Message;

            WORD_OF_THE_DAY = wordOfTheDay;

            // Initialize ImageSharp stuff
            _font = SystemFonts.CreateFont("Arial", 32, FontStyle.Bold);
            _rendererOptions = new RendererOptions(_font);
            _drawingOptions = new DrawingOptions()
            {
                TextOptions = new TextOptions()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        /// <summary>
        /// Update the ability to click on letters, enter, or backspace
        /// </summary>
        public void UpdateButtonPermissions()
        {
            // Letter Buttons
            if (PlacedLetters.Count % 5 == 0 && PlacedLetters.Count / 5 == CurrentRow)
                CanIClickALetter = false;
            else
                CanIClickALetter = true;

            // Enter Button
            if (PlacedLetters.Count % 5 == 0 && PlacedLetters.Count / 5 == CurrentRow)
                CanIPressEnter = true;
            else
                CanIPressEnter = false;

            // Backspace Button
            CanIPressBackspace = CurrentGuessedWord != "";
        }

        /// <summary>
        /// Render the image, and then return the name of the file
        /// </summary>
        public async Task<string> MakeImageAndReturnFileName()
        {
            using (var image = new Image<Rgba32>(353, 423, Colors.RgbaDarkGrey))
            {
                var index = 0;

                var letterSize = TextMeasurer.Measure("A", _rendererOptions);

                // Draw the squares (62x62)
                for (int column = 0; column < 6; column++)
                {
                    var columnModifier = 70 * column;

                    for (int row = 0; row < 5; row++)
                    {
                        var rowModifier = 70 * row;

                        var box = new Rectangle(5 + rowModifier, 5 + columnModifier, 62, 62);

                        // Draw the box
                        image.Mutate(x => x.DrawPolygon(new Pen(Colors.RgbaLightGrey, 2), new PointF[]
                        {
                            // Top left
                            new PointF(5 + rowModifier, 5 + columnModifier),

                            // Bottom left
                            new PointF(5 + rowModifier, 67 + columnModifier),

                            // Bottom right
                            new PointF(67 + rowModifier, 67 + columnModifier),

                            // Top right
                            new PointF(67 + rowModifier, 5 + columnModifier),
                        }));

                        // Draw a letter if it's available
                        if (PlacedLetters.Count >= (index + 1))
                        {
                            // Background color
                            image.Mutate(x => x.Fill(PlacedLetters[index].Color, box));

                            // Draw the letter
                            letterSize = TextMeasurer.Measure(PlacedLetters[index].ActualLetter, _rendererOptions);
                            image.Mutate(x => x.DrawText(options: _drawingOptions, PlacedLetters[index].ActualLetter, _font, Colors.RgbaWhite, new PointF(5 + rowModifier + (62 / 2), 5 + columnModifier + (62 / 2))));
                        }

                        index++;
                    }
                }

                image.Save($"wordle-{User.Id}.png");
            }

            return $"wordle-{User.Id}.png";
        }
    }
}