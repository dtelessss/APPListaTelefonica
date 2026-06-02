using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Services;

public class AuthService
{
    private readonly DatabaseService _db;
    private readonly EmailService _email;
    
    public AuthService()
    {
        _db = new DatabaseService();
        _email = new EmailService();
    }
    
    // ==========================================
    // VALIDAÇÃO DE PALAVRA-PASSE
    // ==========================================
    
    public (bool Valida, string? MensagemErro) ValidarForcaPalavraPasse(string palavraPasse)
    {
        if (string.IsNullOrWhiteSpace(palavraPasse))
            return (false, "A palavra-passe não pode estar vazia.");
        
        if (palavraPasse.Length < 12)
            return (false, "A palavra-passe deve ter pelo menos 12 caracteres.");
        
        int tipos = 0;
        if (Regex.IsMatch(palavraPasse, @"[a-z]")) tipos++;
        if (Regex.IsMatch(palavraPasse, @"[A-Z]")) tipos++;
        if (Regex.IsMatch(palavraPasse, @"[0-9]")) tipos++;
        if (Regex.IsMatch(palavraPasse, @"[^a-zA-Z0-9]")) tipos++;
        
        if (tipos < 3)
            return (false, "A palavra-passe deve conter pelo menos 3 dos seguintes tipos: minúsculas, maiúsculas, números e caracteres especiais.");
        
        return (true, null);
    }
    
    // ==========================================
    // VALIDAÇÃO DE EMAIL
    // ==========================================
    
    public bool ValidarEmailProfissional(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        if (!Regex.IsMatch(email, pattern))
            return false;
        
        string[] dominiosBloqueados = { 
            "@yahoo.com", "@hotmail.com", "@outlook.com", 
            "@live.com", "@aol.com", "@icloud.com", "@me.com", "@mac.com",
            "@protonmail.com", "@mail.com", "@gmx.com", "@yandex.com"
        };
        
        foreach (var dominio in dominiosBloqueados)
        {
            if (email.EndsWith(dominio, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        
        return true;
    }
    
    // ==========================================
    // REGISTO DE UTILIZADOR
    // ==========================================
    
    public async Task<(bool Sucesso, string? MensagemErro, Guid? UtilizadorId)> RegistarUtilizadorAsync(
        string email, string palavraPasse, string? unidadeOrganica = null)
    {
        try
        {
            if (!ValidarEmailProfissional(email))
                return (false, "O email fornecido não é um email profissional válido.", null);
            
            var (valida, mensagemErro) = ValidarForcaPalavraPasse(palavraPasse);
            if (!valida)
                return (false, mensagemErro, null);
            
            bool emailExiste = await _db.VerificarExistenciaUtilizadorPorEmail(email);
            if (emailExiste)
                return (false, "Já existe um utilizador registado com este email.", null);
            
            bool isPrimeiro = await _db.VerificarSePrimeiroUtilizador();
            CargoUtilizador cargo = isPrimeiro ? CargoUtilizador.Administrador : CargoUtilizador.Normal;
            
            string hashPalavraPasse = BCrypt.Net.BCrypt.HashPassword(palavraPasse, BCrypt.Net.BCrypt.GenerateSalt(12));
            
            var utilizador = new Utilizador
            {
                Id = Guid.NewGuid(),
                Email = email,
                Cargo = cargo,
                UnidadeOrganica = unidadeOrganica,
                EmailVerificado = false,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };
            
            bool criado = await _db.CriarUtilizador(utilizador, hashPalavraPasse);
            
            if (!criado)
                return (false, "Erro ao criar o utilizador. Tente novamente.", null);
            
            string codigo = await _db.CriarCodigoVerificacao(utilizador.Id, "verificacao_email");
            await _email.EnviarCodigoVerificacaoEmailAsync(email, codigo);
            await _email.EnviarNotificacaoBoasVindasAsync(email, isPrimeiro);
            
            return (true, isPrimeiro ? 
                "Conta de administrador criada com sucesso! Verifique o seu email para confirmar o registo." :
                "Conta criada com sucesso! Verifique o seu email para confirmar o registo.", 
                utilizador.Id);
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}", null);
        }
    }
    
    // ==========================================
    // VERIFICAÇÃO DE EMAIL
    // ==========================================
    
    public async Task<(bool Sucesso, string? MensagemErro)> VerificarEmailAsync(Guid utilizadorId, string codigo)
    {
        try
        {
            bool utilizadorExiste = await _db.VerificarExistenciaUtilizadorPorId(utilizadorId);
            if (!utilizadorExiste)
                return (false, "Utilizador não encontrado.");
            
            bool jaVerificado = await _db.VerificarEmailConfirmado(utilizadorId);
            if (jaVerificado)
                return (false, "Este email já foi verificado anteriormente.");
            
            bool codigoValido = await _db.VerificarValidadeCodigo(utilizadorId, codigo, "verificacao_email");
            if (!codigoValido)
                return (false, "Código inválido, expirado ou já utilizado.");
            
            bool atualizado = await _db.AtualizarVerificacaoEmailUtilizador(utilizadorId);
            
            return atualizado ? 
                (true, "Email verificado com sucesso!") : 
                (false, "Erro ao verificar email. Tente novamente.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}");
        }
    }
    
    public async Task<(bool Sucesso, string? MensagemErro)> ReenviarCodigoVerificacaoAsync(Guid utilizadorId)
    {
        try
        {
            bool utilizadorExiste = await _db.VerificarExistenciaUtilizadorPorId(utilizadorId);
            if (!utilizadorExiste)
                return (false, "Utilizador não encontrado.");
            
            bool jaVerificado = await _db.VerificarEmailConfirmado(utilizadorId);
            if (jaVerificado)
                return (false, "Este email já foi verificado.");
            
            await _db.EliminarCodigosExpirados(utilizadorId);
            
            string codigo = await _db.CriarCodigoVerificacao(utilizadorId, "verificacao_email");
            
            var utilizador = await _db.ObterUtilizadorPorId(utilizadorId);
            if (utilizador != null)
            {
                await _email.EnviarCodigoVerificacaoEmailAsync(utilizador.Email, codigo);
            }
            
            return (true, "Novo código enviado com sucesso!");
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}");
        }
    }
    
    // ==========================================
    // LOGIN
    // ==========================================
    
    public async Task<(bool Sucesso, string? MensagemErro, Utilizador? Utilizador)> LoginAsync(
        string email, string palavraPasse, bool manterSessao = false)
    {
        try
        {
            var utilizador = await _db.ObterUtilizadorPorEmail(email);
            
            if (utilizador == null)
                return (false, "Email ou palavra-passe incorretos.", null);
            
            if (!utilizador.Ativo)
                return (false, "Esta conta foi desativada. Contacte o administrador.", null);
            
            if (!utilizador.EmailVerificado)
                return (false, "Precisa de verificar o seu email antes de fazer login. Verifique a sua caixa de correio.", null);
            
                bool passwordValida = BCrypt.Net.BCrypt.Verify(palavraPasse, utilizador.HashPalavraPasse);
            
            if (!passwordValida)
                return (false, "Email ou palavra-passe incorretos.", null);
            
            await _db.AtualizarUltimoLoginUtilizador(utilizador.Id);
            
            if (manterSessao)
            {
                await CriarSessaoPersistenteAsync(utilizador.Id);
            }
            
            return (true, null, utilizador);
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}", null);
        }
    }
    
    // ==========================================
    // GESTÃO DE SESSÃO PERSISTENTE
    // ==========================================
    
    private async Task CriarSessaoPersistenteAsync(Guid utilizadorId)
    {
        try
        {
            var config = await _db.ObterConfiguracoesApp();
            int diasSessao = config?.DuracaoSessaoDias ?? 7;
            
            var sessao = new
            {
                UtilizadorId = utilizadorId,
                DataLogin = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddDays(diasSessao)
            };
            
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "AppListaTelefonica");
            
            if (!System.IO.Directory.Exists(appFolder))
                System.IO.Directory.CreateDirectory(appFolder);
            
            string sessionFile = System.IO.Path.Combine(appFolder, ".session");
            string sessionData = System.Text.Json.JsonSerializer.Serialize(sessao);
            
            await System.IO.File.WriteAllTextAsync(sessionFile, sessionData);
        }
        catch
        {
            // Falha ao criar sessão não deve impedir login
        }
    }
    
    public async Task<(bool SessaoValida, Utilizador? Utilizador)> VerificarSessaoPersistenteAsync()
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "AppListaTelefonica");
            string sessionFile = System.IO.Path.Combine(appFolder, ".session");
            
            if (!System.IO.File.Exists(sessionFile))
                return (false, null);
            
            string sessionData = await System.IO.File.ReadAllTextAsync(sessionFile);
            
            var sessao = System.Text.Json.JsonSerializer.Deserialize<dynamic>(sessionData);
            
            if (sessao == null)
                return (false, null);
            
            DateTime expiraEm = sessao.GetProperty("ExpiraEm").GetDateTime();
            
            if (DateTime.UtcNow > expiraEm)
            {
                await TerminarSessaoAsync();
                return (false, null);
            }
            
            Guid utilizadorId = Guid.Parse(sessao.GetProperty("UtilizadorId").GetString());
            var utilizador = await _db.ObterUtilizadorPorId(utilizadorId);
            
            if (utilizador == null || !utilizador.Ativo)
            {
                await TerminarSessaoAsync();
                return (false, null);
            }
            
            return (true, utilizador);
        }
        catch
        {
            await TerminarSessaoAsync();
            return (false, null);
        }
    }
    
    public async Task TerminarSessaoAsync()
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "AppListaTelefonica");
            string sessionFile = System.IO.Path.Combine(appFolder, ".session");
            
            if (System.IO.File.Exists(sessionFile))
                System.IO.File.Delete(sessionFile);
        }
        catch
        {
            // Ignorar erros ao terminar sessão
        }
        
        await Task.CompletedTask;
    }
    
    // ==========================================
    // RECUPERAÇÃO DE PASSWORD
    // ==========================================
    
    public async Task<(bool Sucesso, string? MensagemErro)> SolicitarRecuperacaoPasswordAsync(string email)
    {
        try
        {
            var utilizador = await _db.ObterUtilizadorPorEmail(email);
            
            if (utilizador == null)
                return (false, "Se este email estiver registado, receberá um código de recuperação.");
            
            if (!utilizador.Ativo)
                return (false, "Esta conta foi desativada.");
            
            await _db.EliminarCodigosExpirados(utilizador.Id);
            
            string codigo = await _db.CriarCodigoVerificacao(utilizador.Id, "recuperacao_password");
            await _email.EnviarCodigoRecuperacaoPasswordAsync(email, codigo);
            
            return (true, "Se este email estiver registado, receberá um código de recuperação.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}");
        }
    }
    
    public async Task<(bool Sucesso, string? MensagemErro)> ConfirmarCodigoRecuperacaoAsync(
        string email, string codigo)
    {
        try
        {
            var utilizador = await _db.ObterUtilizadorPorEmail(email);
            
            if (utilizador == null)
                return (false, "Utilizador não encontrado.");
            
            bool codigoValido = await _db.VerificarValidadeCodigo(utilizador.Id, codigo, "recuperacao_password");
            
            if (!codigoValido)
                return (false, "Código inválido, expirado ou já utilizado.");
            
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}");
        }
    }
    
    public async Task<(bool Sucesso, string? MensagemErro)> AlterarPasswordAsync(
        Guid utilizadorId, string novaPalavraPasse)
    {
        try
        {
            var (valida, mensagemErro) = ValidarForcaPalavraPasse(novaPalavraPasse);
            if (!valida)
                return (false, mensagemErro);
            
            string novoHash = BCrypt.Net.BCrypt.HashPassword(novaPalavraPasse, BCrypt.Net.BCrypt.GenerateSalt(12));
            
            bool atualizado = await _db.AtualizarPalavraPasseUtilizador(utilizadorId, novoHash);
            
            if (atualizado)
            {
                var utilizador = await _db.ObterUtilizadorPorId(utilizadorId);
                if (utilizador != null)
                {
                    await _email.EnviarNotificacaoPasswordAlteradaAsync(utilizador.Email);
                }
            }
            
            return atualizado ? 
                (true, "Palavra-passe alterada com sucesso!") : 
                (false, "Erro ao alterar palavra-passe.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}");
        }
    }
    
    public async Task<(bool Sucesso, string? MensagemErro)> RecuperarPasswordAsync(
        string email, string codigo, string novaPalavraPasse)
    {
        try
        {
            var (codigoValido, erroCodigo) = await ConfirmarCodigoRecuperacaoAsync(email, codigo);
            if (!codigoValido)
                return (false, erroCodigo);
            
            var utilizador = await _db.ObterUtilizadorPorEmail(email);
            if (utilizador == null)
                return (false, "Utilizador não encontrado.");
            
            return await AlterarPasswordAsync(utilizador.Id, novaPalavraPasse);
        }
        catch (Exception ex)
        {
            return (false, $"Erro interno: {ex.Message}");
        }
    }
}