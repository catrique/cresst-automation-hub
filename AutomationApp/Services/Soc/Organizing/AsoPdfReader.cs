using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutomationApp.Models.Soc;
using UglyToad.PdfPig;

namespace AutomationApp.Services.Soc.Organizing;


public class AsoPdfReader
{
    private static readonly string[] FieldStopWords =
    [
        "Cargo", "CPF", "Idade", "Matr[íi]cula", "Nascimento",
        "RG", "Risco", "Setor", "Sexo"
    ];

    public AsoData Extract(string pdfPath)
    {
        try
        {
            var text = ReadAllText(pdfPath);

            if (string.IsNullOrWhiteSpace(text))
                return FailedRead(pdfPath);

            var data = new AsoData
            {
                Funcionario = ExtractFuncionario(text) ?? "",
                Cpf = ExtractCpf(text) ?? "",
                Matricula = ExtractMatricula(text) ?? "",
                Cargo = ExtractCargo(text) ?? "",
                TipoExame = ExtractTipoExame(text) ?? "",
                Resultado = ExtractResultado(text) ?? "APTO",
                DataExame = ExtractDataExame(text) ?? "",
                MedicoExaminador = ExtractMedicoExaminador(text) ?? "",
                MedicoPcmso = ExtractMedicoPcmso(text) ?? "",
                CaminhoPDF = pdfPath,
                LeituraOk = true
            };

            if (string.IsNullOrWhiteSpace(data.Funcionario))
                return FailedRead(pdfPath);

            return data;
        }
        catch
        {
            return FailedRead(pdfPath);
        }
    }


    private static string ReadAllText(string path)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(path);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return Clean(sb.ToString());
    }

    private static AsoData FailedRead(string path) =>
        new() { CaminhoPDF = path, LeituraOk = false };

    private static string Clean(string text) =>
        Regex.Replace(
            text.Replace("\t", " ").Replace("\r", " "),
            @" +", " "
        ).Trim();

    private static string BuildStopWordsLookahead() =>
        $@"(?=\s*(?:{string.Join("|", FieldStopWords)})\b)";

 
    private static string? ExtractFuncionario(string text)
    {
        var pattern = $@"Nome[:\s]+\n?\s*([\s\S]+?){BuildStopWordsLookahead()}";
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return null;

        var nome = Clean(m.Groups[1].Value);

        var digitIndex = nome.IndexOfAny("0123456789".ToCharArray());
        if (digitIndex > 0)
            nome = nome[..digitIndex].Trim();

        return IsValidPersonName(nome) ? nome.ToUpper() : null;
    }

    private static string? ExtractCpf(string text)
    {
        string[] patterns =
        [
            @"CPF[:\s]*([\d]{3}\.[\d]{3}\.[\d]{3}-[\d]{2})",
            @"CPF[:\s]*([\d]{11})",
            @"([\d]{3}\.[\d]{3}\.[\d]{3}-[\d]{2})"
        ];

        foreach (var p in patterns)
        {
            var m = Regex.Match(text, p);
            if (!m.Success) continue;

            var raw = Regex.Replace(m.Groups[1].Value, @"\D", "");
            if (raw.Length == 11)
                return $"{raw[..3]}.{raw[3..6]}.{raw[6..9]}-{raw[9..11]}";
        }
        return null;
    }

    private static string? ExtractMatricula(string text)
    {
        var m = Regex.Match(text,
            @"Matr[íi]cula\s*eSocial[:\s]*([A-Za-z0-9\-\/]+)",
            RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.Trim();

        m = Regex.Match(text,
            @"Matr[íi]cula[:\s]*([A-Za-z0-9\-\/]+)",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractCargo(string text)
    {
        var m = Regex.Match(text,
            @"Cargo[:\s]*([\s\S]+?)(?=\s*Setor\b)",
            RegexOptions.IgnoreCase);

        if (!m.Success) return null;

        var cargo = Clean(m.Groups[1].Value);
        return cargo.Length is > 3 and < 300 ? cargo.ToUpper() : null;
    }

    private static string? ExtractTipoExame(string text)
    {
        var m = Regex.Match(text,
            @"TIPO\s+DE\s+EXAME[\s\n]*([A-ZÀ-Úa-zà-ú\s]+?)(?=\s*(?:AVALIAÇÃO|RESULTADO|Apto|Inapto|$))",
            RegexOptions.IgnoreCase);

        if (!m.Success) return null;

        var t = Clean(m.Groups[1].Value);
        return t.Length is > 3 and < 50 ? t : null;
    }

    private static string? ExtractResultado(string text)
    {
        var m = Regex.Match(text,
            @"RESULTADO\s+DO\s+EXAME[\s\n]*(Apto|Inapto)[\s\w]*",
            RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.Trim().ToUpper();

        m = Regex.Match(text,
            @"(Apto|Inapto)\s+para\s+função",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim().ToUpper() : null;
    }

    private static string? ExtractDataExame(string text)
    {
        string[] patterns =
        [
            @"(\d{2}/\d{2}/\d{4})\s*Exame\s+Clínico",
            @"Exame\s+Clínico\s*(\d{2}/\d{2}/\d{4})",
            @"AVALIAÇÃO\s+CLÍNICA[\s\S]{0,100}?(\d{2}/\d{2}/\d{4})"
        ];

        foreach (var p in patterns)
        {
            var m = Regex.Match(text, p, RegexOptions.IgnoreCase);
            if (m.Success &&
                DateTime.TryParseExact(m.Groups[1].Value, "dd/MM/yyyy",
                    null, DateTimeStyles.None, out _))
                return m.Groups[1].Value;
        }
        return null;
    }

    private static string? ExtractMedicoPcmso(string text)
    {
        var match = Regex.Match(text,
            @"PCMSO\s*[\r\n]*\s*([A-ZÀ-Ú][A-ZÀ-Úa-zà-ú\.\-\s]{4,99}?)\s*\d{4,}",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        var nome = Clean(match.Groups[1].Value);

        if (nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2 &&
            nome.Length > 5)
        {
            return nome.ToUpperInvariant();
        }

        return null;
    }

    private static string? ExtractMedicoExaminador(string text)
    {
        var matches = Regex.Matches(text,
            @"([A-ZÀ-Úa-zà-ú\s\.]{5,100}?)[-–—]\s*\d{4,6}\s*[\/\-]?\s*[A-Z]{2}");

        if (matches.Count > 0)
        {
            var nome = Clean(matches[^1].Groups[1].Value).ToUpper();
            if (IsValidPersonName(nome)) return nome;
        }

        var assinado = Regex.Match(text,
            @"Assinado\s+(?:biometricamente|digitalmente)\s+por:\s*([A-ZÀ-Úa-zà-ú\s\.]+?)(?=[\:\,\n]|\d)",
            RegexOptions.IgnoreCase);

        if (assinado.Success)
        {
            var nome = Regex.Replace(Clean(assinado.Groups[1].Value), @"[\*\d]+", "").Trim();
            if (IsValidPersonName(nome)) return nome.ToUpper();
        }

        return null;
    }

    
    private static bool IsValidPersonName(string text) =>
        !string.IsNullOrWhiteSpace(text)
        && text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2
        && text.Length > 5;
}