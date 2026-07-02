using System.Text;

namespace AutomationApp.Services.Betha
{
    public class BethaApiService : IBethaApiService
    {
        private readonly HttpClient _httpClient;

        public BethaApiService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string?> SendGetRequestAsync(string endpoint, string queryString)
        {
            string urlCompleta = string.IsNullOrWhiteSpace(queryString) ? endpoint : $"{endpoint}?{queryString}";
            HttpResponseMessage response = await _httpClient.GetAsync(urlCompleta);
            return await HandleResponseAsync(response);
        }

        public async Task<string?> SendPostRequestAsync(string endpoint, string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
            return await HandleResponseAsync(response);
        }

        public async Task<string?> SendPutRequestAsync(string endpoint, string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PutAsync(endpoint, content);
            return await HandleResponseAsync(response);
        }

        public async Task<string?> UploadFileRequestAsync(string endpoint, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Arquivo para upload não encontrado: {filePath}");

            using var multipartContent = new MultipartFormDataContent();
            
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var fileContent = new StreamContent(fileStream);
            
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            multipartContent.Add(fileContent, "file", Path.GetFileName(filePath));
            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, multipartContent);
            return await HandleResponseAsync(response);
        }

        private async Task<string?> HandleResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string erroCorpo = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Erro na API Betha: {(int)response.StatusCode} - {response.ReasonPhrase}. Detalhes: {erroCorpo}");
            }
            return await response.Content.ReadAsStringAsync();
        }
    }
}