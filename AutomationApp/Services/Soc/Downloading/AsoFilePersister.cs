namespace AutomationApp.Services.Soc.Downloading;

public class AsoFilePersister
{
    public string BuildDestinationFolder(string rawStartDate, string rawEndDate, string baseDownloadPath)
    {
        string start = FormatDateSegment(rawStartDate);
        string end   = FormatDateSegment(rawEndDate);
        string folderName = $"ASOS_{start}_a_{end}";
        string fullPath   = Path.Combine(Directory.GetCurrentDirectory(), baseDownloadPath, folderName);

        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public async Task SaveAsync(byte[] content, string filename, string destinationFolder)
    {
        string safeName = EnsurePdfExtension(filename);
        string fullPath = Path.Combine(destinationFolder, safeName);
        await File.WriteAllBytesAsync(fullPath, content);
    }

    private static string FormatDateSegment(string raw) =>
        raw.Length >= 5 ? raw[..5].Replace("/", "-") : "??";

    private static string EnsurePdfExtension(string filename) =>
        filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? filename
            : filename + ".pdf";
}