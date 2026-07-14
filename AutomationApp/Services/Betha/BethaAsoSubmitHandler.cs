using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using AutomationApp.Models.Betha;

namespace AutomationApp.Services.Betha
{
    public class BethaAsoSubmitHandler
    {
        private readonly AsoBusinessRulesService _rulesService = new();
        private readonly AsoStorageService _storageService = new(msg => { });
        private static readonly long ID_INSTITUICAO_MEDICA = Program.Settings.Betha.Api.MedicalInstitutionId;
        private static readonly long ID_FORMULARIO_ASO = Program.Settings.Betha.Api.AsoFormId;
        private static string TIPO_CAMPO_FORMULARIO = Program.Settings.Betha.Api.FormFieldType;

        public async Task<bool> SubmitSingleAsoAsync(BethaAsoData asoRow, string _funcionarioIdRealNaoUsado)
        {
            var middleware = Program.ServiceProvider?.GetService<IBethaApiService>();
            if (middleware == null) throw new Exception("Middleware BethaApiService não inicializado.");

            var endpoints = Program.Settings.Betha?.Api?.Endpoints;
            if (endpoints == null ||
                !endpoints.TryGetValue("Individual", out string? endpointPessoaFisica) ||
                !endpoints.TryGetValue("RegistrationSelect", out string? endpointMatricula) ||
                !endpoints.TryGetValue("Professional", out string? endpointProfissional) ||
                !endpoints.TryGetValue("Aso", out string? endpointAso) ||
                !endpoints.TryGetValue("Attachment", out string? endpointAttachment) ||
                !endpoints.TryGetValue("AsoForm", out string? endpointAsoForm))
            {
                throw new Exception("Erro de Configuração: alguma rota obrigatória está ausente no appsettings.");
            }

            _storageService.ValidatePdfFile(asoRow.CaminhoPDF);
            string tipoExameNorm = _rulesService.NormalizeExamType(asoRow.TipoExame);

            if (string.IsNullOrWhiteSpace(asoRow.MedicoExaminador) || string.IsNullOrWhiteSpace(asoRow.MedicoPcmso))
                throw new Exception("Médico Examinador ou Coordenador do PCMSO não preenchido na planilha.");

            string cpfLimpo = asoRow.Cpf.Replace(".", "").Replace("-", "").Trim();
            if (cpfLimpo.Length != 11)
                throw new Exception($"Divergência: o CPF informado [{asoRow.Cpf}] possui tamanho inválido.");

            Console.WriteLine($"-> [Linha {asoRow.LinhaPlanilha}] Buscando pessoa física por nome/CPF...");
            string filtroPessoa = $"(nome like \"%25{asoRow.Funcionario.Trim()}%25\")";
            var pessoas = await BuscarRegistrosAsync(middleware, endpointPessoaFisica, filtroPessoa);
            var pessoaEncontrada = pessoas.FirstOrDefault(p =>
                p.TryGetProperty("cpf", out var cpfEl) && cpfEl.GetString() == cpfLimpo);

            if (pessoaEncontrada.ValueKind == JsonValueKind.Undefined)
                throw new Exception($"Divergência: pessoa não encontrada ou CPF não bate para {asoRow.Funcionario}.");

            long pessoaId = pessoaEncontrada.GetProperty("id").GetInt64();

            Console.WriteLine($"-> [Linha {asoRow.LinhaPlanilha}] Buscando matrícula '{asoRow.Matricula}'...");
            string filtroMatricula = $"pessoaNome like \"%25%25\" and pessoa = {pessoaId}";
            var matriculas = await BuscarRegistrosAsync(middleware, endpointMatricula, filtroMatricula);
            var matriculaEncontrada = matriculas.FirstOrDefault(m =>
                m.TryGetProperty("descricao", out var d) && d.GetString() == asoRow.Matricula.Trim());

            if (matriculaEncontrada.ValueKind == JsonValueKind.Undefined)
                throw new Exception($"Divergência: matrícula '{asoRow.Matricula}' não encontrada para {asoRow.Funcionario}.");

            long matriculaId = matriculaEncontrada.GetProperty("id").GetInt64();
            string? dataInicioContrato = matriculaEncontrada.TryGetProperty("dataInicioContrato", out var dic)
                ? dic.GetString() : null;

            var (dataExameStr, dataInicioAtividades, dataValidadeFinal) = _rulesService.CalcularDatas(
                asoRow.DataExame, asoRow.Resultado, tipoExameNorm, dataInicioContrato);

            var dadosParciais = new Dictionary<string, object?>
            {
                ["data"] = dataExameStr,
                ["encaminhamentoAso"] = "NENHUMA",
                ["conclusaoAso"] = "INCONCLUSIVO",
                ["dataValidadeAso"] = dataExameStr,
                ["tipoExameAso"] = tipoExameNorm,
                ["reabilitado"] = false,
                ["matricula"] = new { id = matriculaId },
                ["pessoaFisica"] = new { id = pessoaId },
                ["isSaveParcial"] = true
            };

            Console.WriteLine($"-> [Linha {asoRow.LinhaPlanilha}] Criando rascunho de ASO na Betha...");
            string? responseDraft = await middleware.SendPostRequestAsync(endpointAso, JsonSerializer.Serialize(dadosParciais));
            if (string.IsNullOrEmpty(responseDraft)) return false;

            using JsonDocument draftDoc = JsonDocument.Parse(responseDraft);
            long asoId = draftDoc.RootElement.GetProperty("id").GetInt64();

            long medicoExaminadorId = await BuscarMedicoIdAsync(middleware, endpointProfissional, asoRow.MedicoExaminador.Trim());
            long medicoPcmsoId = await BuscarMedicoIdAsync(middleware, endpointProfissional, asoRow.MedicoPcmso.Trim());

            string caminhoLimpoPdf = asoRow.CaminhoPDF.Replace("\"", "").Trim();
            Console.WriteLine($"-> Transmitindo arquivo PDF...");
            string? uploadResponse = await middleware.UploadFileRequestAsync(endpointAttachment, caminhoLimpoPdf);
            if (string.IsNullOrEmpty(uploadResponse))
                throw new Exception("Falha no upload do PDF: resposta vazia.");

            using JsonDocument uploadDoc = JsonDocument.Parse(uploadResponse);
            JsonElement anexoObj = uploadDoc.RootElement.ValueKind == JsonValueKind.Array
                ? uploadDoc.RootElement[0]
                : uploadDoc.RootElement;

            long anexoId = anexoObj.GetProperty("id").GetInt64();
            string anexoName = anexoObj.GetProperty("name").GetString() ?? "";
            string anexoKey = anexoObj.GetProperty("key").GetString() ?? "";

            string resultadoLimpo = asoRow.Resultado.Trim().ToUpper();

            var dadosCompletos = new Dictionary<string, object?>(dadosParciais)
            {
                ["id"] = asoId,
                ["conclusaoAso"] = resultadoLimpo,
                ["dataValidadeAso"] = dataValidadeFinal,
                ["dataInicioAtividades"] = dataInicioAtividades,
                ["isSaveParcial"] = false,
                ["medicoResponsavel"] = new { id = medicoExaminadorId },
                ["medicoResponsavelPcmso"] = new { id = medicoPcmsoId },
                ["instituicaoMedica"] = new { id = ID_INSTITUICAO_MEDICA },
                ["anexos"] = new object[]
                {
                    new
                    {
                        data = dataExameStr,
                        tipoDocumento = new { id = 1780 },
                        arquivos = new object[]
                        {
                            new { id = anexoId, name = anexoName, key = anexoKey }
                        }
                    }
                },
                ["formulario"] = new
                {
                    aso = new { id = asoId },
                    formulario = new { id = ID_FORMULARIO_ASO },
                    campoAdicional = Array.Empty<object>()
                }
            };

            string urlUpdate = $"{endpointAso.TrimEnd('/')}/{asoId}";
            Console.WriteLine($"-> Atualizando dados médicos no ASO {asoId}...");
            await middleware.SendPutRequestAsync(urlUpdate, JsonSerializer.Serialize(dadosCompletos));

            var payloadForm = new
            {
                id = (object?)null,
                aso = new { id = asoId },
                formulario = new
                {
                    id = ID_FORMULARIO_ASO,
                    descricao = "ASO-EXTERNO",
                    desabilitado = false,
                    tipoCampo = TIPO_CAMPO_FORMULARIO
                },
                campoAdicional = Array.Empty<object>(),
                version = (object?)null
            };

            Console.WriteLine($"-> Consolidando formulário final...");
            await middleware.SendPostRequestAsync(endpointAsoForm, JsonSerializer.Serialize(payloadForm));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🎉 Sucesso! ASO {asoId} totalmente processado.");
            Console.ResetColor();
            return true;
        }

        private async Task<long> BuscarMedicoIdAsync(IBethaApiService middleware, string endpointProfissional, string nomeMedico)
        {
            string filtro = $"(nome like \"%25{nomeMedico}%25\" and profissao = \"MEDICO\")";
            var medicos = await BuscarRegistrosAsync(middleware, endpointProfissional, filtro);
            if (medicos.Count == 0)
                throw new Exception($"Médico '{nomeMedico}' não encontrado.");
            return medicos[0].GetProperty("id").GetInt64();
        }

        private async Task<List<JsonElement>> BuscarRegistrosAsync(IBethaApiService middleware, string endpoint, string filtro, int limit = 50)
        {
            string query = $"filter={Uri.EscapeDataString(filtro)}&limit={limit}&offset=0";
            string? json = await middleware.SendGetRequestAsync(endpoint, query);

            var resultado = new List<JsonElement>();
            if (string.IsNullOrEmpty(json)) return resultado;

            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("content", out JsonElement content) && content.ValueKind == JsonValueKind.Array)
                foreach (var el in content.EnumerateArray())
                    resultado.Add(el.Clone());
            return resultado;
        }
    }
}