// using System.Net.Http.Headers;
// using System.Text.Json;
// using AutomationApp.Models.Betha.Payloads;

// namespace AutomationApp.Services.Betha
// {
//     public class BethaApiClientService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly Action<string> _logger;

//         public BethaApiClientService(Action<string> logger)
//         {
//             _logger = logger;
//             _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
//         }

//         public void ConfigureAuthentication()
//         {
//             var apiConfig = Program.Settings.Betha.Api;

//             _httpClient.DefaultRequestHeaders.Clear();
//             _httpClient.DefaultRequestHeaders.Add("Authorization", apiConfig.Authorization);
//             _httpClient.DefaultRequestHeaders.Add("user-access", apiConfig.UserAccess);
//             _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//             _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Automation/1.0");
//         }

//         public async Task<string> ProcessBethaStreamAsync(
//             string funcionario, 
//             string cpf, 
//             string tipoExame, 
//             string dataExame, 
//             string dataVencimento, 
//             string resultado, 
//             string examinador, 
//             string pcmso,
//             string pdfPath)
//         {
//             var apiConfig = Program.Settings.Betha.Api;
//             string endpointPessoa = apiConfig.Endpoints.ContainsKey("Individual") ? apiConfig.Endpoints["Individual"] : "pessoa/fisica";
//             string endpointAso = apiConfig.Endpoints.ContainsKey("Aso") ? apiConfig.Endpoints["Aso"] : "aso";
//             string fullSearchURLforPerson = $"{apiConfig.BaseUrl.TrimEnd('/')}/{endpointPessoa.TrimStart('/')}";
//             string fullURLforASOSubmission = $"{apiConfig.BaseUrl.TrimEnd('/')}/{endpointAso.TrimStart('/')}";

//             await Task.Delay(100); 
//             string jsonPessoaMock = $"{{\"id\": \"PESSOA_{new Random().Next(10000, 99999)}\", \"nome\": \"{funcionario}\"}}";
//             var personResponse = JsonSerializer.Deserialize<BethaPessoaResponse>(jsonPessoaMock);
//             string personIdReal = personResponse?.Id ?? throw new Exception("Falha ao deserializar dados da pessoa.");
           
//             var payloadParcial = new BethaAsoParcialRequest
//             {
//                 PessoaId = personIdReal,
//                 TipoExame = tipoExame
//             };
//             string partialJson = JsonSerializer.Serialize(payloadParcial);
            
//             await Task.Delay(100);
//             string jsonAsoMock = $"{{\"id\": \"ASO_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}\"}}";
//             var asoResponse = JsonSerializer.Deserialize<BethaAsoParcialResponse>(jsonAsoMock);
//             string asoIdReal = asoResponse?.Id ?? throw new Exception("Falha ao criar rascunho de ASO.");

//             var fullPayload = new BethaAsoCompletoRequest
//             {
//                 DataExame = dataExame,
//                 DataVencimento = dataVencimento,
//                 Resultado = resultado,
//                 MedicoExaminador = examinador,
//                 MedicoPcmso = pcmso
//             };
//             string fullJson = JsonSerializer.Serialize(fullPayload);
//             await Task.Delay(100);

//             string pathClean = pdfPath.Replace("\"", "").Trim();
//             using (var fileStream = new FileStream(pathClean, FileMode.Open, FileAccess.Read))
//             {
//                 await Task.Delay(150); 
//             }

//             return asoIdReal;
//         }
//     }
// }