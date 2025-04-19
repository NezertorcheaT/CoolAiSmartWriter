using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace YandexGPT;

internal class YandexContext : IDisposable
{
    public OAuthToken OAuthToken { get; }
    public YDirectoryId DirectoryId { get; private init; }
    public IamToken? IamToken { get; private set; }
    private readonly Thread _thread;
    private bool _isDisposed = false;

    public YandexContext(OAuthToken oAuthToken, YDirectoryId directoryId)
    {
        OAuthToken = oAuthToken;
        DirectoryId = directoryId;

        _thread = new Thread(DoUpdates);
        _thread.Start();
    }

    private async void DoUpdates()
    {
        var sleepTime = TimeSpan.FromHours(5);
        var client = new HttpClient();

        while (!_isDisposed)
        {
            HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Post, "https://iam.api.cloud.yandex.net/iam/v1/tokens");
            var content = $"{{\"yandexPassportOauthToken\":\"{OAuthToken}\"}}";
            request.Content = new StringContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseText = "";
            responseText = await response.Content.ReadAsStringAsync();
            var parsed = JsonNode.Parse(responseText);
            if (parsed is not null)
                responseText = (parsed["iamToken"] ?? responseText).ToString();
            IamToken = new IamToken(responseText);
            if (_isDisposed) break;
            Thread.Sleep(sleepTime);
        }

        client.Dispose();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _thread.Join();
    }
}

internal readonly record struct OAuthToken(string Token)
{
    public static implicit operator string(OAuthToken obj) => obj.Token;
    public override string ToString() => Token;
}

internal readonly record struct YDirectoryId(string Token)
{
    public static implicit operator string(YDirectoryId obj) => obj.Token;
    public override string ToString() => Token;
}

internal readonly record struct IamToken(string Token)
{
    public static implicit operator string(IamToken obj) => obj.Token;
    public override string ToString() => Token;
}