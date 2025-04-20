using System.ComponentModel.DataAnnotations;
using YandexGPT;

namespace Core.Models;

public class AiViewModel(YandexAi yandexAi)
{
    [Required] public YandexAi YandexAi { get; init; } = yandexAi;
    public string Description { get; set; } = string.Empty;
}