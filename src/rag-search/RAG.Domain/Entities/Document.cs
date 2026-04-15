namespace RAG.Domain.Entities;

public class Document
{
    public string Id { get; set; }
    public string Filename { get; set; }
    public string FileType { get; set; }
    public double FileSize { get; set; }
    public string? CreationDate { get; set; }
    public string? LastModifiedDate { get; set; }
    public string? ContentPath { get; set; }
    public bool IsIndexed { get; set; }
}
