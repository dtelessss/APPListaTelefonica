using System;

namespace AppListaTelefonica.Models
{
    public class Contacto
    {
        public Guid Id { get; set; }
        public Guid ListId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? UnidadeOrganica { get; set; }
        public string? NumeroInterno { get; set; }
        public string? Empresa { get; set; }
        public string[] Emails { get; set; } = Array.Empty<string>();
        public string[] NumerosContacto { get; set; } = Array.Empty<string>();
        public DateTime CriadoEm { get; set; }

        // Propriedades auxiliares para exibição na DataGrid
        public string EmailsFormatados => Emails != null && Emails.Length > 0 ? string.Join(", ", Emails) : "—";
        public string NumerosContactoFormatados => NumerosContacto != null && NumerosContacto.Length > 0 ? string.Join(", ", NumerosContacto) : "—";
    }
}