using System;

namespace AppListaTelefonica.Models;

public class ConfiguracaoApp
{
    public Guid Id { get; set; }
    public string ServidorSmtp { get; set; } = string.Empty;
    public int PortaSmtp { get; set; } = 587;
    public string UtilizadorSmtp { get; set; } = string.Empty;
    public string PalavraPasseSmtp { get; set; } = string.Empty;
    public int DuracaoSessaoDias { get; set; } = 7;
    public string? ServidorBd { get; set; }
    public int? PortaBd { get; set; }
    public string? NomeBd { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}