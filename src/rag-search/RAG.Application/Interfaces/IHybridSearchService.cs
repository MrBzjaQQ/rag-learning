using RAG.Domain.DTOs;

namespace RAG.Application.Interfaces;

public interface IHybridSearchService
{
    Task<List<SearchResult>> SearchAsync(string query, HybridSearchOptions options, CancellationToken ct = default);
}

public class HybridSearchOptions
{
    public int TopK { get; set; } = 5;
    public float SimilarityThreshold { get; set; } = 0.75f;
    public int DenseTopK { get; set; } = 50;
    public int SparseTopK { get; set; } = 50;
    public float RrfK { get; set; } = 60f;
    public float DenseWeight { get; set; } = 0.5f;
    public float SparseWeight { get; set; } = 0.5f;
}
