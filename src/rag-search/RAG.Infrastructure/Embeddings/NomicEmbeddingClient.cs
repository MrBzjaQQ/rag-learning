using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pgvector;
using RAG.Application.Configuration;
using RAG.Application.Interfaces;

namespace RAG.Infrastructure.Embeddings;

public class NomicEmbeddingClient : INomicEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingOptions _options;
    
    public NomicEmbeddingClient(HttpClient httpClient, IOptions<EmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }
    
    public async Task<Vector> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var request = new { input = text, model = _options.ModelName };
        var response = await _httpClient.PostAsJsonAsync("embeddings", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IList<EmbeddingResponse>>(ct);
        return new Vector(result[0].Embedding[0].ToArray());
    }
    
    public async Task<List<Vector>> GetEmbeddingsBatchAsync(List<string> texts, CancellationToken ct = default)
    {
        var request = new { input = texts, model = _options.ModelName };
        var response = await _httpClient.PostAsJsonAsync("embeddings", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IList<EmbeddingResponse>>(ct);
        return result.SelectMany(d => d.Embedding.Select(x => new Vector(x.ToArray()))).ToList();
    }
    
    private class EmbeddingResponse
    {
        public List<List<float>> Embedding { get; set; } = new();
    }
}
