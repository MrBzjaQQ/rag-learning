namespace RAG.Api.Models;

public class FileResponse
{
    public string Id { get; set; }
    public string Filename { get; set; }
    public string FileType { get; set; }
    public double FileSize { get; set; }
    public string ContentPath { get; set; }
}
