using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using RAG.Application.Interfaces;
using RAG.Domain.DTOs;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IHybridSearchService _hybridSearchService;
    private readonly IRAGOrchestrator _orchestrator;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        IHybridSearchService hybridSearchService,
        IRAGOrchestrator orchestrator,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _hybridSearchService = hybridSearchService;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/v1/search/documents - Векторный поиск
    /// </summary>
    [HttpPost("documents")]
    public async Task<ActionResult<SearchResponse>> SearchDocuments([FromBody] SearchRequest request)
    {
        try
        {
            var results = await _searchService.SearchByVectorAsync(
                request.Query, 
                request.TopK, 
                request.SimilarityThreshold);
            
            return Ok(new SearchResponse
            {
                Results = results,
                TotalCount = results.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(500, $"Error searching documents: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/v1/search/rag - RAG с чанками
    /// </summary>
    [HttpPost("rag")]
    public async Task<ActionResult<RAGResponse>> Rag([FromBody] RAGRequest request)
    {
        try
        {
            var response = await _orchestrator.QueryAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing RAG query");
            return StatusCode(500, $"Error performing RAG query: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/v1/search/rag-answer - RAG с полными документами
    /// </summary>
    [HttpPost("rag-answer")]
    [RequestTimeout(600000)]
    public async Task<ActionResult<RAGResponse>> RagAnswer([FromBody] RAGRequest request)
    {
        try
        {
            var response = await _orchestrator.QueryWithFullDocumentsAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing RAG answer");
            return StatusCode(500, $"Error performing RAG answer: {ex.Message}");
        }
    }
}
