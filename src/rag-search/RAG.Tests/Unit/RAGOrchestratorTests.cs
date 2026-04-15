using Microsoft.Extensions.Logging;
using Moq;
using RAG.Application.Interfaces;
using RAG.Application.Services;
using RAG.Domain.DTOs;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;

namespace RAG.Tests.Unit;

public class RAGOrchestratorTests
{
    private readonly Mock<IHybridSearchService> _mockHybridSearch;
    private readonly Mock<IRerankingService> _mockReranking;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<IDocumentRepository> _mockDocRepo;
    private readonly RAGOrchestrator _orchestrator;

    public RAGOrchestratorTests()
    {
        _mockHybridSearch = new Mock<IHybridSearchService>();
        _mockReranking = new Mock<IRerankingService>();
        _mockChatClient = new Mock<IChatClient>();
        _mockDocRepo = new Mock<IDocumentRepository>();
        
        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<RAGOrchestrator>();
        
        _orchestrator = new RAGOrchestrator(
            _mockHybridSearch.Object,
            _mockReranking.Object,
            _mockChatClient.Object,
            _mockDocRepo.Object,
            logger);
    }
    
    [Fact]
    public async Task QueryAsync_NoResults_ReturnsEmptyAnswer()
    {
        // Arrange
        _mockHybridSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<HybridSearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());
        
        var request = new RAGRequest { Query = "test query" };
        
        // Act
        var response = await _orchestrator.QueryAsync(request);
        
        // Assert
        Assert.Contains("No relevant documents found", response.Answer);
        Assert.Empty(response.Sources);
    }
    
    [Fact]
    public async Task QueryAsync_HasResults_ReturnsWithAnswer()
    {
        // Arrange
        var searchResults = new List<SearchResult>
        {
            new() { Text = "chunk 1", DocumentId = "doc1", Score = 0.9f },
            new() { Text = "chunk 2", DocumentId = "doc2", Score = 0.8f }
        };
        
        _mockHybridSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<HybridSearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        
        _mockReranking.Setup(r => r.RerankAsync(It.IsAny<string>(), It.IsAny<List<SearchResult>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        
        _mockChatClient.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Generated answer");
        
        var request = new RAGRequest { Query = "test query", TopK = 2 };
        
        // Act
        var response = await _orchestrator.QueryAsync(request);
        
        // Assert
        Assert.Equal("Generated answer", response.Answer);
        Assert.Equal(2, response.Sources.Count);
        _mockChatClient.Verify(c => c.GenerateAsync(
            "test query",
            It.Is<string>(s => s.Contains("chunk 1") && s.Contains("chunk 2")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
    
    [Fact]
    public async Task QueryWithFullDocumentsAsync_ReadsFullDocuments()
    {
        // Arrange
        var searchResults = new List<SearchResult>
        {
            new() { Text = "chunk 1", DocumentId = "doc1", Score = 0.9f }
        };
        
        var fullDoc = new Document
        {
            Id = "doc1",
            ContentPath = "test.txt"
        };
        
        _mockHybridSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<HybridSearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        
        _mockReranking.Setup(r => r.RerankAsync(It.IsAny<string>(), It.IsAny<List<SearchResult>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        
        _mockDocRepo.Setup(r => r.GetByIdAsync("doc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fullDoc);
        
        _mockChatClient.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Full doc answer");
        
        var request = new RAGRequest { Query = "test query" };
        
        // Act
        var response = await _orchestrator.QueryWithFullDocumentsAsync(request);
        
        // Assert
        _mockDocRepo.Verify(r => r.GetByIdAsync("doc1", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Full doc answer", response.Answer);
    }
}
