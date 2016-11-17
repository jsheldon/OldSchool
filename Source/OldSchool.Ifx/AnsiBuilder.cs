using System;
using System.Text.RegularExpressions;

namespace OldSchool.Ifx
{
    public class AnsiBuilder
    {
        private const string ESC = "\x1B";
        private static readonly Regex m_AnsiRegEx = new Regex(@"\[\[(\w+)\.(\w+)\]\]", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        public static string SaveCursorPosition => ESC + "[s";

        public static string LoadCursorPosition => ESC + "[u";

        public static string ClearScreenAndHomeCursor => ESC + "[2J";

        public static string ClearToEol => ESC + "K";

        /// <summary>Gets the ANSI sequence to move the cursor up the specified number of lines.</summary>
        /// <param name="numLines">The number of lines to move the cursor up.</param>
        /// <returns>The ANSI sequence to move the cursor up the specified number of lines.</returns>
        public static string MoveCursorUp(int numLines)
        {
            return $"{ESC}[{numLines}A";
        }

        /// <summary>Gets the ANSI sequence to move the cursor down the specified number of lines.</summary>
        /// <param name="numberLines">The number of lines to move the cursor down.</param>
        /// <returns>The ANSI sequence to move the cursor down the specified number of lines.</returns>
        public static string MoveCursorDown(int numberLines)
        {
            return $"{ESC}[{numberLines}B";
        }

        /// <summary>Gets the ANSI sequence to move the cursor right the specified number of columns.</summary>
        /// <param name="numberColumns">The number of columns to move the cursor right.</param>
        /// <returns>The ANSI sequence to move the cursor right the specified number of columns.</returns>
        public static string MoveCursorRight(int numberColumns)
        {
            return $"{ESC}[{numberColumns}C";
        }

        /// <summary>Gets the ANSI sequence to move the cursor left the specified number of columns.</summary>
        /// <param name="numberColumns">The number of columns to move the cursor left.</param>
        /// <returns>The ANSI sequence to move the cursor left the specified number of columns.</returns>
        public static string MoveCursorLeft(int numberColumns)
        {
            return $"{ESC}[{numberColumns}D";
        }

        /// <summary>Gets the ANSI sequence to move the cursor to a new line.</summary>
        /// <returns>The ANSI sequence to move the cursor to a new line.</returns>
        public static string MoveCursorToNewLine()
        {
            return Environment.NewLine;
        }

        /// <summary>Gets the ANSI sequence to set the foreground to the specified color.</summary>
        /// <param name="foregroundColor">Which foreground color to set.</param>
        /// <returns>The ANSI sequence to set the foreground to the specified color.</returns>
        public static string SetForegroundColor(AnsiForegroundColor foregroundColor)
        {
            return $"{ESC}[{(int)foregroundColor}m";
        }

        /// <summary>Gets the ANSI sequence to set the background to the specified color.</summary>
        /// <param name="backgroundColor">Which background color to set.</param>
        /// <returns>The ANSI sequence to set the background to the specified color.</returns>
        public static string SetBackgroundColor(AnsiBackgroundColor backgroundColor)
        {
            return $"{ESC}[{(int)backgroundColor}m";
        }

        /// <summary>Gets the ANSI sequence to set all of the attribute, forground, and background colors.</summary>
        /// <param name="attribute">Which attribute to set.</param>
        /// <param name="foregroundColor">Which foreground color to set.</param>
        /// <param name="backgroundColor">Which background color to set.</param>
        /// <returns>The ANSI sequence to set all of the attribute, forground, and background colors.</returns>
        public static string SetTextAttributes(AnsiAttribute attribute, AnsiForegroundColor foregroundColor, AnsiBackgroundColor backgroundColor)
        {
            return $"{ESC}[{(int)attribute};{(int)foregroundColor};{(int)backgroundColor}m";
        }

        /// <summary>Gets the ANSI sequence to set the text attribute.</summary>
        /// <param name="attribute">Which attribute to set.</param>
        /// <returns>The ANSI sequence to set the text attribute.</returns>
        public static string SetTextAttributes(AnsiAttribute attribute)
        {
            return $"{ESC}[{(int)attribute}m";
        }

        /// <summary>Gets the ANSI sequence to set the text foreground and background colors.</summary>
        /// <param name="foregroundColor">Which foreground color to set.</param>
        /// <param name="backgroundColor">Which background color to set.</param>
        /// <returns>The ANSI sequence to set the text foreground and background colors.</returns>
        public static string SetTextAttributes(AnsiForegroundColor foregroundColor, AnsiBackgroundColor backgroundColor)
        {
            return $"{ESC}[{(int)foregroundColor};{(int)backgroundColor}m";
        }

        /// <summary>Gets the ANSI sequence to set the cursor to the specified coordinates.</summary>
        /// <param name="line">Which line to set the cursor on.</param>
        /// <param name="column">Which column to set the cursor on.</param>
        /// <returns>The ANSI sequence to set the cursor to the specified coordinates.</returns>
        public static string MoveCursorTo(int line, int column)
        {
            return $"{ESC}[{line};{column}H";
        }

        private static string GetForegroundColor(string color)
        {
            AnsiForegroundColor foregroundColor;
            if (!Enum.TryParse(color, true, out foregroundColor))
                return string.Empty;

            return $"{ESC}[{(int)foregroundColor}m";
        }

        private static string GetBackgroundColor(string color)
        {
            AnsiBackgroundColor backgroundColor;
            if (!Enum.TryParse(color, true, out backgroundColor))
                return string.Empty;

            return $"{ESC}[{(int)backgroundColor}m";
        }

        private static string AnsiEvaluator(Match match)
        {
            if (!match.Success)
                return string.Empty;

            if (match.Groups.Count != 3)
                return string.Empty;

            var key = match.Groups[1].Value.ToLower();
            var value = match.Groups[2].Value.ToLower();

            switch (key)
            {
                case "fg":
                    return GetForegroundColor(value);
                case "bg":
                    return GetBackgroundColor(value);
                case "action":
                    return GetAction(value);
                case "code":
                    return GetCode(value);
                case "attr":
                    return GetAttribute(value);
                default:
                    return string.Empty;
            }
        }

        private static string GetAttribute(string value)
        {
            AnsiAttribute attribute;
            if (!Enum.TryParse(value, true, out attribute))
                return string.Empty;

            return $"{ESC}[{(int)attribute}m";
        }

        private static string GetCode(string value)
        {
            switch (value.ToLower())
            {
                case "esc":
                    return ESC;
                default:
                    return string.Empty;
            }
        }

        private static string GetAction(string value)
        {
            switch (value.ToLower())
            {
                case "cls":
                    return ClearScreenAndHomeCursor;
                case "moveup":
                    return MoveCursorUp(1);
                default:
                    return string.Empty;
            }
        }

        public static string Parse(string value)
        {
            if (!m_AnsiRegEx.IsMatch(value))
                return value;

            return m_AnsiRegEx.Replace(value, AnsiEvaluator);
        }
    }

    public enum AnsiAttribute
    {
        Normal = 0,
        Bold = 1,
        Underline = 4,
        Blink = 5,
        Reverse = 7
    }

    // Background Colors              
    public enum AnsiBackgroundColor
    {
        Black = 40,
        Red = 41,
        Green = 42,
        Yellow = 43,
        Blue = 44,
        Magenta = 45,
        Cyan = 46,
        White = 47
    }

    public enum AnsiForegroundColor
    {
        Black = 30,
        Red = 31,
        Green = 32,
        Yellow = 33,
        Blue = 34,
        Magenta = 35,
        Cyan = 36,
        White = 37
    }
}