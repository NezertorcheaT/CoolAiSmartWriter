using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using YandexGPT;

namespace Core.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly AiSettings _settings;
    private readonly YandexAi _yContext;
    private AiViewModel _aiViewModel;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
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
    public async Task<IActionResult> GenerateText(string userContent)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateImages(string userContent, int count)
    {
        if (string.IsNullOrEmpty(userContent))
        {
            TempData["Error"] = "Поле не может быть пустым!";
            return RedirectToAction("TextCreation");
        }

        // Генерация файла на основе введенных данных
        var prevSettings = _yContext.Settings;

        _yContext.Settings =
            _yContext.Settings.SetMessage(1, new AiSettings.Message(AiSettings.MessageRole.User, userContent));
        var text = count != 1
            ? """
              Нужно сгенерировать план расстановки иллюстраций для художественного произведения заданного пользователем.
              В план будут входить промпт для генеративного ИИ, который будет рисовать иллюстрации, и позиция иллюстрации в тексте.
              Промпт должен понятно и подробно описывать происходящее на иллюстрации, не менее 7 слов.
              Иллюстрации должны быть равномерно распределены по всему тексту, несколько иллюстраций, идущих подряд, неприемлемо.
              Ответ присылать строго в формате JSON:
              { "prompt": "<Промпт для генерации изображения>", "position": <Номер слова в тексте, после которого нужно вставить картинку> }
              В ответе должен получиться JSON массив из показанных выше объектов.
              Количество картинок должно быть строго 
              """ + count + ", не больше не меньше. \n" +
              "Если количество иллюстраций будет больше чем задано, переполнения будут отсекаться.\n" +
              "Никаких дополнительных украшающих конструкций, только чистый JSON."
            : """
              Нужно сделать промпт для генеративного ИИ, который будет рисовать
              обложку для художественного произведения заданного пользователем.
              [ { "prompt": "<Промпт для генерации изображения>", "position": 0 } ]
              Значение поля position оставить как есть.
              В ответе должен получиться JSON массив из одного объекта.
              Никаких дополнительных украшающих конструкций, только чистый JSON.
              """;
        _yContext.Settings.SetMessage(0, new AiSettings.Message(AiSettings.MessageRole.System, text
        ));
        var answer = await _yContext.Prompt();
        answer = answer.Replace("```\n", "");
        answer = answer.Replace("```", "");
        answer = answer.Replace("```json", "");
        answer = answer.Replace("``` json", "");

        _yContext.Settings = prevSettings;
        Console.WriteLine(answer);
        var prompts = JsonNode.Parse(answer)?
            .AsArray()
            .Select(i => (i["prompt"].ToString(), i["position"].GetValue<int>()))
            .Take(count)
            .ToArray();

        foreach (var pr in prompts)
        {
            Console.Write("*  ");
            Console.WriteLine(pr.Item1);
        }

        var paths = await _yContext.ImageGeneration(prompts.Select(i => i.Item1).ToArray(), _env.WebRootPath);

        var split = userContent.Split(' ');
        var images = paths
            .Select((i, ind) => (path: i, position: prompts[ind].Item2))
            .Select(i =>
            {
                var path = i.path;
                var pos = i.position;
                var k = Math.Clamp(pos, 0, split.Length);
                pos = 0;
                for (var j = 0; j < k; j++)
                {
                    pos += split[j].Length;
                    if (j != k - 1)
                        pos++;
                }

                return (path, position: pos);
            });

        var pdfr = new Pdfer(images, userContent).GeneratePdf();

        return File(pdfr, "application/pdf", "generated_book.pdf");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(
        new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
    );
}