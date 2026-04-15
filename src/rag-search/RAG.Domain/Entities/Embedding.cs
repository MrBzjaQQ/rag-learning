using Pgvector;

namespace RAG.Domain.Entities;

public class Embedding
{
    public string Id { get; set; }
    public string Text { get; set; }
    // public Dictionary<string, object>? MetaData { get; set; }
    public Vector Vector { get; set; }
    public string? DocumentId { get; set; }
    public int? ChunkIndex { get; set; }
    
    public Document? Document { get; set; }
}
