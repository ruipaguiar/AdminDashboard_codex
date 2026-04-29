namespace AdminDasboard.Infrastructure.Analysis;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string BaseUrl { get; init; } = "https://api.openai.com/";

    public string Model { get; init; } = "gpt-5.4-nano";

    public string? ApiKey { get; init; }

    public int MaxOutputTokens { get; init; } = 900;
}
