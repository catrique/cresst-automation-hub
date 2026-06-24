using Xunit;
using AutomationApp.Services.Betha;

namespace AutomationApp.Tests
{
    public class AsoBusinessRulesServiceTests
    {
        private readonly AsoBusinessRulesService _rulesService;

        public AsoBusinessRulesServiceTests()
        {
            _rulesService = new AsoBusinessRulesService();
        }

        #region NormalizeExamType Tests

        [Theory]
        [InlineData("  Admissional  ", "ADMISSIONAL")]
        [InlineData("  ADMISSIONAL  ", "ADMISSIONAL")]
        [InlineData("  AdmISSIONAL  ", "ADMISSIONAL")]
        [InlineData("PERIÓDICO", "PERIODICO")]
        [InlineData("PERIÓDICÓ", "PERIODICO")]
        [InlineData("Mudança de Função", "MUDANCA_FUNCAO")]
        [InlineData("Mudanca de Funcão", "MUDANCA_FUNCAO")]
        [InlineData("MUDANÇA DE FUNÇÃO", "MUDANCA_FUNCAO")]
        [InlineData("MUDANcA DE FUNcÃO", "MUDANCA_FUNCAO")]
        [InlineData("Demissional", "DEMISSIONAL")]
        [InlineData("RETORNO AO TRABALHO", "RETORNO_TRABALHO")]
        public void NormalizeExamType_ShouldCleanAndNormalizeText(string input, string expected)
        {
            string result = _rulesService.NormalizeExamType(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeExamType_ShouldThrowArgumentException_WhenInputIsInvalid(string invalidInput)
        {
            var ex = Assert.Throws<ArgumentException>(() => _rulesService.NormalizeExamType(invalidInput));
            Assert.Contains("O tipo de exame está em branco na planilha", ex.Message);
        }

        #endregion

        #region CalculateDates Tests

        [Theory]
        [InlineData("25/05/2026", "RETORNO_TRABALHO", "INAPTO", "25/05/2026", "25/08/2026")]
        [InlineData("25/05/2026", "DEMISSIONAL", "APTO", "25/05/2026", "25/08/2026")]
        [InlineData("10/01/2026", "ADMISSIONAL", "APTO", "10/01/2026", "10/01/2027")]
        [InlineData("31/12/2026", "PERIODICO", "APTO", "31/12/2026", "31/12/2027")]
        [InlineData("15/04/2026", "MUDANCA_FUNCAO", "APTO", "15/04/2026", "15/04/2027")]
        [InlineData("28/02/2028", "PERIODICO", "APTO", "28/02/2028", "28/02/2029")]
        public void CalculateDates_ShouldHandleDateCorrectly(
            string examDateStr,
            string examType,
            string asoResult,
            string expectedExamDateStr,
            string expectedExpirationDateStr)
        {
            var (dtExame, dueDate) = _rulesService.CalculateDates(examDateStr, examType, asoResult);

            DateTime expectedExamDate = DateTime.ParseExact(expectedExamDateStr, "dd/MM/yyyy", null);
            DateTime expectedExpirationDate = DateTime.ParseExact(expectedExpirationDateStr, "dd/MM/yyyy", null);

            Assert.Equal(expectedExamDate, dtExame);
            Assert.Equal(expectedExpirationDate, dueDate);
        }

        [Theory]
        [InlineData("ADMISSIONAL")]
        [InlineData("PERIODICO")]
        [InlineData("MUDANCA_FUNCAO")]
        public void CalculateDates_ShouldCalculateCorrectExpiration_ForStandardExams(string examType)
        {
            string examDateStr = "25/05/2026";
            string asoResult = "APTO";
            DateTime expectedExamDate = new DateTime(2026, 5, 25);

            var (dtExame, dueDate) = _rulesService.CalculateDates(examDateStr, examType, asoResult);

            Assert.Equal(expectedExamDate, dtExame);
            Assert.Equal(new DateTime(2027, 5, 25), dueDate);
        }

        [Theory]
        [InlineData("32/05/2026")] 
        [InlineData("01/05/26")] 
        [InlineData("2026/05/16")] 
        [InlineData("12/21/2026")] 
        [InlineData("not-a-date")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void CalculateDates_ShouldThrowException_WhenDateFormatIsTrulyInvalid(string invalidDate)
        {
            
            Assert.Throws<FormatException>(() =>
                _rulesService.CalculateDates(invalidDate, "ADMISSIONAL", "APTO")
            );
        }

        #endregion
    }
}