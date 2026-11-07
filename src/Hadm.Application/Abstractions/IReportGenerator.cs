namespace Hadm.Application.Abstractions;

public interface IReportGenerator
{
    byte[] Generate(ReportDocument document);
}

public sealed record ReportDocument(
    bool RightToLeft,
    string Title,
    string Subtitle,
    string Period,
    string Disclaimer,
    string SummaryHeading,
    IReadOnlyList<string> SummaryLines,
    IReadOnlyList<ReportSection> Sections,
    string Footer);

public sealed record ReportSection(string Heading, IReadOnlyList<string> Lines);
