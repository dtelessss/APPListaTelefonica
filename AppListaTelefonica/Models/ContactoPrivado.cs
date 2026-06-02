using System;

namespace AppListaTelefonica.Models;

public class ContactoPrivado
{
    public Guid Id { get; set; }
    public Guid UtilizadorId { get; set; }
    public Guid? IdentificadorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? NumeroInterno { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    
    // Propriedade auxiliar para mostrar na UI
    public string? IdentificadorNome { get; set; }
}