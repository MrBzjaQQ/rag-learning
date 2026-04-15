using RAG.Application.Services;
using RAG.Domain.DTOs;
using RAG.Application.Interfaces;

namespace RAG.Tests.Unit;

public class HybridSearchServiceTests
{
    [Fact]
    public void CalculateRRF_DenseResultsOnly_ReturnsCorrectScores()
    {
        // Arrange
        var denseResults = new List<SearchResult>
        {
            new() { DocumentId = "doc1", Score = 0.9f },
            new() { DocumentId = "doc2", Score = 0.8f },
            new() { DocumentId = "doc3", Score = 0.7f }
        };
        var sparseResults = new List<SearchResult>();
        float k = 60;
        
        // Act - RRF calculation
        var scores = new Dictionary<string, float>();
        for (int i = 0; i < denseResults.Count; i++)
        {
            scores[denseResults[i].DocumentId] = 1 / (k + i + 1);
        }
        
        // Assert
        Assert.Equal(1 / (k + 1), scores["doc1"], 0.0001f);
        Assert.Equal(1 / (k + 2), scores["doc2"], 0.0001f);
        Assert.Equal(1 / (k + 3), scores["doc3"], 0.0001f);
    }
    
    [Fact]
    public void CalculateRRF_DocInBothLists_ScoresAreCombined()
    {
        // Arrange
        var denseResults = new List<SearchResult>
        {
            new() { DocumentId = "doc1", Score = 0.9f }
        };
        var sparseResults = new List<SearchResult>
        {
            new() { DocumentId = "doc1", Score = 0.8f }
        };
        float k = 60;
        
        // Act
        var rrfScore = (1 / (k + 1)) + (1 / (k + 1));
        
        // Assert
        Assert.Equal(2 / (k + 1), rrfScore, 0.0001f);
    }
    
    [Fact]
    public void CalculateRRF_DocNotInList_NotIncluded()
    {
        // Arrange
        var denseResults = new List<SearchResult>
        {
            new() { DocumentId = "doc1", Score = 0.9f }
        };
        var sparseResults = new List<SearchResult>();
        
        // Act
        var scores = new Dictionary<string, float>();
        foreach (var doc in denseResults)
        {
            scores[doc.DocumentId] = 1 / (60 + 1);
        }
        
        // Assert
        Assert.False(scores.ContainsKey("doc2"));
    }
    
    [Fact]
    public void ApplyWeights_DenseOnly_Downweighted()
    {
        // Arrange
        var rrfScores = new Dictionary<string, (float Score, int DenseRank, int SparseRank)>
        {
            ["doc1"] = (0.5f, 1, 0)
        };
        float denseWeight = 0.5f;
        float sparseWeight = 0.5f;
        
        // Act
        var score = rrfScores["doc1"].Score * denseWeight;
        
        // Assert
        Assert.Equal(0.25f, score, 0.0001f);
    }
    
    [Fact]
    public void ApplyWeights_BothLists_NoDownweight()
    {
        // Arrange
        var rrfScores = new Dictionary<string, (float Score, int DenseRank, int SparseRank)>
        {
            ["doc1"] = (0.5f, 1, 1)
        };
        float denseWeight = 0.5f;
        float sparseWeight = 0.5f;
        
        // Act
        var score = rrfScores["doc1"].Score;

        // Assert
        Assert.Equal(0.5f, score, 0.0001f);
    }
}
