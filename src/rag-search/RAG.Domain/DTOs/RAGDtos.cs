namespace RAG.Domain.DTOs;

public class RAGRequest
{
    public string Query { get; set; }
    public int TopK { get; set; } = 5;
    public float SimilarityThreshold { get; set; } = 0.75f;
}

public class RAGResponse
{
    public string Answer { get; set; }
    public List<SearchResult> Sources { get; set; }
}
