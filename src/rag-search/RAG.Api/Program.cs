using Microsoft.EntityFrameworkCore;
using RAG.Infrastructure.Data;
using RAG.Infrastructure.Data.Repositories;
using RAG.Domain.Interfaces;
using RAG.Application.Configuration;
using RAG.Application.Interfaces;
using RAG.Infrastructure.Embeddings;
using RAG.Infrastructure.LLM;
using RAG.Infrastructure.Services;
using RAG.Application.Services;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add DbContext with Npgsql and PgVector
builder.Services.AddDbContext<RagDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RagDb"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(RagDbContext).Assembly.FullName);
            npgsqlOptions.UseVector();
        }));

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();

// Search Service
builder.Services.AddScoped<ISearchService, RAG.Infrastructure.Services.DenseSearchService>();
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "RAG API",
        Version = "v1",
        Description = "RAG Application API"
    });
});

// CORS для Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Health Check
builder.Services.AddHealthChecks();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Embedding client
builder.Services.AddHttpClient<INomicEmbeddingClient, NomicEmbeddingClient>(client =>
{
    var options = builder.Configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>();
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(6000);
});

// LLM client
builder.Services.AddHttpClient<IChatClient, OpenAIChatClient>(client =>
{
    var options = builder.Configuration.GetSection(LLMOptions.SectionName).Get<LLMOptions>();
    client.BaseAddress = new Uri(options.BaseUrl);
});

// Reranking service
builder.Services.AddHttpClient<IRerankingService, RerankingService>(client =>
{
    var options = builder.Configuration.GetSection(LLMOptions.SectionName).Get<LLMOptions>();
    client.BaseAddress = new Uri(options.BaseUrl);
});

// RAG Orchestrator
builder.Services.AddScoped<IRAGOrchestrator, RAGOrchestrator>();

// Bind configuration options
builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection(EmbeddingOptions.SectionName));
builder.Services.Configure<LLMOptions>(builder.Configuration.GetSection(LLMOptions.SectionName));
builder.Services.Configure<RerankerOptions>(builder.Configuration.GetSection(RerankerOptions.SectionName));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG API v1");
    });
}

app.UseHttpsRedirection();

// CORS
app.UseCors("AngularCors");

app.UseAuthorization();

app.MapControllers();

// Health Check endpoint
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var ragDbContext = scope.ServiceProvider.GetRequiredService<RagDbContext>();
    ragDbContext.Database.EnsureCreated();
}

app.Run();
