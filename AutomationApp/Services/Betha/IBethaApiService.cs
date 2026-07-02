using System.Threading.Tasks;

namespace AutomationApp.Services.Betha
{
    public interface IBethaApiService
    {
        Task<string?> SendGetRequestAsync(string endpoint, string queryString);
        Task<string?> SendPostRequestAsync(string endpoint, string jsonPayload);
        Task<string?> SendPutRequestAsync(string endpoint, string jsonPayload);
        Task<string?> UploadFileRequestAsync(string endpoint, string filePath);
    }
}