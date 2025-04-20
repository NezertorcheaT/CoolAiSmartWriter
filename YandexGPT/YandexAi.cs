using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace YandexGPT;

public class YandexAi : IDisposable
{
    public AiSettings Settings { get; set; }
    private YandexContext _context;
    private readonly HttpClient _client = new();
    private const int _a4AspectY = 700;
    private readonly int _a4AspectX;

    public YandexAi(AiSettings settings)
    {
        _a4AspectX = (int)(1 / Math.Sqrt(2) * _a4AspectY);
        Settings = settings;
        _context = new YandexContext(
            new OAuthToken(Environment.GetEnvironmentVariable("OAUTH_TOKEN")!),
            new YDirectoryId(Environment.GetEnvironmentVariable("DIRECTORY_ID")!)
        );
    }

    public async Task<IEnumerable<string>> ImageGeneration(ICollection<string> prompts, string webRoot)
    {
        while (_context.IamToken is null) await Task.Delay(25);

        List<Task<string>> resultTasks = new(prompts.Count);
        foreach (var prompt in prompts)
        {
            var promptText =
                $@"
            {{
            ""modelUri"": ""art://{_context.DirectoryId}/yandex-art/latest"",
            ""generationOptions"": {{
              ""seed"": ""{(int)Stopwatch.GetTimestamp() % 1000}"",
              ""aspectRatio"": {{
                 ""widthRatio"": ""{_a4AspectX}"",
                 ""heightRatio"": ""{_a4AspectY}""
               }}
            }},
            ""messages"": [
              {{
                ""weight"": ""1"",
                ""text"": ""{prompt}""
              }}
            ]
            }}
            ";
            Console.WriteLine(promptText);

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://llm.api.cloud.yandex.net/foundationModels/v1/imageGenerationAsync");

            request.Headers.Add("Authorization", "Bearer " + _context.IamToken);

            request.Content = new StringContent(promptText.Replace("\n", string.Empty).Replace("\r", string.Empty));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            resultTasks.Add(response.Content.ReadAsStringAsync());
        }

        await Task.WhenAll(resultTasks);
        var resultIds = resultTasks.Select(i => JsonNode.Parse(i.Result)?["id"]?.ToString() ?? "");
        await Task.Delay(8000);

        List<Task<string>> imageTasks = new(prompts.Count);
        foreach (var id in resultIds)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://llm.api.cloud.yandex.net:443/operations/{id}");

            request.Headers.Add("Authorization", "Bearer " + _context.IamToken);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            imageTasks.Add(response.Content.ReadAsStringAsync());
        }

        await Task.WhenAll(imageTasks);
        var base64Images = imageTasks
            .Select(i =>
            {
                var jsonNode = JsonNode.Parse(i.Result);
                if (jsonNode is null) return (response: "", path: "");
                return (response: jsonNode["response"]["image"].ToString(),
                    path: $@"{webRoot}\generated\{jsonNode["id"]}.jpeg");
            })
            .ToArray();

        foreach (var (base64Image, outputFilePath) in base64Images)
        {
            var base64Data = base64Image.Contains(',')
                ? base64Image.Split(',')[1]
                : base64Image;

            var imageBytes = Convert.FromBase64String(base64Data);

            var directory = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            _ = File.WriteAllBytesAsync(outputFilePath, imageBytes);

            Console.WriteLine($"Файл успешно сохранен: {outputFilePath}");
        }

        return base64Images.Select(i => i.path);
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