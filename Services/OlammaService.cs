namespace Back_HR.Services
{
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;

    public class OllamaService
    {
        private readonly HttpClient _httpClient;

        public OllamaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:11434/");
        }

        public async Task<string> GetResponseAsync(string prompt, string model = "hr-assistant")
        {
            var request = new
            {
                model,
                prompt,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("api/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama API error: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return jsonResponse.RootElement.GetProperty("response").GetString();
        }

        // For streaming responses (better for UX)
        public async IAsyncEnumerable<string> GetStreamingResponseAsync(string prompt, string model = "hr-assistant")
        {
            var request = new
            {
                model,
                prompt,
                stream = true
            };

            var response = await _httpClient.PostAsJsonAsync("api/generate", request);

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    var json = JsonSerializer.Deserialize<JsonDocument>(line);
                    yield return json.RootElement.GetProperty("response").GetString();
                }
            }
        }
    }
}
