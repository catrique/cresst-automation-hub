using System.Net;
using AutomationApp.Services.Betha;
using Moq;
using Moq.Protected;

namespace AutomationApp.Tests.Services.Betha
{
    public class BethaApiServiceTests
    {
        private HttpMessageHandler CreateMockHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent)
                });

            return mockHandler.Object;
        }

        [Fact]
        public async Task GetEmployeeIdByCpfAsync_ShouldReturnId_WhenApiReturnsValidJsonArray()
        {
            string mockCpf = "123.456.789-00";
            string apiResponseJson = "[{ \"id\": \"98765\", \"name\": \"João Silva\" }]";
            
            var handler = CreateMockHttpMessageHandler(apiResponseJson, HttpStatusCode.OK);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.fakebetha.cloud/") };
            
            var service = new BethaApiService(httpClient);

            string result = await service.GetEmployeeIdByCpfAsync(mockCpf);

            Assert.Equal("98765", result);
        }

        [Fact]
        public async Task GetEmployeeIdByCpfAsync_ShouldReturnNull_WhenEmployeeNotFound()
        {
            string mockCpf = "00000000000";
            
            var handler = CreateMockHttpMessageHandler(string.Empty, HttpStatusCode.NotFound);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.fakebetha.cloud/") };
            
            var service = new BethaApiService(httpClient);

            string result = await service.GetEmployeeIdByCpfAsync(mockCpf);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetEmployeeIdByCpfAsync_ShouldThrowArgumentException_WhenCpfIsEmpty()
        {
            var httpClient = new HttpClient();
            var service = new BethaApiService(httpClient);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetEmployeeIdByCpfAsync(""));
        }
    }
}