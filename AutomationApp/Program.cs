using AutomationApp.Configuration;
using AutomationApp.Commands;
using Microsoft.Extensions.Configuration;
using AutomationApp.Services.Betha;
using Microsoft.Extensions.DependencyInjection;

namespace AutomationApp
{
    internal static class Program
    {
        public static AppSettings Settings { get; private set; } = new();
        public static IServiceProvider? ServiceProvider { get; private set; }

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

                var serviceCollection = new ServiceCollection();

                serviceCollection.AddHttpClient<IBethaApiService, BethaApiService>(client =>
                {
                    var baseUrl = Settings.Betha?.Api?.BaseUrl ?? "https://api.rh.betha.cloud/rh/api/";
                    client.BaseAddress = new Uri(baseUrl);

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var authorization = Settings.Betha?.Api?.Authorization;
                    if (!string.IsNullOrEmpty(authorization))
                        client.DefaultRequestHeaders.Add("Authorization", authorization);

                    var userAccess = Settings.Betha?.Api?.UserAccess;
                    if (!string.IsNullOrEmpty(userAccess))
                        client.DefaultRequestHeaders.Add("User-Access", userAccess);

                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Origin", "https://rh.betha.cloud");
                });

                ServiceProvider = serviceCollection.BuildServiceProvider();

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