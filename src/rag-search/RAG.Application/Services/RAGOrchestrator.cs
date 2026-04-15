using Microsoft.Extensions.Logging;
using RAG.Application.Interfaces;
using RAG.Domain.DTOs;
using RAG.Domain.Interfaces;

namespace RAG.Application.Services;

public class RAGOrchestrator : IRAGOrchestrator
{
    private readonly IHybridSearchService _hybridSearchService;
    private readonly IRerankingService _rerankingService;
    private readonly IChatClient _chatClient;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<RAGOrchestrator> _logger;

    public RAGOrchestrator(
        IHybridSearchService hybridSearchService,
        IRerankingService rerankingService,
        IChatClient chatClient,
        IDocumentRepository documentRepository,
        ILogger<RAGOrchestrator> logger)
    {
        _hybridSearchService = hybridSearchService;
        _rerankingService = rerankingService;
        _chatClient = chatClient;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// RAG с чанками (как в прототипе /api/v1/search/rag)
    /// </summary>
    public async Task<RAGResponse> QueryAsync(RAGRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("RAG query started: {Query}", request.Query);
        
        // 1. Hybrid Search
        var hybridOptions = new HybridSearchOptions
        {
            TopK = request.TopK,
            SimilarityThreshold = request.SimilarityThreshold,
            DenseTopK = 50,
            SparseTopK = 50
        };
        
        var searchResults = await _hybridSearchService.SearchAsync(request.Query, hybridOptions, ct);
        
        if (!searchResults.Any())
        {
            _logger.LogWarning("No results found for query: {Query}", request.Query);
            return new RAGResponse
            {
                Answer = "No relevant documents found in the database. Please upload and index documents first.",
                Sources = new List<SearchResult>()
            };
        }
        
        // 2. Reranking
        var rerankedResults = await _rerankingService.RerankAsync(
            request.Query, searchResults, request.TopK, ct);
        
        // 3. Build context from chunks
        var context = string.Join("\n\n", rerankedResults.Select(r => r.Text));
        
        // 4. Generate answer
        var answer = await _chatClient.GenerateAsync(request.Query, context, ct);
        
        _logger.LogInformation("RAG query completed. Found {Count} sources", rerankedResults.Count);
        
        return new RAGResponse
        {
            Answer = answer,
            Sources = rerankedResults
        };
    }

    /// <summary>
    /// RAG с полными документами (как в прототипе /api/v1/search/rag-answer)
    /// </summary>
    public async Task<RAGResponse> QueryWithFullDocumentsAsync(RAGRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("RAG query (full docs) started: {Query}", request.Query);
        
        // 1. Hybrid Search
        var hybridOptions = new HybridSearchOptions
        {
            TopK = request.TopK,
            SimilarityThreshold = request.SimilarityThreshold,
            DenseTopK = 50,
            SparseTopK = 50
        };
        
        var searchResults = await _hybridSearchService.SearchAsync(request.Query, hybridOptions, ct);
        
        if (!searchResults.Any())
        {
            _logger.LogWarning("No results found for query: {Query}", request.Query);
            return new RAGResponse
            {
                Answer = "No relevant documents found in the database. Please upload and index documents first.",
                Sources = new List<SearchResult>()
            };
        }
        
        // 2. Reranking
        var rerankedResults = await _rerankingService.RerankAsync(
            request.Query, searchResults, request.TopK, ct);
        
        // 3. Get unique document IDs
        var uniqueDocumentIds = rerankedResults
            .Select(r => r.DocumentId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
        
        // 4. Read full document content
        var fullContents = new List<string>();
        foreach (var docId in uniqueDocumentIds)
        {
            var doc = await _documentRepository.GetByIdAsync(docId, ct);
            if (doc?.ContentPath != null && File.Exists(doc.ContentPath))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(doc.ContentPath, ct);
                    content = content.Replace("\x00", string.Empty); // Clean NUL characters
                    fullContents.Add(content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read document {DocId}", docId);
                    // Fallback to chunk text
                    var chunkResult = rerankedResults.FirstOrDefault(r => r.DocumentId == docId);
                    if (chunkResult != null)
                    {
                        fullContents.Add(chunkResult.Text);
                    }
                }
            }
        }
        
        // 5. Build context from full documents
        var context = string.Join("\n\n", fullContents);
        
        // 6. Generate answer
        var answer = await _chatClient.GenerateAsync(request.Query, context, ct);
        
        _logger.LogInformation("RAG query (full docs) completed. Found {Count} unique documents", uniqueDocumentIds.Count);
        
        return new RAGResponse
        {
            Answer = answer,
            Sources = rerankedResults
        };
    }
}
