using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RAG.Application.Configuration;
using RAG.Application.Interfaces;

namespace RAG.Infrastructure.LLM;

public class OpenAIChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly LLMOptions _options;
    
    public OpenAIChatClient(HttpClient httpClient, IOptions<LLMOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }
    
    public async Task<string> GenerateAsync(string query, string context, CancellationToken ct = default)
    {
        var prompt = $"Based on the following context, please answer the question.\n\nContext:\n{context}\n\nQuestion:\n{query}\n\nAnswer:";
        
        var request = new
        {
            model = _options.ModelName,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant that answers questions based on the provided context." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 8192
        };
        
        var response = await _httpClient.PostAsJsonAsync("chat/completions", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
        return result.Choices[0].Message.Content;
    }
    
    private class ChatResponse
    {
        public List<ChatChoice> Choices { get; set; } = new();
    }
    
    private class ChatChoice
    {
        public ChatMessage Message { get; set; } = new();
    }
    
    private class ChatMessage
    {
        public string Content { get; set; } = "";
    }
}
