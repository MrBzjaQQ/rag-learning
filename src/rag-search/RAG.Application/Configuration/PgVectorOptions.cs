namespace RAG.Application.Configuration;

public class PgVectorOptions
{
    public const string SectionName = "PgVector";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 15432;
    public string Database { get; set; } = "rag_db";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "postgres";
    
    public string GetConnectionString() => 
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}
