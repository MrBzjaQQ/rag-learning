using Pgvector;

namespace RAG.Application.Interfaces;

public interface INomicEmbeddingClient
{
    Task<Vector> GetEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<Vector>> GetEmbeddingsBatchAsync(List<string> texts, CancellationToken ct = default);
}
