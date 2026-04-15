using Microsoft.EntityFrameworkCore;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;

namespace RAG.Infrastructure.Data.Repositories;

public class EmbeddingRepository : IEmbeddingRepository
{
    private readonly RagDbContext _context;
    
    public EmbeddingRepository(RagDbContext context) => _context = context;
    
    public async Task<Embedding?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Embeddings.FindAsync(new object[] { id }, ct);
    
    public async Task<IEnumerable<Embedding>> GetByDocumentIdAsync(string documentId, CancellationToken ct = default) =>
        await _context.Embeddings.Where(e => e.DocumentId == documentId).ToListAsync(ct);
    
    public async Task<Embedding> AddAsync(Embedding embedding, CancellationToken ct = default)
    {
        await _context.Embeddings.AddAsync(embedding, ct);
        return embedding;
    }
    
    public async Task<IEnumerable<Embedding>> AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken ct = default)
    {
        await _context.Embeddings.AddRangeAsync(embeddings, ct);
        return embeddings;
    }
    
    public async Task DeleteByDocumentIdAsync(string documentId, CancellationToken ct = default)
    {
        var embeddings = await GetByDocumentIdAsync(documentId, ct);
        _context.Embeddings.RemoveRange(embeddings);
    }
    
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
