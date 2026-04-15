namespace RAG.Application.Configuration;

public class LLMOptions
{
    public const string SectionName = "LLM";
    public string BaseUrl { get; set; } = "http://localhost:8033/v1";
    public string ApiKey { get; set; } = "not-used";
    public string ModelName { get; set; } = "llama3";
}
