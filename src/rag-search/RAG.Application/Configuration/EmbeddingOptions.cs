namespace RAG.Application.Configuration;

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string BaseUrl { get; set; } = "http://localhost:8034/v1";
    public string ApiKey { get; set; } = "not-used";
    public string ModelName { get; set; } = "nomic-ai/nomic-embed-text-v2-moe";
    public int Dimension { get; set; } = 768;
}
