using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RAG.Domain.Entities;
using Pgvector;

namespace RAG.Infrastructure.Data;

public class RagDbContext : DbContext
{
    public RagDbContext(DbContextOptions<RagDbContext> options) : base(options) {}
    
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Filename).IsRequired();
            entity.Property(e => e.FileType).IsRequired();
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.CreationDate);
            entity.Property(e => e.LastModifiedDate);
            entity.Property(e => e.ContentPath);
            entity.Property(e => e.IsIndexed).HasDefaultValue(false);
        });
        
        // Configure Embedding
        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.Vector)
                  .IsRequired()
                  .HasColumnType("vector")
                  .HasColumnName("vector");
            entity.Property(e => e.ChunkIndex);
            // entity.Ignore(e => e.MetaData);
            
            entity.HasIndex(e => e.DocumentId);
            entity.HasOne(e => e.Document)
                  .WithMany()
                  .HasForeignKey(e => e.DocumentId);
        });
        
        // Create GIN index for full-text search (trigram)
        modelBuilder.Entity<Embedding>().HasIndex(e => e.Text)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // HNSW vector index should be created manually after data is loaded
        // with correct dimensions: CREATE INDEX ON "Embeddings" USING hnsw (vector vector_cosine_ops) WITH (m = 16, ef_construction = 64);
    }
    
 
}
