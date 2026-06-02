using System;

namespace AppListaTelefonica.Models;

public class ContactoPublico
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NumeroInterno { get; set; }
    public string? Telemovel { get; set; }
    public string? UnidadeOrganica { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}