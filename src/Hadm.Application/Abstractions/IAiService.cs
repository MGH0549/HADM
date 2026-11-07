namespace Hadm.Application.Abstractions;

public interface IAiService
{
    bool IsConfigured { get; }
    Task<string> GenerateJsonAsync(string systemPrompt, string inputJson, object jsonSchema, int maxOutputTokens, CancellationToken cancellationToken);
}
