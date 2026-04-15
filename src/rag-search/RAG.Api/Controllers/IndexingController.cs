using Microsoft.AspNetCore.Mvc;
using RAG.Application.Interfaces;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/v1/indexing")]
public class IndexingController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly INomicEmbeddingClient _embeddingClient;
    private readonly ILogger<IndexingController> _logger;

    public IndexingController(
        IDocumentRepository documentRepository,
        IEmbeddingRepository embeddingRepository,
        INomicEmbeddingClient embeddingClient,
        ILogger<IndexingController> logger)
    {
        _documentRepository = documentRepository;
        _embeddingRepository = embeddingRepository;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/v1/indexing/documents/{file_id}/index - Индексировать документ
    /// </summary>
    [HttpPost("documents/{fileId}/index")]
    public async Task<ActionResult> IndexDocument(string fileId)
    {
        try
        {
            var doc = await _documentRepository.GetByIdAsync(fileId);
            if (doc == null)
                return NotFound("File not found");
            
            // Read file content
            if (!System.IO.File.Exists(doc.ContentPath))
                return NotFound("File content not found");
            
            var content = await System.IO.File.ReadAllTextAsync(doc.ContentPath);
            content = content.Replace("\0", string.Empty); // Clean NUL characters
            
            // Chunk text
            var chunks = ChunkText(content);
            _logger.LogInformation("Chunked into {Count} parts", chunks.Count);
            
            // Generate embeddings in batch
            var embeddings = await _embeddingClient.GetEmbeddingsBatchAsync(chunks);
            
            // Save embeddings
            for (int i = 0; i < chunks.Count; i++)
            {
                var embedding = new Embedding
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = chunks[i],
                    // MetaData = BuildMetadata(doc, chunks[i], i),
                    Vector = embeddings[i],
                    DocumentId = fileId,
                    ChunkIndex = i
                };
                await _embeddingRepository.AddAsync(embedding);
            }
            
            await _embeddingRepository.SaveChangesAsync();
            
            // Mark document as indexed
            doc.IsIndexed = true;
            await _documentRepository.SaveChangesAsync();
            
            return Ok(new { Success = true, Message = $"Document {fileId} indexed successfully", Chunks = chunks.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {FileId}", fileId);
            return StatusCode(500, $"Error indexing document: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/v1/indexing/all - Индексировать все документы
    /// </summary>
    [HttpPost("all")]
    public async Task<ActionResult> IndexAllDocuments()
    {
        var docs = await _documentRepository.GetAllAsync();
        var unindexed = docs.Where(d => !d.IsIndexed).ToList();
        
        if (!unindexed.Any())
            return Ok(new { Message = "No unindexed documents found" });
        
        _logger.LogInformation("Starting indexing for {Count} documents", unindexed.Count);
        
        foreach (var doc in unindexed)
        {
            try
            {
                await IndexDocumentInternal(doc);
                doc.IsIndexed = true;
                await _documentRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document {DocId}", doc.Id);
            }
        }
        
        return Ok(new { Message = $"Indexed {unindexed.Count} documents" });
    }
    
    private async Task IndexDocumentInternal(Document doc)
    {
        var content = await System.IO.File.ReadAllTextAsync(doc.ContentPath);
        content = content.Replace("\0", string.Empty);
        
        var chunks = ChunkText(content);
        var embeddings = await _embeddingClient.GetEmbeddingsBatchAsync(chunks);
        
        for (int i = 0; i < chunks.Count; i++)
        {
            await _embeddingRepository.AddAsync(new Embedding
            {
                Id = Guid.NewGuid().ToString(),
                Text = chunks[i],
                // MetaData = BuildMetadata(doc, chunks[i], i),
                Vector = embeddings[i],
                DocumentId = doc.Id,
                ChunkIndex = i
            });
        }
        
        await _embeddingRepository.SaveChangesAsync();
    }
    
    private List<string> ChunkText(string text)
    {
        var chunks = new List<string>();
        var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        const int maxChunkSize = 500;
        var currentChunk = new List<string>();
        var currentLength = 0;
        
        foreach (var word in words)
        {
            if (currentLength + word.Length + 1 > maxChunkSize && currentChunk.Any())
            {
                chunks.Add(string.Join(" ", currentChunk));
                currentChunk = new List<string> { word };
                currentLength = word.Length;
            }
            else
            {
                currentChunk.Add(word);
                currentLength += word.Length + 1;
            }
        }
        
        if (currentChunk.Any())
        {
            chunks.Add(string.Join(" ", currentChunk));
        }
        
        return chunks;
    }
    
    private Dictionary<string, object> BuildMetadata(Document doc, string text, int chunkIndex)
    {
        return new Dictionary<string, object>
        {
            ["file_path"] = doc.ContentPath,
            ["file_name"] = doc.Filename,
            ["file_type"] = doc.FileType,
            ["file_size"] = doc.FileSize,
            ["document_id"] = doc.Id,
            ["doc_id"] = doc.Id,
            ["ref_doc_id"] = doc.Id,
            ["chunk_index"] = chunkIndex
        };
    }
}
