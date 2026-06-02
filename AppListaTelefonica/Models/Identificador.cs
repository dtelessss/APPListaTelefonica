using System;

namespace AppListaTelefonica.Models;

public class Identificador
{
    public Guid Id { get; set; }
    public Guid UtilizadorId { get; set; }
    public string Nome { get; set; } = string.Empty;
}