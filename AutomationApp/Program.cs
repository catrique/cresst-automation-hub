using AutomationApp.Configuration;
using AutomationApp.Commands;
using Microsoft.Extensions.Configuration;

namespace AutomationApp
{
    internal static class Program
    {
        public static AppSettings Settings { get; private set; } = new();
     
        static async Task<int> Main(string[] args)
        {
            try
            {

                var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

                IConfiguration configuration = builder.Build();

                Settings = configuration.Get<AppSettings>() ?? new AppSettings();

                var menuHandler = new MenuCommandHandler();
                return await menuHandler.HandleInteractiveMenuAsync();

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL] Crash na inicialização do App:\n{ex.Message}");
                Console.ResetColor();
                return 2;
            }
        }
    }
}