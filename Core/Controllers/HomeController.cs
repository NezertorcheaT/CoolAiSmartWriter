using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using YandexGPT;

namespace Core.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AiSettings _settings;
    private readonly YandexAi _yContext;
    private AiViewModel _aiViewModel;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
        _settings = AiSettings
            .Template()
            .AddMessage(
                new AiSettings.Message(AiSettings.MessageRole.System,
                    "Нужно написать литературное произведение, учитывая интересы пользователя," +
                    " предоставленное им описание или план. Текст должен быть написан в художественном стиле. " +
                    "Если не явно не указано пользователем, текст произведения должен иметь не менее" +
                    " 25 слов."
                )
            )
            .AddMessage(
                new AiSettings.Message(AiSettings.MessageRole.User,
                    "сгенерируй мне стих про пчелу строчки на четыре"
                )
            );
        _yContext = new YandexAi(_settings);
        _aiViewModel = new AiViewModel(_yContext);
    }

    public IActionResult Index() => View();
    public IActionResult Privacy() => View();

    public IActionResult TextCreation() => View(_aiViewModel);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateFile(string userContent)
    {
        if (string.IsNullOrEmpty(userContent))
        {
            TempData["Error"] = "Поле не может быть пустым!";
            return RedirectToAction("TextCreation");
        }

        // Генерация файла на основе введенных данных
        _yContext.Settings =
            _yContext.Settings.SetMessage(1, new AiSettings.Message(AiSettings.MessageRole.User, userContent));
        byte[] fileBytes = Encoding.UTF8.GetBytes(await _yContext.Prompt());
        return File(fileBytes, "text/plain", "generated_file.txt");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(
        new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
    );
}