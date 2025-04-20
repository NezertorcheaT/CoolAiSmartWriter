using System.Globalization;
using System.Text;

namespace YandexGPT;

public class AiSettings
{
    public record struct Message(MessageRole Role, string Text);

    public enum MessageRole
    {
        System,
        User,
        Assistant
    }

    public enum ReasoningOption
    {
        Disabled,
        EnabledHidden
    }

    private bool _stream = false;
    private float _temperature = 0.6f;
    private int _maxTokens = 2000;
    private ReasoningOption _reasoningOption = ReasoningOption.Disabled;
    private List<Message> _messages = [];

    public static AiSettings Template() => new();

    public AiSettings ReasoningOptions(ReasoningOption s)
    {
        _reasoningOption = s;
        return this;
    }

    public AiSettings MaxTokens(int s)
    {
        _maxTokens = s;
        return this;
    }

    public AiSettings AddMessagesRange(IEnumerable<Message> message)
    {
        _messages.AddRange(message);
        return this;
    }

    public AiSettings AddMessage(Message message)
    {
        _messages.Add(message);
        return this;
    }

    public AiSettings SetMessage(int index, Message message)
    {
        _messages[index] = message;
        return this;
    }

    public AiSettings Temperature(float s)
    {
        _temperature = s;
        return this;
    }

    public AiSettings IsStreaming(bool s)
    {
        _stream = s;
        return this;
    }

    public string Build(string dirId)
    {
        var sb = new StringBuilder();
        sb.Append("""{ "modelUri": "gpt://""");
        sb.Append(dirId);
        sb.Append("""/yandexgpt", "completionOptions": { "stream": """);
        sb.Append(_stream ? "true" : "false");
        sb.Append(""", "temperature": """);
        sb.Append(_temperature.ToString(NumberFormatInfo.InvariantInfo));
        sb.Append(@", ""maxTokens"": """);
        sb.Append(_maxTokens);
        sb.Append(@""", ""reasoningOptions"": { ""mode"": """);
        sb.Append(_reasoningOption is ReasoningOption.Disabled ? "DISABLED" : "ENABLED_HIDDEN");
        sb.Append(@""" } }, ""messages"": [ ");
        var i = 0;
        foreach (var message in _messages)
        {
            sb.Append($"{{\"role\": \"{message.Role switch
            {
                MessageRole.System => "system",
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                _ => "system"
            }}\",\"text\": \"{message.Text.Replace("\n", "\\n").Replace("\"", "\\\"")}\"}}");
            if (i != _messages.Count - 1)
                sb.Append(", ");
            i++;
        }

        sb.Append("] }");

        return sb.ToString();
    }
}