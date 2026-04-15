using Microsoft.Extensions.Logging;
using RAG.Application.Interfaces;
using RAG.Domain.DTOs;
using RAG.Domain.Interfaces;

namespace RAG.Application.Services;

public class HybridSearchService : IHybridSearchService
{
    private readonly ISearchService _searchService;
    private readonly INomicEmbeddingClient _embeddingClient;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(
        ISearchService searchService,
        INomicEmbeddingClient embeddingClient,
        ILogger<HybridSearchService> logger)
    {
        _searchService = searchService;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, HybridSearchOptions options, CancellationToken ct = default)
    {
        var denseResults = await _searchService.SearchByVectorAsync(
            query, options.DenseTopK, options.SimilarityThreshold, ct);
        
        var sparseResults = await _searchService.SearchByKeywordsAsync(
            query, options.SparseTopK, ct);
        
        _logger.LogInformation("Dense results: {DenseCount}, Sparse results: {SparseCount}", denseResults.Count, sparseResults.Count);
        
        var rrfScores = CalculateRRF(denseResults, sparseResults, options.RrfK);
        
        var combinedResults = ApplyWeights(rrfScores, options.DenseWeight, options.SparseWeight);
        
        return combinedResults
            .OrderByDescending(r => r.Score)
            .Take(options.TopK)
            .ToList();
    }
    
    private Dictionary<string, (float Score, int DenseRank, int SparseRank)> CalculateRRF(
        List<SearchResult> denseResults, 
        List<SearchResult> sparseResults, 
        float k)
    {
        var rrfScores = new Dictionary<string, (float Score, int DenseRank, int SparseRank)>();
        
        for (int i = 0; i < denseResults.Count; i++)
        {
            var docId = denseResults[i].DocumentId;
            if (string.IsNullOrEmpty(docId)) continue;
            
            if (!rrfScores.ContainsKey(docId))
            {
                rrfScores[docId] = (1 / (k + i + 1), i + 1, 0);
            }
            else
            {
                var existing = rrfScores[docId];
                rrfScores[docId] = (existing.Score + 1 / (k + i + 1), i + 1, existing.SparseRank);
            }
        }
        
        for (int i = 0; i < sparseResults.Count; i++)
        {
            var docId = sparseResults[i].DocumentId;
            if (string.IsNullOrEmpty(docId)) continue;
            
            if (!rrfScores.ContainsKey(docId))
            {
                rrfScores[docId] = (1 / (k + i + 1), 0, i + 1);
            }
            else
            {
                var existing = rrfScores[docId];
                rrfScores[docId] = (existing.Score + 1 / (k + i + 1), existing.DenseRank, i + 1);
            }
        }
        
        return rrfScores;
    }
    
    private List<SearchResult> ApplyWeights(
        Dictionary<string, (float Score, int DenseRank, int SparseRank)> rrfScores,
        float denseWeight,
        float sparseWeight)
    {
        var results = new List<SearchResult>();
        
        foreach (var (docId, data) in rrfScores)
        {
            float normalizedScore = data.Score;
            
            if (data.DenseRank > 0 && data.SparseRank == 0)
            {
                normalizedScore *= denseWeight;
            }
            else if (data.DenseRank == 0 && data.SparseRank > 0)
            {
                normalizedScore *= sparseWeight;
            }
            
            results.Add(new SearchResult
            {
                DocumentId = docId,
                Score = normalizedScore
            });
        }
        
        return results;
    }
}
