using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public class AiViewModel(Task<string> outputPrompt)
{
    [Required] public Task<string> OutputPrompt { get; init; } = outputPrompt;
}