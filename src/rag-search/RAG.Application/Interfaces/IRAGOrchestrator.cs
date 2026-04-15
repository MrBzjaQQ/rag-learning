using RAG.Domain.DTOs;

namespace RAG.Application.Interfaces;

public interface IRAGOrchestrator
{
    Task<RAGResponse> QueryAsync(RAGRequest request, CancellationToken ct = default);
    Task<RAGResponse> QueryWithFullDocumentsAsync(RAGRequest request, CancellationToken ct = default);
}
