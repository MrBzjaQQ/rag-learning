using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using RAG.Api.Models;
using RAG.Domain.Entities;
using RAG.Domain.Interfaces;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/v1/file")]
public class FilesController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IDocumentRepository documentRepository,
        ILogger<FilesController> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/v1/file - Загрузка файлов
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<List<FileResponse>>> UploadFiles([FromForm] List<IFormFile> files)
    {
        try
        {
            var uploaded = new List<FileResponse>();
            
            foreach (var file in files)
            {
                var fileId = Guid.NewGuid().ToString();
                var extension = Path.GetExtension(file.FileName);
                var safeFileName = $"{fileId}{extension}";
                var filePath = Path.Combine("uploads", safeFileName);
                
                Directory.CreateDirectory("uploads");
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                var doc = new Document
                {
                    Id = fileId,
                    Filename = file.FileName,
                    FileType = file.ContentType ?? "application/octet-stream",
                    FileSize = file.Length,
                    ContentPath = filePath,
                    IsIndexed = false
                };
                
                await _documentRepository.AddAsync(doc);
                uploaded.Add(new FileResponse
                {
                    Id = doc.Id,
                    Filename = doc.Filename,
                    FileType = doc.FileType,
                    FileSize = doc.FileSize,
                    ContentPath = doc.ContentPath
                });
            }
            
            await _documentRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFile), new { fileId = uploaded.First().Id }, uploaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading files");
            return StatusCode(500, $"Error uploading files: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/v1/file/upload-zip - Загрузка ZIP архива
    /// </summary>
    [HttpPost("upload-zip")]
    public async Task<ActionResult<List<FileResponse>>> UploadZipArchive(IFormFile zipFile)
    {
        try
        {
            var uploaded = new List<FileResponse>();
            var uploadDir = "uploads";
            Directory.CreateDirectory(uploadDir);
            
            using (var stream = new FileStream(Path.Combine(uploadDir, zipFile.FileName), FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }
            
            var zipPath = Path.Combine(uploadDir, zipFile.FileName);
            
            using (var zip = new ZipArchive(System.IO.File.OpenRead(zipPath)))
            {
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    var fileId = Guid.NewGuid().ToString();
                    var extension = Path.GetExtension(entry.Name);
                    var safeFileName = $"{fileId}{extension}";
                    var filePath = Path.Combine(uploadDir, safeFileName);
                    
                    entry.ExtractToFile(filePath, true);
                    
                    var doc = new Document
                    {
                        Id = fileId,
                        Filename = entry.FullName,
                        FileType = extension.TrimStart('.') ?? "application/octet-stream",
                        FileSize = new FileInfo(filePath).Length,
                        ContentPath = filePath,
                        IsIndexed = false
                    };
                    
                    await _documentRepository.AddAsync(doc);
                    uploaded.Add(new FileResponse
                    {
                        Id = doc.Id,
                        Filename = doc.Filename,
                        FileType = doc.FileType,
                        FileSize = doc.FileSize,
                        ContentPath = doc.ContentPath
                    });
                }
            }
            
            await _documentRepository.SaveChangesAsync();
            System.IO.File.Delete(zipPath);
            
            return CreatedAtAction(nameof(GetFile), new { fileId = uploaded.First()?.Id }, uploaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ZIP archive");
            return StatusCode(500, $"Error processing ZIP archive: {ex.Message}");
        }
    }

    /// <summary>
    /// GET /api/v1/file/{fileId} - Метаданные файла
    /// </summary>
    [HttpGet("{fileId}")]
    public async Task<ActionResult<FileResponse>> GetFile(string fileId)
    {
        var doc = await _documentRepository.GetByIdAsync(fileId);
        if (doc == null)
            return NotFound("File not found");
        
        return Ok(new FileResponse
        {
            Id = doc.Id,
            Filename = doc.Filename,
            FileType = doc.FileType,
            FileSize = doc.FileSize,
            ContentPath = doc.ContentPath
        });
    }

    /// <summary>
    /// GET /api/v1/file/{fileId}/download - Скачать файл
    /// </summary>
    [HttpGet("{fileId}/download")]
    public async Task<IActionResult> DownloadFile(string fileId)
    {
        var doc = await _documentRepository.GetByIdAsync(fileId);
        if (doc == null || !System.IO.File.Exists(doc.ContentPath))
            return NotFound("File not found");
        
        var memory = new MemoryStream();
        using (var stream = new FileStream(doc.ContentPath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        
        return File(memory, doc.FileType, doc.Filename);
    }

    /// <summary>
    /// DELETE /api/v1/file/{fileId} - Удалить файл
    /// </summary>
    [HttpDelete("{fileId}")]
    public async Task<ActionResult<FileDeleteResponse>> DeleteFile(string fileId)
    {
        var doc = await _documentRepository.GetByIdAsync(fileId);
        if (doc != null)
        {
            if (System.IO.File.Exists(doc.ContentPath))
                System.IO.File.Delete(doc.ContentPath);
            
            await _documentRepository.DeleteAsync(fileId);
            await _documentRepository.SaveChangesAsync();
        }
        
        return Ok(new FileDeleteResponse
        {
            Success = true,
            Message = $"File {fileId} deleted successfully (or was already deleted)"
        });
    }
}
