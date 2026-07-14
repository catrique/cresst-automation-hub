using Spectre.Console;

namespace AutomationApp.Utils
{
    public static class MessageConsole
    {
        public static void Header(string title)
        {
            Console.Clear();
            AnsiConsole.Write(new Rule($"[cyan]{title}[/]").Justify(Justify.Left));
            Console.WriteLine();
        }

        public static void Clear()
        {
            Console.Clear();
        }

        public static void Error(string message, bool resetColor = true)
        {
            AnsiConsole.MarkupLine($"[red]❌ {message}[/]");
        }

        public static void Success(string message, bool resetColor = true)
        {
            AnsiConsole.MarkupLine($"[green]✔ {message}[/]");
        }

        public static void Warning(string message, bool resetColor = true)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ {message}[/]");
        }

        public static void Info(string message, bool resetColor = true)
        {
            AnsiConsole.MarkupLine($"[cyan]ℹ {message}[/]");
        }

        public static void Default(string message, bool resetColor = true)
        {
            AnsiConsole.MarkupLine(message);
        }
    }
}