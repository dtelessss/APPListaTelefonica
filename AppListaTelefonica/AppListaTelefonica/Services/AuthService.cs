using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;  // ← IMPORTANTE: BCrypt.Net
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Services
{
    public class AuthService
    {
        private readonly DatabaseService _db;
        private Utilizador? _utilizadorAtual;

        public AuthService()
        {
            _db = new DatabaseService();
        }

        // Validação de palavra-passe
        private (bool IsValid, string Message) ValidarPalavraPasse(string palavraPasse)
        {
            if (string.IsNullOrWhiteSpace(palavraPasse))
                return (false, "Palavra-passe não pode estar vazia");

            if (palavraPasse.Length < 12)
                return (false, "Palavra-passe deve ter pelo menos 12 caracteres");

            bool temMaiuscula = palavraPasse.Any(char.IsUpper);
            bool temMinuscula = palavraPasse.Any(char.IsLower);
            bool temNumero = palavraPasse.Any(char.IsDigit);
            bool temEspecial = palavraPasse.Any(ch => !char.IsLetterOrDigit(ch));

            int tipos = new[] { temMaiuscula, temMinuscula, temNumero, temEspecial }.Count(x => x);

            if (tipos < 3)
            {
                return (false, "Palavra-passe deve conter pelo menos 3 dos 4 tipos:\n" +
                              "- Letras maiúsculas (A-Z)\n" +
                              "- Letras minúsculas (a-z)\n" +
                              "- Números (0-9)\n" +
                              "- Caracteres especiais (!@#$%)");
            }

            return (true, "Palavra-passe válida");
        }

        // REGISTO
        public async Task<(bool Sucesso, string Mensagem, Guid? UtilizadorId)> Registar(string email, string palavraPasse, string? unidadeOrganica = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Email inválido", null);

                var validacao = ValidarPalavraPasse(palavraPasse);
                if (!validacao.IsValid)
                    return (false, validacao.Message, null);

                bool emailExiste = await _db.VerificarExistenciaUtilizadorPorEmail(email);
                if (emailExiste)
                    return (false, "Email já registado", null);

                bool isPrimeiro = await _db.VerificarSePrimeiroUtilizador();
                CargoUtilizador cargo = isPrimeiro ? CargoUtilizador.Administrador : CargoUtilizador.Normal;

                // ✅ BCrypt correto
                string hashPalavraPasse = BCrypt.HashPassword(palavraPasse);

                var novoUtilizador = new Utilizador
                {
                    Id = Guid.NewGuid(),
                    Email = email.ToLower().Trim(),
                    HashPalavraPasse = hashPalavraPasse,
                    Cargo = cargo,
                    UnidadeOrganica = unidadeOrganica,
                    EmailVerificado = false,
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow
                };

                bool criado = await _db.CriarUtilizador(novoUtilizador, hashPalavraPasse);
                if (!criado)
                    return (false, "Erro ao criar conta", null);

                string codigo = await _db.CriarCodigoVerificacao(novoUtilizador.Id, "verificacao_email");
                Console.WriteLine($"[EMAIL] Código para {email}: {codigo}");

                return (true, "Conta criada! Verifique seu email.", novoUtilizador.Id);
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}", null);
            }
        }

        // VERIFICAR EMAIL
        public async Task<(bool Sucesso, string Mensagem)> VerificarEmail(string email, string codigo)
        {
            try
            {
                var utilizador = await _db.ObterUtilizadorPorEmail(email);
                if (utilizador == null)
                    return (false, "Utilizador não encontrado");

                if (utilizador.EmailVerificado)
                    return (false, "Email já verificado");

                bool codigoValido = await _db.VerificarValidadeCodigo(utilizador.Id, codigo, "verificacao_email");
                if (!codigoValido)
                    return (false, "Código inválido ou expirado");

                await _db.AtualizarVerificacaoEmailUtilizador(utilizador.Id);

                return (true, "Email verificado com sucesso!");
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}");
            }
        }

        // REENVIAR CÓDIGO
        public async Task<(bool Sucesso, string Mensagem)> ReenviarCodigoVerificacao(string email)
        {
            try
            {
                var utilizador = await _db.ObterUtilizadorPorEmail(email);
                if (utilizador == null)
                    return (false, "Utilizador não encontrado");

                await _db.EliminarCodigosExpirados(utilizador.Id);
                string novoCodigo = await _db.CriarCodigoVerificacao(utilizador.Id, "verificacao_email");
                Console.WriteLine($"[EMAIL] NOVO CÓDIGO: {novoCodigo}");

                return (true, "Novo código enviado!");
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}");
            }
        }

        // LOGIN
        public async Task<(bool Sucesso, string Mensagem)> Login(string email, string palavraPasse)
        {
            try
            {
                var utilizador = await _db.ObterUtilizadorPorEmail(email.ToLower().Trim());

                if (utilizador == null)
                    return (false, "Email não registado");

                if (!utilizador.EmailVerificado)
                    return (false, "Email não verificado");

                if (!utilizador.Ativo)
                    return (false, "Conta desativada");

                // ✅ BCrypt Verify correto
                bool passwordValida = BCrypt.Verify(palavraPasse, utilizador.HashPalavraPasse);
                if (!passwordValida)
                    return (false, "Palavra-passe incorreta");

                await _db.AtualizarUltimoLoginUtilizador(utilizador.Id);
                _utilizadorAtual = utilizador;

                return (true, "Login realizado!");
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}");
            }
        }

        // RECUPERAÇÃO
        public async Task<(bool Sucesso, string Mensagem)> SolicitarRecuperacaoPalavraPasse(string email)
        {
            try
            {
                var utilizador = await _db.ObterUtilizadorPorEmail(email);
                if (utilizador == null)
                    return (false, "Email não registado");

                await _db.EliminarCodigosExpirados(utilizador.Id);
                string codigo = await _db.CriarCodigoVerificacao(utilizador.Id, "recuperacao_palavra_passe");
                Console.WriteLine($"[EMAIL] CÓDIGO RECUPERAÇÃO: {codigo}");

                return (true, "Código de recuperação enviado!");
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> RedefinirPalavraPasse(string email, string codigo, string novaPalavraPasse)
        {
            try
            {
                var validacao = ValidarPalavraPasse(novaPalavraPasse);
                if (!validacao.IsValid)
                    return (false, validacao.Message);

                var utilizador = await _db.ObterUtilizadorPorEmail(email);
                if (utilizador == null)
                    return (false, "Utilizador não encontrado");

                bool codigoValido = await _db.VerificarValidadeCodigo(utilizador.Id, codigo, "recuperacao_palavra_passe");
                if (!codigoValido)
                    return (false, "Código inválido ou expirado");

                string novoHash = BCrypt.HashPassword(novaPalavraPasse);
                bool atualizado = await _db.AtualizarPalavraPasseUtilizador(utilizador.Id, novoHash);

                if (!atualizado)
                    return (false, "Erro ao redefinir palavra-passe");

                return (true, "Palavra-passe redefinida!");
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}");
            }
        }

        // MÉTODOS DE SESSÃO
        public void Logout() => _utilizadorAtual = null;
        public Utilizador? ObterUtilizadorAtual() => _utilizadorAtual;
        public bool EstaLogado() => _utilizadorAtual != null;
        public bool IsAdministrador() => _utilizadorAtual?.Cargo == CargoUtilizador.Administrador;
    }
}