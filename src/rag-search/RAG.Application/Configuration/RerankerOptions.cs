namespace RAG.Application.Configuration;

public class RerankerOptions
{
    public const string SectionName = "Reranker";
    public string BaseUrl { get; set; } = "http://localhost:8033/v1";
    public string ModelName { get; set; } = "bge-reranker-base";
}
