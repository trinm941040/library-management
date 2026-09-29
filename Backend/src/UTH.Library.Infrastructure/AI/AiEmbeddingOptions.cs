namespace UTH.Library.Infrastructure.AI;

public sealed class AiEmbeddingOptions
{
    public const string SectionName = "AI";
    public const int DatabaseDimensions = 1536;
    public string Provider { get; init; } = "OpenAI";
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; init; } = DatabaseDimensions;
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
    public string ApiKey { get; init; } = string.Empty;

    public static bool IsValid(AiEmbeddingOptions options) =>
        string.Equals(options.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(options.EmbeddingModel) &&
        options.EmbeddingDimensions == DatabaseDimensions &&
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _);
}
