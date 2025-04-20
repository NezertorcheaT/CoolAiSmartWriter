using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace YandexGPT;

public class YandexAi : IDisposable
{
    public AiSettings Settings { get; set; }
    private YandexContext _context;
    private readonly HttpClient _client = new();

    public YandexAi(AiSettings settings)
    {
        Settings = settings;
        _context = new YandexContext(
            new OAuthToken(Environment.GetEnvironmentVariable("OAUTH_TOKEN")!),
            new YDirectoryId(Environment.GetEnvironmentVariable("DIRECTORY_ID")!)
        );
    }

    public async Task<string> Prompt()
    {
        while (_context.IamToken is null) await Task.Delay(25);

        var promptText = Settings.Build(_context.DirectoryId);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
            "https://llm.api.cloud.yandex.net/foundationModels/v1/completion");

        request.Headers.Add("Authorization", "Bearer " + _context.IamToken);

        request.Content =
            new StringContent(promptText.Replace("\n", string.Empty).Replace("\r", string.Empty));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())?["result"]?["alternatives"]?.AsArray()
            .LastOrDefault()?["message"]?["text"]
            ?.ToString() ?? string.Empty;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}