using Microsoft.EntityFrameworkCore;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;

namespace RAG.Infrastructure.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly RagDbContext _context;
    
    public DocumentRepository(RagDbContext context) => _context = context;
    
    public async Task<Document?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Documents.FindAsync(new object[] { id }, ct);
    
    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Documents.ToListAsync(ct);
    
    public async Task<Document> AddAsync(Document document, CancellationToken ct = default)
    {
        await _context.Documents.AddAsync(document, ct);
        return document;
    }
    
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var doc = await GetByIdAsync(id, ct);
        if (doc != null) _context.Documents.Remove(doc);
    }
    
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
