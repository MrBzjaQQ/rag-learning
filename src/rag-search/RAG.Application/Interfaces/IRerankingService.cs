using RAG.Domain.DTOs;

namespace RAG.Application.Interfaces;

public interface IRerankingService
{
    Task<List<SearchResult>> RerankAsync(string query, List<SearchResult> candidates, int topK, CancellationToken ct = default);
}
