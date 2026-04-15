using RAG.Domain.Entities;

namespace RAG.Domain.Interfaces;

public interface IEmbeddingRepository
{
    Task<Embedding?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<Embedding>> GetByDocumentIdAsync(string documentId, CancellationToken ct = default);
    Task<Embedding> AddAsync(Embedding embedding, CancellationToken ct = default);
    Task<IEnumerable<Embedding>> AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken ct = default);
    Task DeleteByDocumentIdAsync(string documentId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
