using Xunit;
using AutomationApp.Services.Betha;

namespace AutomationApp.Tests
{
    public class AsoStorageServiceTests
    {
        private readonly AsoStorageService _storageService;

        public AsoStorageServiceTests()
        {
            Action<string> dummyLogger = msg => { };
            _storageService = new AsoStorageService(dummyLogger);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        public void ValidatePdfFile_ShouldThrowFileNotFoundException_WhenPathIsEmpty(string emptyPath)
        {

            var ex = Assert.Throws<FileNotFoundException>(() => _storageService.ValidatePdfFile(emptyPath));
            Assert.Contains("O caminho do PDF está em branco na planilha", ex.Message);
        }

        [Fact]
        public void ValidatePdfFile_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {

            string ghostPath = @"C:\GhostFolder\NonExistingAso.pdf";
            var ex = Assert.Throws<FileNotFoundException>(() => _storageService.ValidatePdfFile(ghostPath));
            Assert.Contains("Arquivo PDF não encontrado no caminho", ex.Message);
        }

        [Fact]
        public void ValidatePdfFile_ShouldThrowFormatException_WhenExtensionIsInvalid()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"test_aso_{Guid.NewGuid()}.png");
            File.WriteAllText(tempPath, "%PDF-1.4 mock file content");

            try
            {
                var exception = Assert.Throws<FormatException>(() =>
                    _storageService.ValidatePdfFile(tempPath)
                );

                Assert.Contains("não é um PDF válido", exception.Message);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ValidatePdfFile_ShouldPassWithoutErrors_WhenFileIsValid()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"test_aso_{Guid.NewGuid()}.pdf");
            File.WriteAllText(tempPath, "%PDF-1.4 mock file content");

            try
            {
                _storageService.ValidatePdfFile(tempPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}