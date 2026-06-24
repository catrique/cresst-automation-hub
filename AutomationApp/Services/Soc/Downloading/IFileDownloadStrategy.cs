namespace AutomationApp.Services.Soc.Downloading;
using Microsoft.Playwright;

public interface IFileDownloadStrategy
{
    Task<(byte[] content, string suggestedFilename)> DownloadAsync(IPage page, ILocator triggerLocator);
}