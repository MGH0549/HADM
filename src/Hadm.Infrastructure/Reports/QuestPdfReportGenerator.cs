using Hadm.Application.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace Hadm.Infrastructure.Reports;

public sealed class QuestPdfReportGenerator : IReportGenerator
{
    private readonly string[] _fontFamilies;

    public QuestPdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var fonts = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "/usr/share/fonts/truetype",
            "/usr/local/share/fonts",
            Environment.GetEnvironmentVariable("HADM_FONT_DIR")
        }.Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)).Distinct();
        foreach (var directory in fonts.OfType<string>())
            FontManager.RegisterFontsFromDirectory(directory);
        var available = FontManager.GetRegisteredFonts()
            .Select(font => font.FamilyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _fontFamilies = new[] { "Arial", "Noto Naskh Arabic", "Noto Sans Arabic", "Segoe UI", "Lato" }
            .Where(available.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (_fontFamilies.Length == 0)
            throw new InvalidOperationException("No supported PDF font is registered. Install Arial or Lato, or configure HADM_FONT_DIR.");
    }

    public byte[] Generate(ReportDocument document) => Document.Create(root => root.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(42);
        page.DefaultTextStyle(x => x.FontFamily(_fontFamilies).FontSize(10).FontColor(Colors.Grey.Darken3));
        page.Header().Column(col =>
        {
            col.Item().Text(document.Title).FontSize(24).Bold().FontColor("#22685E");
            col.Item().Text(document.Subtitle).FontSize(11);
            col.Item().Text(document.Period).FontSize(10).FontColor(Colors.Grey.Darken1);
        });
        page.Content().PaddingVertical(20).Column(col =>
        {
            col.Spacing(14);
            col.Item().Background("#F1F6F3").Padding(14).Column(box =>
            {
                box.Item().Text(document.SummaryHeading).Bold().FontSize(13);
                foreach (var line in document.SummaryLines) box.Item().Text(line);
            });
            foreach (var section in document.Sections)
                col.Item().Column(box =>
                {
                    box.Spacing(4);
                    box.Item().Text(section.Heading).Bold().FontSize(14).FontColor("#22685E");
                    foreach (var line in section.Lines) box.Item().Text(line);
                });
            col.Item().Background("#FFF7EA").Padding(12).Text(document.Disclaimer).Italic();
        });
        page.Footer().AlignCenter().Text(document.Footer).FontSize(9).FontColor(Colors.Grey.Darken1);
        if (document.RightToLeft) page.ContentFromRightToLeft();
    })).GeneratePdf();
}
