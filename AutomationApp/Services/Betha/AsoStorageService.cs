namespace AutomationApp.Services.Betha
{
    public class AsoStorageService
    {
        private readonly Action<string> _logger;

        public AsoStorageService(Action<string> logger)
        {
            _logger = logger;
        }

        public void ValidatePdfFile(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new FileNotFoundException("O caminho do PDF está em branco na planilha.");

            string pathClean = pdfPath.Replace("\"", "").Trim();

            if (!File.Exists(pathClean))
                throw new FileNotFoundException($"Arquivo PDF não encontrado no caminho especificado: '{pathClean}'");

            if (!pathClean.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"O arquivo especificado não é um PDF válido: '{Path.GetFileName(pathClean)}'");
        }
    }
}