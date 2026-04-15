using RAG.Domain.Entities;

namespace RAG.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken ct = default);
    Task<Document> AddAsync(Document document, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
