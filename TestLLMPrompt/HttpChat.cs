using Newtonsoft.Json;
using System.Configuration;
using System.Text;
using System.Threading;

namespace TestLLMPrompt
{
    internal class HttpChat
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string _openAIKey = ConfigurationManager.AppSettings["OPENAI_API_KEY"];
        private static readonly string _openAIEndpoint = ConfigurationManager.AppSettings["OPENAI_ENDPOINT_NAME"];
        private static readonly string _openAIDeployment = ConfigurationManager.AppSettings["OPENAI_DEPLOYMENT_NAME"];
        private static readonly object lockObj = new object();
        private static readonly HttpChat instance = new HttpChat();

        // Private constructor to prevent instantiation
        private HttpChat()
        {
            if (string.IsNullOrEmpty(_openAIKey))
                throw new Exception("Failed to find API key");

            client.Timeout = TimeSpan.FromSeconds(120);
        }

        // Public method to get the singleton instance
        public static HttpChat Instance => instance;

        public async Task<string> ChatResponseString(List<SimpleGptMessage> msg, CancellationToken cancellationToken)
        {
            var completionRequest = new
            {
                model = _openAIDeployment ?? "",
                max_tokens = 2000,
                temperature = 0,
                messages = msg
            };

            var requestString = JsonConvert.SerializeObject(completionRequest);
            string url = $"{_openAIEndpoint}openai/deployments/{_openAIDeployment}/chat/completions?api-version=2024-05-01-preview";

            OpenAIResponse returnObject = null;
            try
            {
                string threadID = null;
                Thread thread = Thread.CurrentThread;
                lock (lockObj)
                {
                    threadID = $"   Thread ID: {thread.ManagedThreadId}";
                }

                int maxRetries = 16;
                int delayMilliseconds = 20000;
                Random random = new Random();
                delayMilliseconds = random.Next(10000, 20000);

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        Console.WriteLine($"Starting attempt {attempt}, Thread: {threadID}");
                        var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
                        httpReq.Headers.Add("Authorization", $"Bearer {_openAIKey}");
                        httpReq.Headers.Add("api-key", $"{_openAIKey}");
                        httpReq.Content = new StringContent(requestString, Encoding.UTF8, "application/json");

                        HttpResponseMessage httpResponse = await client.SendAsync(httpReq, cancellationToken).ConfigureAwait(false);
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Request succeeded of thread id {threadID}");

                            httpResponse.EnsureSuccessStatusCode();
                            var result = await httpResponse.Content.ReadAsStringAsync();

                            if (string.IsNullOrEmpty(result))
                                throw new Exception("Failed to get result");
                            returnObject = JsonConvert.DeserializeObject<OpenAIResponse>(result);

                            if (returnObject == null)
                                throw new Exception("Failed to deserialize");
                            break;
                        }
                        else if (httpResponse.StatusCode == (System.Net.HttpStatusCode)429)
                        {
                            Console.WriteLine($"Attempt {attempt}: Too Many Requests. Retrying in {delayMilliseconds}ms... of thread id {threadID}");
                        }
                        else
                        {
                            Console.WriteLine($"Request failed with status code: {httpResponse.StatusCode}");
                            throw new Exception($"Failed with Status Code {httpResponse.StatusCode}");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Attempt {attempt}: Exception occurred - {ex.Message}. Retrying in {delayMilliseconds}ms...");
                    }
                    catch (OperationCanceledException ex)
                    {
                        Console.WriteLine("Operation cancelled");
                        throw new OperationCanceledException("Operation Cancelled");
                    }

                    await Task.Delay(delayMilliseconds);
                    if (attempt < 2)
                        delayMilliseconds *= 2;
                }

                return returnObject.choices[0].message.content;
            }
            catch (Exception e)
            {
                Console.WriteLine($"exception: {e.Message}, inner exception: {e.InnerException}");
                throw;
            }
        }

        public async Task<string> ChatResponseStringWithMaxToken(List<SimpleGptMessage> msg, CancellationToken cancellationToken)
        {
            var completionRequest = new
            {
                model = _openAIDeployment ?? "",
                max_tokens = 2000,
                temperature = 0,
                messages = msg
            };

            var requestString = JsonConvert.SerializeObject(completionRequest);
           
            string url = $"{_openAIEndpoint}openai/deployments/{_openAIDeployment}/chat/completions?api-version=2024-05-01-preview";

            OpenAIResponse returnObject = null;
            try
            {
                string threadID = null;
                Thread thread = Thread.CurrentThread;
                lock (lockObj)
                {
                    threadID = $"   Thread ID: {thread.ManagedThreadId}";
                }

                int maxRetries = 6;
                int delayMilliseconds = 10000;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
                        httpReq.Headers.Add("Authorization", $"Bearer {_openAIKey}");
                        httpReq.Headers.Add("api-key", $"{_openAIKey}");
                        httpReq.Content = new StringContent(requestString, Encoding.UTF8, "application/json");

                        HttpResponseMessage httpResponse = await client.SendAsync(httpReq, cancellationToken).ConfigureAwait(false);
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Request succeeded of thread id {threadID}");

                            httpResponse.EnsureSuccessStatusCode();
                            var result = await httpResponse.Content.ReadAsStringAsync();

                            if (string.IsNullOrEmpty(result))
                                throw new Exception("Failed to get result");
                            returnObject = JsonConvert.DeserializeObject<OpenAIResponse>(result);

                            if (returnObject == null)
                                throw new Exception("Failed to deserialize");
                            break;
                        }
                        else if (httpResponse.StatusCode == (System.Net.HttpStatusCode)429)
                        {
                            Console.WriteLine($"Attempt {attempt}: Too Many Requests. Retrying in {delayMilliseconds}ms... of thread id {threadID}");
                        }
                        else
                        {
                            Console.WriteLine($"Request failed with status code: {httpResponse.StatusCode}");
                            throw new Exception($"Failed with Status Code {httpResponse.StatusCode}");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Attempt {attempt}: Exception occurred - {ex.Message}. Retrying in {delayMilliseconds}ms...");
                        throw new Exception(ex.InnerException.ToString());
                    }
                    catch (OperationCanceledException ex)
                    {
                        Console.WriteLine("Operation cancelled");
                        throw new OperationCanceledException("Operation Cancelled");
                    }

                    await Task.Delay(delayMilliseconds);
                    delayMilliseconds *= 2;
                }

                return returnObject.choices[0].message.content;
            }
            catch (Exception e)
            {
                Console.WriteLine($"other exception: {e.InnerException}");
                throw new Exception($"other exception: {e.InnerException}");
            }
        }
    }

    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

}