using RAG.Domain.DTOs;

namespace RAG.Application.Interfaces;

public interface ISearchService
{
    Task<List<SearchResult>> SearchByVectorAsync(string query, int topK = 5, float threshold = 0.75f, CancellationToken ct = default);
    Task<List<SearchResult>> SearchByKeywordsAsync(string query, int topK = 5, CancellationToken ct = default);
}
