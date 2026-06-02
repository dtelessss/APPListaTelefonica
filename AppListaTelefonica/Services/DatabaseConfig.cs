namespace AppListaTelefonica.Services;

public static class DatabaseConfig
{
    
    private static readonly string Host = "localhost";
    private static readonly string Port = "5432";
    private static readonly string Database = "listatelefonica";
    private static readonly string Username = "estagio";
    private static readonly string Password = "nuEQATLZxrG%42Mk";
    
    public static string ConnectionString => 
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
    
    // Opcional: string para produção (comentar/descomentar)
    // public static string ConnectionStringProduction => 
    //     $"Host=192.168.1.100;Port=5432;Database=listatelefonica;Username=estagio;Password=aaa";
}