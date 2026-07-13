namespace AutomationApp.Utils
{
    public static class MessageConsole
    {
        private static void Write(
            string message,
            ConsoleColor color,
            bool resetColor = true)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);

            if (resetColor)
                Console.ResetColor();
        }

        public static void Error(string message, bool resetColor = true)
            => Write(message, ConsoleColor.Red, resetColor);

        public static void Success(string message, bool resetColor = true)
            => Write(message, ConsoleColor.Green, resetColor);

        public static void Warning(string message, bool resetColor = true)
            => Write(message, ConsoleColor.Yellow, resetColor);

        public static void Info(string message, bool resetColor = true)
            => Write(message, ConsoleColor.Cyan, resetColor);

        public static void Default(string message, bool resetColor = true)
            => Write(message, ConsoleColor.Gray, resetColor);
    }
}