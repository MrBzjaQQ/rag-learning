namespace RAG.Application.Interfaces;

public interface IChatClient
{
    Task<string> GenerateAsync(string query, string context, CancellationToken ct = default);
}
