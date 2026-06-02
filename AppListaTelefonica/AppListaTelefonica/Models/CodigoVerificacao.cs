using System;

namespace AppListaTelefonica.Models;

public class CodigoVerificacao
{
    public Guid Id { get; set; }
    public Guid UtilizadorId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Proposito { get; set; } = string.Empty; 
    public DateTime ExpiraEm { get; set; }
    public bool Utilizado { get; set; }
    public DateTime CriadoEm { get; set; }
    
    // Propriedades auxiliares
    public bool EstaExpirado => DateTime.UtcNow > ExpiraEm;
    public bool IsValido => !Utilizado && !EstaExpirado;
}