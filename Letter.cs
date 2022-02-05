using SixLabors.ImageSharp.PixelFormats;

namespace Wordle
{
    public class Letter
    {
        public string ActualLetter { get; set; }

        public LetterType Type { get; set; }

        public Rgba32 Color { get; set; }

        public int ButtonColor { get; set; }

        public Letter(string letter)
        {
            ActualLetter = letter;
            Type = LetterType.NotTriedYet;
            Color = Colors.RgbaDarkGrey;
            ButtonColor = 2;
        }

        public void UpdateLetter(LetterType type)
        {
            Type = type;

            switch (Type)
            {
                case LetterType.CorrectSpot:
                    Color = Colors.RgbaGreen;
                    ButtonColor = 3; // Green
                    break;

                case LetterType.IncorrectSpot:
                    Color = Colors.RgbaYellow;
                    ButtonColor = 1; // Blurple
                    break;

                case LetterType.NoSpot:
                    Color = Colors.RgbaGrey;
                    ButtonColor = 4; // Red
                    break;

                default:
                    break;
            }
        }
    }
}