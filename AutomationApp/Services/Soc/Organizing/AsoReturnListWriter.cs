using AutomationApp.Models.Soc;

namespace AutomationApp.Services.Soc.Organizing;

public class AsoReturnListWriter
{
    public void Generate(IReadOnlyList<AsoData> records, string outputFolder)
    {
        var retornos = records
            .Where(r => r.TipoExame.Contains("Retorno", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (retornos.Count == 0) return;

        var aptos = retornos
            .Where(r => !r.Resultado.Contains("INAPTO", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Funcionario)
            .Order()
            .ToList();

        var inaptos = retornos
            .Where(r => r.Resultado.Contains("INAPTO", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Funcionario)
            .Order()
            .ToList();

        var path = Path.Combine(outputFolder, "Lista_Retorno_Trabalho.txt");

        using var writer = new StreamWriter(path, append: false, System.Text.Encoding.UTF8);

        writer.WriteLine("Bom dia Leonardo!");
        writer.WriteLine("Informo o retorno ao trabalho dos seguintes servidores, conforme avaliação:");
        writer.WriteLine();

        writer.WriteLine("APTOS:");
        if (aptos.Count > 0) aptos.ForEach(n => writer.WriteLine($"- {n}"));
        else writer.WriteLine("(Nenhum)");

        writer.WriteLine();
        writer.WriteLine(new string('-', 40));
        writer.WriteLine();

        writer.WriteLine("INAPTOS:");
        if (inaptos.Count > 0) inaptos.ForEach(n => writer.WriteLine($"- {n}"));
        else writer.WriteLine("(Nenhum)");
    }
}