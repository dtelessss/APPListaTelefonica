using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Services;

public class AdminService
{
    private readonly DatabaseService _db;
    
    public AdminService()
    {
        _db = new DatabaseService();
    }
    
    // ==========================================
    // GESTÃO DE UTILIZADORES
    // ==========================================
    
    public async Task<List<Utilizador>> ListarUtilizadores(Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return new List<Utilizador>();
        
        return await _db.ObterTodosUtilizadores();
    }
    
    public async Task<List<Utilizador>> ListarUtilizadoresPorCargo(CargoUtilizador cargo, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return new List<Utilizador>();
        
        return await _db.ObterUtilizadoresPorCargo(cargo);
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AlterarCargoUtilizador(Guid utilizadorId, CargoUtilizador novoCargo, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem alterar cargos.");
        
        if (utilizadorId == admin.Id)
            return (false, "Nao pode alterar o seu proprio cargo.");
        
        bool atualizado = await _db.AtualizarCargoUtilizador(utilizadorId, novoCargo);
        return atualizado ? (true, "Cargo atualizado com sucesso!") : (false, "Erro ao atualizar cargo.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtivarDesativarUtilizador(Guid utilizadorId, bool ativo, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem gerir utilizadores.");
        
        if (utilizadorId == admin.Id)
            return (false, "Nao pode desativar a sua propria conta.");
        
        bool atualizado = await _db.AtualizarEstadoAtivoUtilizador(utilizadorId, ativo);
        return atualizado ? (true, $"Utilizador {(ativo ? "ativado" : "desativado")} com sucesso!") : (false, "Erro ao alterar estado.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> EliminarUtilizador(Guid utilizadorId, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem eliminar utilizadores.");
        
        if (utilizadorId == admin.Id)
            return (false, "Nao pode eliminar a sua propria conta.");
        
        bool eliminado = await _db.EliminarUtilizador(utilizadorId);
        return eliminado ? (true, "Utilizador eliminado com sucesso!") : (false, "Erro ao eliminar utilizador.");
    }
    
    public async Task<int> ObterTotalUtilizadores(Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return 0;
        
        return await _db.ObterTotalUtilizadores();
    }
    
    // ==========================================
    // CONFIGURAÇÕES DA APLICAÇÃO
    // ==========================================
    
    public async Task<ConfiguracaoApp?> ObterConfiguracoes(Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return null;
        
        return await _db.ObterConfiguracoesApp();
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtualizarConfiguracoesEmail(
        string servidorSmtp, int portaSmtp, string utilizadorSmtp, string palavraPasseSmtp, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem alterar configuracoes.");
        
        if (string.IsNullOrWhiteSpace(servidorSmtp))
            return (false, "O servidor SMTP e obrigatorio.");
        
        if (string.IsNullOrWhiteSpace(utilizadorSmtp))
            return (false, "O utilizador SMTP e obrigatorio.");
        
        var config = await _db.ObterConfiguracoesApp();
        if (config == null)
            return (false, "Configuracoes nao encontradas.");
        
        config.ServidorSmtp = servidorSmtp;
        config.PortaSmtp = portaSmtp;
        config.UtilizadorSmtp = utilizadorSmtp;
        config.PalavraPasseSmtp = palavraPasseSmtp;
        config.AtualizadoEm = DateTime.UtcNow;
        
        bool atualizado = await _db.AtualizarConfiguracoesApp(config);
        return atualizado ? (true, "Configuracoes de email atualizadas com sucesso!") : (false, "Erro ao atualizar configuracoes.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtualizarDuracaoSessao(int dias, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem alterar configuracoes.");
        
        if (dias < 1 || dias > 365)
            return (false, "A duracao da sessao deve ser entre 1 e 365 dias.");
        
        var config = await _db.ObterConfiguracoesApp();
        if (config == null)
            return (false, "Configuracoes nao encontradas.");
        
        config.DuracaoSessaoDias = dias;
        config.AtualizadoEm = DateTime.UtcNow;
        
        bool atualizado = await _db.AtualizarConfiguracoesApp(config);
        return atualizado ? (true, $"Duracao da sessao atualizada para {dias} dias!") : (false, "Erro ao atualizar configuracoes.");
    }
    
    public async Task<(bool Sucesso, string? Mensagem)> AtualizarConfiguracoesBaseDados(
        string servidorBd, int portaBd, string nomeBd, Utilizador admin)
    {
        if (admin.Cargo != CargoUtilizador.Administrador)
            return (false, "Apenas administradores podem alterar configuracoes.");
        
        var config = await _db.ObterConfiguracoesApp();
        if (config == null)
            return (false, "Configuracoes nao encontradas.");
        
        config.ServidorBd = servidorBd;
        config.PortaBd = portaBd;
        config.NomeBd = nomeBd;
        config.AtualizadoEm = DateTime.UtcNow;
        
        bool atualizado = await _db.AtualizarConfiguracoesApp(config);
        return atualizado ? (true, "Configuracoes da base de dados atualizadas com sucesso!") : (false, "Erro ao atualizar configuracoes.");
    }
}