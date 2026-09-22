using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using Microsoft.Extensions.Options;

namespace TextToImageGenerator.Services
{
    
    public sealed class CloudflareAiService : ICloudflareAiService
    {
        private readonly HttpClient _http;
        private readonly CloudflareAiOptions _options;

        public CloudflareAiService(HttpClient http, IOptions<CloudflareAiOptions> options)
        {
            _options = options.Value;
            _http = http;
            _http.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");

            if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiToken);
            }
        }

        public async Task<byte[]> GenerateImageAsync(string prompt, int width, int height, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var url = $"accounts/{_options.AccountId}/ai/run/{_options.ImageModel}";

            using var form = new MultipartFormDataContent
        {
            { new StringContent(prompt), "prompt" },
            { new StringContent(width.ToString()), "width" },
            { new StringContent(height.ToString()), "height" }
        };

            using var response = await _http.PostAsync(url, form, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"A API retornou {(int)response.StatusCode} {response.ReasonPhrase}: {responseText}");
            }

            using var doc = JsonDocument.Parse(responseText);
            var base64 = doc.RootElement.GetProperty("result").GetProperty("image").GetString();
            if (string.IsNullOrEmpty(base64))
                throw new InvalidOperationException("A resposta não continha imagem.");

            return Convert.FromBase64String(base64);
        }

        public async IAsyncEnumerable<string> StreamTextAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var url = $"accounts/{_options.AccountId}/ai/run/{_options.TextModel}";

            var payload = new
            {
                messages = new[] { new { role = "user", content = prompt } },
                stream = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"A API retornou {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var payloadJson = line["data:".Length..].Trim();
                if (payloadJson == "[DONE]")
                    yield break;

                string? chunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(payloadJson);
                    if (doc.RootElement.TryGetProperty("response", out var responseProp))
                        chunk = responseProp.GetString();
                }
                catch (JsonException)
                {
                    continue; // ignora linhas malformadas do stream
                }

                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk;
            }
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId) || string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                throw new InvalidOperationException(
                    "CloudflareAi:AccountId e CloudflareAi:ApiToken não configurados. " +
                    "Use 'dotnet user-secrets' ou variáveis de ambiente.");
            }
        }
    }
}
