using System.Net.Http.Json;
using System.Text.Json;
using Hadm.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hadm.Infrastructure.Services;

public sealed class AnthropicAiService(HttpClient http, IConfiguration configuration, ILogger<AnthropicAiService> logger) : IAiService
{
    private readonly string? _apiKey = configuration["Ai:Anthropic:ApiKey"];
    private readonly string _model = configuration["Ai:Anthropic:Model"] ?? "claude-haiku-5-5";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string> GenerateJsonAsync(string systemPrompt, string inputJson, object jsonSchema, int maxOutputTokens, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("The AI provider is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(new
        {
            model = _model,
            max_tokens = maxOutputTokens,
            system = systemPrompt,
            output_config = new { format = new { type = "json_schema", schema = jsonSchema } },
            messages = new[] { new { role = "user", content = inputJson } }
        });

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("AI provider returned status {StatusCode}", response.StatusCode);
            throw new HttpRequestException("The AI service is temporarily unavailable.");
        }
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var text = payload.RootElement.GetProperty("content").EnumerateArray()
            .Where(block => block.TryGetProperty("type", out var type) && type.GetString() == "text")
            .Select(block => block.GetProperty("text").GetString()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("The AI service returned no text.");
        return text;
    }
}
