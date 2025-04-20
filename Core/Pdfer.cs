using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Core;

public class Pdfer
{
    private readonly IReadOnlyCollection<(string path, int position)> _images;
    private readonly string _originalText;

    public Pdfer(IEnumerable<(string path, int position)> images, string originalText)
    {
        _images = images.OrderBy(i => i.position).ToArray();
        _originalText = originalText;
    }

    public byte[] GeneratePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(12));

                page.Content()
                    .Column(column =>
                    {
                        var currentTextPosition = 0;

                        foreach (var image in _images)
                        {
                            // Обрабатываем текст до текущего изображения
                            var imagePosition = Math.Clamp(image.position, 0, _originalText.Length);

                            if (imagePosition > currentTextPosition)
                            {
                                var textSegment = _originalText.Substring(
                                    currentTextPosition,
                                    imagePosition - currentTextPosition
                                );

                                column.Item().Text(textSegment);
                            }

                            // Вставляем изображение
                            column.Item()
                                .Image(image.path).FitArea();
                            currentTextPosition = imagePosition;
                        }

                        // Добавляем оставшийся текст
                        if (currentTextPosition < _originalText.Length)
                        {
                            var remainingText = _originalText[currentTextPosition..];
                            column.Item().Text(remainingText);
                        }
                    });
            });
        }).GeneratePdf();
    }
}