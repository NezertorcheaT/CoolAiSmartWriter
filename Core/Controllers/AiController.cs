using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using YandexGPT;

namespace Core.Controllers;

public class AiController : Controller
{
    private readonly ILogger<AiController> _logger;
    private readonly AiSettings _settings;
    private readonly YandexAi _yContext;

    public AiController(ILogger<AiController> logger)
    {
        _logger = logger;
        _settings = AiSettings
            .Template()
            .AddMessage(
                new AiSettings.Message(AiSettings.MessageRole.User,
                    "сгенерируй мне стих про пчелу строчки на четыре"
                )
            );
        _yContext = new YandexAi(_settings);
    }

    public IActionResult TextCreation() => View(new AiViewModel(_yContext.Prompt()));

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(
        new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
    );
}