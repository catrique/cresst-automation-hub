using System.Text;
using Microsoft.Playwright;

namespace AutomationApp.Services.Soc.Downloading;

public class BlobNewTabDownloadStrategy : IFileDownloadStrategy
{
    private const int WaitAfterClickMs = 3000;

    public async Task<(byte[] content, string suggestedFilename)> DownloadAsync(
        IPage page, ILocator triggerLocator)
    {
        var newTabTask = page.Context.WaitForPageAsync();

        await triggerLocator.ClickAsync();

        var newTab = await newTabTask;
        await newTab.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(WaitAfterClickMs); 
        string title = await newTab.TitleAsync();
        string filename = ExtractFilename(newTab.Url, title);

        string base64 = await ExtractBlobAsBase64Async(newTab);

        await newTab.CloseAsync();

        byte[] bytes = Convert.FromBase64String(base64);
        return (bytes, filename);
    }

    private static string ExtractFilename(string url, string title)
    {
        if (!string.IsNullOrWhiteSpace(title) && title.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return title;

        return $"ASO_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
    }

    private static async Task<string> ExtractBlobAsBase64Async(IPage tab)
    {
        string blobUrl = await tab.EvaluateAsync<string>(@"
            () => {
                const embed = document.querySelector('embed[src^=""blob:""]');
                if (embed) return embed.src;
                const obj = document.querySelector('object[data^=""blob:""]');
                if (obj) return obj.data;
                // Fallback: a própria URL da página é o blob
                return window.location.href.startsWith('blob:') ? window.location.href : null;
            }
        ");

        if (string.IsNullOrEmpty(blobUrl))
            throw new InvalidOperationException("Blob URL not found on the new tab.");

        string base64 = await tab.EvaluateAsync<string>(@"
            async (blobUrl) => {
                const response = await fetch(blobUrl);
                const buffer = await response.arrayBuffer();
                const bytes = new Uint8Array(buffer);
                let binary = '';
                for (let i = 0; i < bytes.length; i++) {
                    binary += String.fromCharCode(bytes[i]);
                }
                return window.btoa(binary);
            }
        ", blobUrl);

        return base64;
    }
}