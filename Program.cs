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
            // definie the retry policy
            // for transient errors (5xx, 408, etc)
            // 3 retries, each time waiting 2^retryAttempt seconds
            var retryPolicy = HttpPolicyExtensions
                                .HandleTransientHttpError()
                                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            // create a socket handler with a 15 minute lifetime
            // pooled connections dramatically improve performance because connection creation is expensive
            var socketHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) };

            // create a policy handler with the retry policy
            var pollyHandler = new PolicyHttpMessageHandler(retryPolicy) { InnerHandler = socketHandler };

            // create the http client with the policy handler and base address
            var httpClient = new HttpClient(pollyHandler) { BaseAddress = new Uri("https://httpbin.org") };

            // initialize a list of tasks to accumulate the async operations
            var tasks = new List<Task>();

            // create an aribrary 10 tasks that will run asynchronously
            for (int i = 0; i < 10; i++)
            {
                var payload = $"{{'id' = {i}}}";

                // add the task to the list. the task will run asynchronously, allowing for parallel execution
                tasks.Add(PostContent(httpClient, payload));
            }

            Console.WriteLine("Waiting for all tasks to complete...");

            // wait for all the tasks to complete
            // ommitting this line could cause the program to exit before all the tasks are complete
            await Task.WhenAll(tasks);

            Console.WriteLine("Done.");

        }

        static async Task PostContent(HttpClient httpClient, string payload)
        {

            // post the payload to the endpoint
            // in this example, "post" is the api endpoint, not the http verb
            // you would change "post" to whatever endpoint you are calling.
            // for example, a weather api might be "setweatherforecast"
            var operationResponse = await httpClient.PostAsJsonAsync("post", payload);

            // read the content of the response as a string
            var content = await operationResponse.Content.ReadAsStringAsync();

            // check the response was success
            if (operationResponse.IsSuccessStatusCode)
            {
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine($"ERROR: {operationResponse.StatusCode} - {content}");
                // end the program on any errors
                Environment.Exit(1);
            }
        }

    }
}
