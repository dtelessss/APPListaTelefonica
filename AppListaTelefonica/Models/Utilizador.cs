using System;

namespace AppListaTelefonica.Models;

public class Utilizador
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HashPalavraPasse { get; set; } = string.Empty;
    public CargoUtilizador Cargo { get; set; } = CargoUtilizador.Normal;
    public string? UnidadeOrganica { get; set; }
    public bool EmailVerificado { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? UltimoLoginEm { get; set; }
    
    public bool IsAdministrador => Cargo == CargoUtilizador.Administrador;
}