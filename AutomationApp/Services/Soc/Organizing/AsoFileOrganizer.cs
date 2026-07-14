using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutomationApp.Models.Soc;

namespace AutomationApp.Services.Soc.Organizing;

public class AsoFileOrganizer
{
    private static readonly Dictionary<string, string> TipoExameMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "admissional", "Admissional"        },
            { "periódico",   "Periódico"           },
            { "periodico",   "Periódico"           },
            { "retorno",     "Retorno ao Trabalho" },
            { "mudança",     "Mudança de Função"   },
            { "mudanca",     "Mudança de Função"   },
            { "demissional", "Demissional"         }
        };

    public string ResolveExamType(string rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType)) return "Outros";

        var lower = rawType.ToLower();
        foreach (var (key, value) in TipoExameMap)
            if (lower.Contains(key)) return value;

        return "Outros";
    }

    public string MoveToSubfolder(AsoData data, string rootFolder)
    {
        var subfolder = data.LeituraOk ? ResolveExamType(data.TipoExame) : "Erro";
        var destDir = Path.Combine(rootFolder, subfolder);
        Directory.CreateDirectory(destDir);

        var newFilename = BuildFilename(data);
        var destPath = ResolveUniqueFilePath(destDir, newFilename);

        File.Move(data.CaminhoPDF, destPath);
        return destPath;
    }


    private static string BuildFilename(AsoData data)
    {
        var nome = data.LeituraOk && !string.IsNullOrWhiteSpace(data.Funcionario)
            ? NormalizeName(data.Funcionario)
            : $"erro_leitura_{Path.GetFileNameWithoutExtension(data.CaminhoPDF)}";

        nome = TruncateName(nome, maxLength: 60);

        return $"{nome.ToUpper()}_{NormalizeDate(data.DataExame)}.pdf";
    }

    private static string TruncateName(string name, int maxLength) =>
        name.Length <= maxLength ? name : name[..maxLength].TrimEnd('_');

    private static string NormalizeName(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormKD);
        var clean = new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c)
                != UnicodeCategory.NonSpacingMark)
            .ToArray());

        clean = Regex.Replace(clean, @"[^a-zA-Z0-9\s]", "");

        foreach (var invalid in Path.GetInvalidFileNameChars())
            clean = clean.Replace(invalid.ToString(), "");

        return clean.ToLower().Trim().Replace(' ', '_');
    }

    private static string ResolveUniqueFilePath(string dir, string filename)
    {
        var path = Path.Combine(dir, filename);
        var noExt = Path.GetFileNameWithoutExtension(filename);
        var counter = 1;

        while (File.Exists(path))
            path = Path.Combine(dir, $"{noExt}_{counter++}.pdf");

        return path;
    }

    private static string NormalizeDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return DateTime.Now.ToString("dd-MM-yy");

        return DateTime.TryParseExact(dateStr, "dd/MM/yyyy",
                   null, DateTimeStyles.None, out var dt)
            ? dt.ToString("dd-MM-yy")
            : DateTime.Now.ToString("dd-MM-yy");
    }
}