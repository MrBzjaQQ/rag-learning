using Pgvector;
using RAG.Infrastructure.Services;

namespace RAG.Tests.Unit;

public class DenseSearchServiceTests
{
    [Fact]
    public void CalculateCosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        var vec1 = new Vector(new float[] { 1, 0, 0 });
        var vec2 = new Vector(new float[] { 1, 0, 0 });
        
        // Act
        var similarity = DenseSearchService.CalculateCosineSimilarity(vec1, vec2);
        
        // Assert
        Assert.Equal(1.0f, similarity, 0.0001f);
    }
    
    [Fact]
    public void CalculateCosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var vec1 = new Vector(new float[] { 1, 0, 0 });
        var vec2 = new Vector(new float[] { 0, 1, 0 });
        
        // Act
        var similarity = DenseSearchService.CalculateCosineSimilarity(vec1, vec2);
        
        // Assert
        Assert.Equal(0.0f, similarity, 0.0001f);
    }
    
    [Fact]
    public void CalculateCosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        // Arrange
        var vec1 = new Vector(new float[] { 1, 0, 0 });
        var vec2 = new Vector(new float[] { -1, 0, 0 });
        
        // Act
        var similarity = DenseSearchService.CalculateCosineSimilarity(vec1, vec2);
        
        // Assert
        Assert.Equal(-1.0f, similarity, 0.0001f);
    }
}
