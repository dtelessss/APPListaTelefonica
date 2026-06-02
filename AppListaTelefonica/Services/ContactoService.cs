using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Services;

public class ContactoService
{
    private readonly DatabaseService _db;
    
    public ContactoService()
    {
        _db = new DatabaseService();
    }
    
    // ==========================================
    // CONTACTOS PÚBLICOS (Admin)
    // ==========================================
    
    public async Task<List<ContactoPublico>> ObterContactosPublicos(string? termoPesquisa = null, string? unidadeOrganica = null)
    {
        return await _db.ObterContactosPublicos(termoPesquisa, unidadeOrganica);
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> CriarContactoPublico(ContactoPublico contacto, Utilizador utilizador)
    {
        if (utilizador.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem gerir contactos publicos.");
        
        if (string.IsNullOrWhiteSpace(contacto.Nome))
            return (false, "O nome e obrigatorio.");
        
        contacto.Id = Guid.NewGuid();
        contacto.CriadoEm = DateTime.UtcNow;
        
        bool criado = await _db.CriarContactoPublico(contacto);
        return criado ? (true, "Contacto publico criado com sucesso!") : (false, "Erro ao criar contacto.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtualizarContactoPublico(ContactoPublico contacto, Utilizador utilizador)
    {
        if (utilizador.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem gerir contactos publicos.");
        
        if (string.IsNullOrWhiteSpace(contacto.Nome))
            return (false, "O nome e obrigatorio.");
        
        contacto.AtualizadoEm = DateTime.UtcNow;
        
        bool atualizado = await _db.AtualizarContactoPublico(contacto);
        return atualizado ? (true, "Contacto publico atualizado com sucesso!") : (false, "Erro ao atualizar contacto.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> EliminarContactoPublico(Guid id, Utilizador utilizador)
    {
        if (utilizador.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem gerir contactos publicos.");
        
        bool eliminado = await _db.EliminarContactoPublico(id);
        return eliminado ? (true, "Contacto publico eliminado com sucesso!") : (false, "Erro ao eliminar contacto.");
    }
    
    // ==========================================
    // CONTACTOS PRIVADOS (Todos os utilizadores)
    // ==========================================
    
    public async Task<List<ContactoPrivado>> ObterContactosPrivados(Guid utilizadorId, Guid? identificadorId = null)
    {
        return await _db.ObterContactosPrivados(utilizadorId, identificadorId);
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> CriarContactoPrivado(ContactoPrivado contacto)
    {
        if (string.IsNullOrWhiteSpace(contacto.Nome))
            return (false, "O nome e obrigatorio.");
        
        contacto.Id = Guid.NewGuid();
        contacto.CriadoEm = DateTime.UtcNow;
        
        bool criado = await _db.CriarContactoPrivado(contacto);
        return criado ? (true, "Contacto privado criado com sucesso!") : (false, "Erro ao criar contacto.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtualizarContactoPrivado(ContactoPrivado contacto)
    {
        if (string.IsNullOrWhiteSpace(contacto.Nome))
            return (false, "O nome e obrigatorio.");
        
        contacto.AtualizadoEm = DateTime.UtcNow;
        
        bool atualizado = await _db.AtualizarContactoPrivado(contacto);
        return atualizado ? (true, "Contacto privado atualizado com sucesso!") : (false, "Erro ao atualizar contacto.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> EliminarContactoPrivado(Guid id, Guid utilizadorId)
    {
        bool eliminado = await _db.EliminarContactoPrivado(id, utilizadorId);
        return eliminado ? (true, "Contacto privado eliminado com sucesso!") : (false, "Erro ao eliminar contacto.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> EliminarTodosContactosPrivados(Guid utilizadorId)
    {
        bool eliminado = await _db.EliminarTodosContactosPrivados(utilizadorId);
        return eliminado ? (true, "Lista privada eliminada com sucesso!") : (false, "Erro ao eliminar lista privada.");
    }
    
    // ==========================================
    // IDENTIFICADORES
    // ==========================================
    
    public async Task<List<Identificador>> ObterIdentificadores(Guid utilizadorId)
    {
        return await _db.ObterIdentificadores(utilizadorId);
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> CriarIdentificador(Identificador identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador.Nome))
            return (false, "O nome do identificador e obrigatorio.");
        
        identificador.Id = Guid.NewGuid();
        
        bool criado = await _db.CriarIdentificador(identificador);
        return criado ? (true, "Identificador criado com sucesso!") : (false, "Erro ao criar identificador.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtualizarIdentificador(Identificador identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador.Nome))
            return (false, "O nome do identificador e obrigatorio.");
        
        bool atualizado = await _db.AtualizarIdentificador(identificador);
        return atualizado ? (true, "Identificador atualizado com sucesso!") : (false, "Erro ao atualizar identificador.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> EliminarIdentificador(Guid id, Guid utilizadorId)
    {
        bool eliminado = await _db.EliminarIdentificador(id, utilizadorId);
        return eliminado ? (true, "Identificador eliminado com sucesso!") : (false, "Erro ao eliminar identificador.");
    }
}