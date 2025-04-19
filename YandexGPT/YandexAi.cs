using System.Net.Http.Headers;

namespace YandexGPT;

public class YandexAi : IDisposable
{
    private readonly AiSettings _settings;
    private YandexContext _context;
    private readonly HttpClient _client = new();

    public YandexAi(AiSettings settings)
    {
        _settings = settings;
        _context = new YandexContext(
            new OAuthToken(Environment.GetEnvironmentVariable("OAUTH_TOKEN")!),
            new YDirectoryId(Environment.GetEnvironmentVariable("DIRECTORY_ID")!)
        );
    }

    public async Task<string> Prompt()
    {
        while (_context.IamToken is null) await Task.Delay(25);

        var promptText = _settings.Build(_context.DirectoryId);
        Console.WriteLine(promptText);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
            "https://llm.api.cloud.yandex.net/foundationModels/v1/completion");

        request.Headers.Add("Authorization", "Bearer " + _context.IamToken);

        request.Content =
            new StringContent(promptText.Replace("\n", string.Empty).Replace("\r", string.Empty));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}