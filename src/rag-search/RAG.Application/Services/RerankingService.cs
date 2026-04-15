using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.Application.Configuration;
using RAG.Application.Interfaces;
using RAG.Domain.DTOs;

namespace RAG.Application.Services;

public class RerankingService : IRerankingService
{
    private readonly HttpClient _httpClient;
    private readonly LLMOptions _options;
    private readonly ILogger<RerankingService> _logger;

    public RerankingService(
        HttpClient httpClient,
        IOptions<LLMOptions> options,
        ILogger<RerankingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<SearchResult>> RerankAsync(string query, List<SearchResult> candidates, int topK, CancellationToken ct = default)
    {
        if (candidates.Count == 0) return candidates;
        
        if (candidates.Count <= topK)
        {
            _logger.LogInformation("Skipping rerank: only {Count} candidates", candidates.Count);
            return candidates;
        }
        
        _logger.LogInformation("Reranking {Count} candidates for query: {Query}", candidates.Count, query);
        
        var rerankResults = new List<(SearchResult Result, float Score)>();
        
        foreach (var candidate in candidates)
        {
            try
            {
                var score = await CalculateRelevanceScore(query, candidate.Text, ct);
                rerankResults.Add((candidate, score));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rerank candidate for document {DocId}", candidate.DocumentId);
                rerankResults.Add((candidate, candidate.Score));
            }
        }
        
        return rerankResults
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .Select(r => 
            {
                r.Result.Score = r.Score;
                return r.Result;
            })
            .ToList();
    }
    
    private async Task<float> CalculateRelevanceScore(string query, string document, CancellationToken ct)
    {
        var prompt = $@"On a scale from 0 to 1, how relevant is the following document to the query?
        
Query: {query}

Document: {document.Substring(0, Math.Min(document.Length, 500))}...

Relevance score (just return a number between 0 and 1):";

        var request = new
        {
            model = _options.ModelName,
            messages = new[]
            {
                new { role = "system", content = "You are a relevance scoring model. Return only a number between 0 and 1." },
                new { role = "user", content = prompt }
            },
            temperature = 0.0,
            max_tokens = 10
        };
        
        var response = await _httpClient.PostAsJsonAsync("chat/completions", request, ct);
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
        
        if (result?.Choices?.Count > 0 && float.TryParse(result.Choices[0].Message.Content, out var score))
        {
            return score;
        }
        
        return 0.5f;
    }
    
    private class ChatResponse 
    { 
        public List<ChatChoice> Choices { get; set; } 
    }
    
    private class ChatChoice 
    { 
        public ChatMessage Message { get; set; } 
    }
    
    private class ChatMessage 
    { 
        public string Content { get; set; } 
    }
}
