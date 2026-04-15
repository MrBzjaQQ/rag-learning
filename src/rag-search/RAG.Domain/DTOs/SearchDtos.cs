namespace RAG.Domain.DTOs;

public class SearchRequest
{
    public string Query { get; set; }
    public int TopK { get; set; } = 5;
    public float SimilarityThreshold { get; set; } = 0.75f;
}

public class SearchResponse
{
    public List<SearchResult> Results { get; set; }
    public int TotalCount { get; set; }
}

public class SearchResult
{
    public string Text { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public float Score { get; set; }
    public string? DocumentId { get; set; }
    public int? ChunkIndex { get; set; }
}
