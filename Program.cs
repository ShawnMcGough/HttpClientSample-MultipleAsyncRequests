using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Json;

namespace HttpClientSample_MultipleRequests
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            var retryPolicy = HttpPolicyExtensions
                                .HandleTransientHttpError()
                                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var socketHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) };
            var pollyHandler = new PolicyHttpMessageHandler(retryPolicy)
            {
                InnerHandler = socketHandler,
            };

            var httpClient = new HttpClient(pollyHandler)
            {
                BaseAddress = new Uri("https://httpbin.org")
            };


            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var payload = $"{{'id' = {i}}}";
                tasks.Add(PostContent(httpClient, payload));
            }

            Console.WriteLine("Waiting for all tasks to complete...");

            await Task.WhenAll(tasks);

            Console.WriteLine("Done.");

        }

        static async Task PostContent(HttpClient httpClient, string payload)
        {
            var operationResponse = await httpClient.PostAsJsonAsync("post", payload); // "post" is the sample endpoint here, not the http verb

            if (operationResponse.IsSuccessStatusCode)
            {
                var content = await operationResponse.Content.ReadAsStringAsync();
                Console.WriteLine(content);

            }
            else
            {
                Console.WriteLine($"error");
                Environment.Exit(1);
            }
        }

    }
}
