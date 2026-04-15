using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;
using RAG.Application.Interfaces;
using RAG.Domain.DTOs;
using RAG.Domain.Interfaces;
using RAG.Infrastructure.Data;

namespace RAG.Infrastructure.Services;

public class DenseSearchService : ISearchService
{
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly INomicEmbeddingClient _embeddingClient;
    private readonly RagDbContext _context;
    private readonly ILogger<DenseSearchService> _logger;

    public DenseSearchService(
        IEmbeddingRepository embeddingRepository,
        INomicEmbeddingClient embeddingClient,
        RagDbContext context,
        ILogger<DenseSearchService> logger)
    {
        _embeddingRepository = embeddingRepository;
        _embeddingClient = embeddingClient;
        _context = context;
        _logger = logger;
    }

    public async Task<List<SearchResult>> SearchByVectorAsync(string query, int topK = 5, float threshold = 0.75f, CancellationToken ct = default)
    {
        try
        {
            var queryEmbedding = await _embeddingClient.GetEmbeddingAsync(query, ct);
            var queryEmbeddingString = queryEmbedding.ToString();

            var sql = @"
                SELECT 
                    e.""Id"",
                    e.""Text"",
                    e.""vector"",
                    e.""DocumentId"",
                    e.""ChunkIndex"",
                    1 - (e.""vector"" <=> @queryEmbedding) AS ""Similarity""
                FROM ""Embeddings"" e
                WHERE 1 - (e.""vector"" <=> @queryEmbedding) >= @threshold
                ORDER BY e.""vector"" <=> @queryEmbedding
                LIMIT @topK";

            var results = await _context.Embeddings
                .FromSqlRaw(sql, 
                    new NpgsqlParameter("@queryEmbedding", queryEmbedding),
                    new NpgsqlParameter("@threshold", threshold),
                    new NpgsqlParameter("@topK", topK))
                .ToListAsync(ct);

            _logger.LogInformation("Found {Count} similar embeddings for query", results.Count);

            return results.Select(r => new SearchResult
            {
                Text = r.Text,
                // Metadata = r.MetaData,
                Score = CalculateCosineSimilarity(r.Vector, queryEmbedding),
                DocumentId = r.DocumentId,
                ChunkIndex = r.ChunkIndex
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during vector search for query: {Query}", query);
            throw;
        }
    }

    internal static float CalculateCosineSimilarity(Vector v1, Vector v2)
    {
        var array1 = v1.ToArray();
        var array2 = v2.ToArray();
        
        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;
        
        for (int i = 0; i < Math.Min(array1.Length, array2.Length); i++)
        {
            dotProduct += array1[i] * array2[i];
            magnitude1 += array1[i] * array1[i];
            magnitude2 += array2[i] * array2[i];
        }
        
        magnitude1 = (float)Math.Sqrt(magnitude1);
        magnitude2 = (float)Math.Sqrt(magnitude2);
        
        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;
        
        return dotProduct / (magnitude1 * magnitude2);
    }

    public async Task<List<SearchResult>> SearchByKeywordsAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        var escapedQuery = query.Replace("'", "''");
        var sql = $@"
            SELECT 
                ""Id"", ""Text"", ""DocumentId"", ""ChunkIndex"",
                ts_rank(to_tsvector('russian', ""Text""), plainto_tsquery('russian', '{escapedQuery}')) as ""Score""
            FROM ""Embeddings""
            WHERE to_tsvector('russian', ""Text"") @@ plainto_tsquery('russian', '{escapedQuery}')
            ORDER BY ""Score"" DESC
            LIMIT {topK}
        ";

        var rawResults = await _context.Database
            .SqlQueryRaw<KeywordSearchResult>(sql)
            .ToListAsync(ct);

        return rawResults.Select(r => new SearchResult
        {
            Text = r.Text,
            // Metadata = r.MetaData,
            Score = r.Score,
            DocumentId = r.DocumentId,
            ChunkIndex = r.ChunkIndex
        }).ToList();
    }

    private class KeywordSearchResult
    {
        public string Id { get; set; } = default!;
        public string Text { get; set; } = default!;
        // public Dictionary<string, object> MetaData { get; set; } = default!;
        public string DocumentId { get; set; } = default!;
        public int ChunkIndex { get; set; }
        public float Score { get; set; }
    }
}
