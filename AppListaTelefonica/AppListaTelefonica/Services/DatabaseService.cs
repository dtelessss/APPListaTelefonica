using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    
    public DatabaseService()
    {
        _connectionString = DatabaseConfig.ConnectionString;
    }
    
    // MÉTODO CENTRALIZADO PARA EXECUTAR QUERIES
    
    private async Task<T?> ExecutarQueryUnica<T>(string sql, Func<NpgsqlDataReader, T> mapear, params (string nome, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nome, valor) in parametros)
        {
            cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
        }
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return mapear(reader);
        }
        return default;
    }

    private async Task<List<T>> ExecutarQueryLista<T>(string sql, Func<NpgsqlDataReader, T> mapear, params (string nome, object valor)[] parametros)
    {
        var resultados = new List<T>();
        
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nome, valor) in parametros)
        {
            cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
        }
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultados.Add(mapear(reader));
        }
        
        return resultados;
    }
    
    /// Método genérico para CRUD
    private async Task<int> ExecutarComando(string sql, params (string nome, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nome, valor) in parametros)
        {
            cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
        }
        
        return await cmd.ExecuteNonQueryAsync();
    }
    
    /// Método genérico para executar scalar (COUNT)
    /// 
    private async Task<object?> ExecutarScalar(string sql, params (string nome, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nome, valor) in parametros)
        {
            cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
        }
        
        return await cmd.ExecuteScalarAsync();
    }
    
    // Secção de Utilizadores

    public async Task<Utilizador?> ObterUtilizadorPorId(Guid id)
    {
        const string sql = @"
            SELECT id, email, hash_palavra_passe, cargo, unidade_organica, 
                   email_verificado, ativo, criado_em, atualizado_em, ultimo_login_em
            FROM utilizadores WHERE id = @id";
        
        return await ExecutarQueryUnica(sql, MapearUtilizador, ("@id", id));
    }
    
    public async Task<Utilizador?> ObterUtilizadorPorEmail(string email)
    {
        const string sql = @"
            SELECT id, email, hash_palavra_passe, cargo, unidade_organica, 
                   email_verificado, ativo, criado_em, atualizado_em, ultimo_login_em
            FROM utilizadores WHERE email = @email";
        
        return await ExecutarQueryUnica(sql, MapearUtilizador, ("@email", email));
    }
    
    public async Task<List<Utilizador>> ObterTodosUtilizadores()
    {
        const string sql = @"
            SELECT id, email, cargo, unidade_organica, email_verificado, 
                   ativo, criado_em, ultimo_login_em
            FROM utilizadores ORDER BY criado_em DESC";
        
        return await ExecutarQueryLista(sql, MapearUtilizadorLista);
    }

    public async Task<List<Utilizador>> ObterUtilizadoresPorUnidadeOrganica(string unidadeOrganica)
    {
        const string sql = @"
            SELECT id, email, cargo, unidade_organica, email_verificado, 
                   ativo, criado_em, ultimo_login_em
            FROM utilizadores WHERE unidade_organica = @unidade_organica ORDER BY email";
        
        return await ExecutarQueryLista(sql, MapearUtilizadorLista, ("@unidade_organica", unidadeOrganica));
    }

    public async Task<List<Utilizador>> ObterUtilizadoresPorCargo(CargoUtilizador cargo)
    {
        string cargoStr = cargo == CargoUtilizador.Administrador ? "administrador" : "normal";
        const string sql = @"
            SELECT id, email, cargo, unidade_organica, email_verificado, 
                   ativo, criado_em, ultimo_login_em
            FROM utilizadores WHERE cargo = @cargo ORDER BY email";
        
        return await ExecutarQueryLista(sql, MapearUtilizadorLista, ("@cargo", cargoStr));
    }
 
    public async Task<bool> VerificarExistenciaUtilizadorPorEmail(string email)
    {
        const string sql = "SELECT COUNT(*) FROM utilizadores WHERE email = @email";
        var resultado = await ExecutarScalar(sql, ("@email", email));
        return resultado != null && (long)resultado > 0;
    }

    public async Task<bool> VerificarExistenciaUtilizadorPorId(Guid id)
    {
        const string sql = "SELECT COUNT(*) FROM utilizadores WHERE id = @id";
        var resultado = await ExecutarScalar(sql, ("@id", id));
        return resultado != null && (long)resultado > 0;
    }
 
    public async Task<bool> VerificarSePrimeiroUtilizador()
    {
        const string sql = "SELECT COUNT(*) FROM utilizadores";
        var resultado = await ExecutarScalar(sql);
        return resultado != null && (long)resultado == 0;
    }

    public async Task<bool> VerificarUtilizadorAtivo(Guid id)
    {
        const string sql = "SELECT ativo FROM utilizadores WHERE id = @id";
        var resultado = await ExecutarScalar(sql, ("@id", id));
        return resultado != null && (bool)resultado;
    }

    public async Task<bool> VerificarEmailConfirmado(Guid id)
    {
        const string sql = "SELECT email_verificado FROM utilizadores WHERE id = @id";
        var resultado = await ExecutarScalar(sql, ("@id", id));
        return resultado != null && (bool)resultado;
    }

    public async Task<int> ObterTotalUtilizadores()
    {
        const string sql = "SELECT COUNT(*) FROM utilizadores";
        var resultado = await ExecutarScalar(sql);
        return resultado != null ? Convert.ToInt32(resultado) : 0;
    }

    public async Task<int> ObterTotalUtilizadoresPorCargo(CargoUtilizador cargo)
    {
        string cargoStr = cargo == CargoUtilizador.Administrador ? "administrador" : "normal";
        const string sql = "SELECT COUNT(*) FROM utilizadores WHERE cargo = @cargo";
        var resultado = await ExecutarScalar(sql, ("@cargo", cargoStr));
        return resultado != null ? Convert.ToInt32(resultado) : 0;
    }
 
    public async Task<bool> CriarUtilizador(Utilizador utilizador, string hashPalavraPasse)
    {
        const string sql = @"
            INSERT INTO utilizadores (id, email, hash_palavra_passe, cargo, unidade_organica, 
                                      email_verificado, ativo, criado_em)
            VALUES (@id, @email, @hash_palavra_passe, @cargo, @unidade_organica, 
                    @email_verificado, @ativo, @criado_em)";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizador.Id),
            ("@email", utilizador.Email),
            ("@hash_palavra_passe", hashPalavraPasse),
            ("@cargo", utilizador.Cargo == CargoUtilizador.Administrador ? "administrador" : "normal"),
            ("@unidade_organica", utilizador.UnidadeOrganica),
            ("@email_verificado", utilizador.EmailVerificado),
            ("@ativo", utilizador.Ativo),
            ("@criado_em", utilizador.CriadoEm));
        
        return linhasAfetadas > 0;
    }
    
    public async Task<bool> AtualizarUltimoLoginUtilizador(Guid utilizadorId)
    {
        const string sql = "UPDATE utilizadores SET ultimo_login_em = @ultimo_login_em WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizadorId),
            ("@ultimo_login_em", DateTime.UtcNow));
        
        return linhasAfetadas > 0;
    }

    public async Task<bool> AtualizarVerificacaoEmailUtilizador(Guid utilizadorId)
    {
        const string sql = "UPDATE utilizadores SET email_verificado = true WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql, ("@id", utilizadorId));
        return linhasAfetadas > 0;
    }

    public async Task<bool> AtualizarCargoUtilizador(Guid utilizadorId, CargoUtilizador novoCargo)
    {
        const string sql = "UPDATE utilizadores SET cargo = @cargo, atualizado_em = @atualizado_em WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizadorId),
            ("@cargo", novoCargo == CargoUtilizador.Administrador ? "administrador" : "normal"),
            ("@atualizado_em", DateTime.UtcNow));
        
        return linhasAfetadas > 0;
    }

    public async Task<bool> AtualizarEstadoAtivoUtilizador(Guid utilizadorId, bool ativo)
    {
        const string sql = "UPDATE utilizadores SET ativo = @ativo, atualizado_em = @atualizado_em WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizadorId),
            ("@ativo", ativo),
            ("@atualizado_em", DateTime.UtcNow));
        
        return linhasAfetadas > 0;
    }

    public async Task<bool> AtualizarUnidadeOrganicaUtilizador(Guid utilizadorId, string unidadeOrganica)
    {
        const string sql = "UPDATE utilizadores SET unidade_organica = @unidade_organica, atualizado_em = @atualizado_em WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizadorId),
            ("@unidade_organica", unidadeOrganica),
            ("@atualizado_em", DateTime.UtcNow));
        
        return linhasAfetadas > 0;
    }

    public async Task<bool> AtualizarPalavraPasseUtilizador(Guid utilizadorId, string novoHashPalavraPasse)
    {
        const string sql = "UPDATE utilizadores SET hash_palavra_passe = @hash, atualizado_em = @atualizado_em WHERE id = @id";
    
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizadorId),
            ("@hash", novoHashPalavraPasse),
            ("@atualizado_em", DateTime.UtcNow));
    
        return linhasAfetadas > 0;
    }

    public async Task<bool> EliminarUtilizador(Guid utilizadorId)
    {
        const string sql = "DELETE FROM utilizadores WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql, ("@id", utilizadorId));
        return linhasAfetadas > 0;
    }
    
    // Secção de Códigos de verificação

    public async Task<string> CriarCodigoVerificacao(Guid utilizadorId, string proposito)
    {
        var random = new Random();
        string codigo = random.Next(100000, 999999).ToString();
        
        const string sql = @"
            INSERT INTO codigos_verificacao (id, utilizador_id, codigo, proposito, expira_em, utilizado, criado_em)
            VALUES (@id, @utilizador_id, @codigo, @proposito, @expira_em, false, @criado_em)";
        
        await ExecutarComando(sql,
            ("@id", Guid.NewGuid()),
            ("@utilizador_id", utilizadorId),
            ("@codigo", codigo),
            ("@proposito", proposito),
            ("@expira_em", DateTime.UtcNow.AddMinutes(15)),
            ("@criado_em", DateTime.UtcNow));
        
        return codigo;
    }
    
    /// <summary>Obtém o último código de verificação de um utilizador</summary>
    public async Task<CodigoVerificacao?> ObterUltimoCodigoVerificacao(Guid utilizadorId, string proposito)
    {
        const string sql = @"
            SELECT id, utilizador_id, codigo, proposito, expira_em, utilizado, criado_em
            FROM codigos_verificacao
            WHERE utilizador_id = @utilizador_id AND proposito = @proposito
            ORDER BY criado_em DESC LIMIT 1";
        
        return await ExecutarQueryUnica(sql, MapearCodigoVerificacao,
            ("@utilizador_id", utilizadorId),
            ("@proposito", proposito));
    }
    
    /// <summary>Verifica se um código é válido e não expirado</summary>
    public async Task<bool> VerificarValidadeCodigo(Guid utilizadorId, string codigo, string proposito)
    {
        const string sql = @"
            SELECT id, utilizado, expira_em FROM codigos_verificacao
            WHERE utilizador_id = @utilizador_id AND codigo = @codigo AND proposito = @proposito
            ORDER BY criado_em DESC LIMIT 1";
        
        var resultado = await ExecutarQueryUnica(sql, reader => new
        {
            Id = reader.GetGuid(0),
            Utilizado = reader.GetBoolean(1),
            ExpiraEm = reader.GetDateTime(2)
        }, ("@utilizador_id", utilizadorId), ("@codigo", codigo), ("@proposito", proposito));
        
        if (resultado == null) return false;
        if (resultado.Utilizado) return false;
        if (DateTime.UtcNow > resultado.ExpiraEm) return false;
        
        // Marcar código como utilizado
        await AtualizarCodigoComoUtilizado(resultado.Id);
        
        return true;
    }
    
    /// <summary>Marca um código como utilizado</summary>
    public async Task<bool> AtualizarCodigoComoUtilizado(Guid codigoId)
    {
        const string sql = "UPDATE codigos_verificacao SET utilizado = true WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql, ("@id", codigoId));
        return linhasAfetadas > 0;
    }
    
    /// <summary>Elimina códigos expirados de um utilizador</summary>
    public async Task<int> EliminarCodigosExpirados(Guid utilizadorId)
    {
        const string sql = "DELETE FROM codigos_verificacao WHERE utilizador_id = @utilizador_id AND expira_em < @agora";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@utilizador_id", utilizadorId),
            ("@agora", DateTime.UtcNow));
        
        return linhasAfetadas;
    }
    
    // Secção de Configurações da Aplicação
    
    /// <summary>Obtém as configurações da aplicação</summary>
    public async Task<ConfiguracaoApp?> ObterConfiguracoesApp()
    {
        const string sql = @"
            SELECT id, servidor_smtp, porta_smtp, utilizador_smtp, palavra_passe_smtp, 
                   duracao_sessao_dias, servidor_bd, porta_bd, nome_bd, criado_em, atualizado_em
            FROM configuracoes_app LIMIT 1";
        
        return await ExecutarQueryUnica(sql, MapearConfiguracaoApp);
    }
    
    /// <summary>Verifica se existem configurações na base de dados</summary>
    public async Task<bool> VerificarExistenciaConfiguracoes()
    {
        const string sql = "SELECT COUNT(*) FROM configuracoes_app";
        var resultado = await ExecutarScalar(sql);
        return resultado != null && (long)resultado > 0;
    }
    
    /// <summary>Cria configurações iniciais da aplicação</summary>
    public async Task<bool> CriarConfiguracoesIniciais(ConfiguracaoApp config)
    {
        const string sql = @"
            INSERT INTO configuracoes_app (id, servidor_smtp, porta_smtp, utilizador_smtp, palavra_passe_smtp, 
                                           duracao_sessao_dias, servidor_bd, porta_bd, nome_bd, criado_em, atualizado_em)
            VALUES (@id, @servidor_smtp, @porta_smtp, @utilizador_smtp, @palavra_passe_smtp, 
                    @duracao_sessao_dias, @servidor_bd, @porta_bd, @nome_bd, @criado_em, @atualizado_em)";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", config.Id),
            ("@servidor_smtp", config.ServidorSmtp),
            ("@porta_smtp", config.PortaSmtp),
            ("@utilizador_smtp", config.UtilizadorSmtp),
            ("@palavra_passe_smtp", config.PalavraPasseSmtp),
            ("@duracao_sessao_dias", config.DuracaoSessaoDias),
            ("@servidor_bd", config.ServidorBd ?? (object)DBNull.Value),
            ("@porta_bd", config.PortaBd ?? (object)DBNull.Value),
            ("@nome_bd", config.NomeBd ?? (object)DBNull.Value),
            ("@criado_em", config.CriadoEm),
            ("@atualizado_em", config.AtualizadoEm));
        
        return linhasAfetadas > 0;
    }
    
    /// <summary>Atualiza as configurações da aplicação</summary>
    public async Task<bool> AtualizarConfiguracoesApp(ConfiguracaoApp config)
    {
        const string sql = @"
            UPDATE configuracoes_app 
            SET servidor_smtp = @servidor_smtp, porta_smtp = @porta_smtp, 
                utilizador_smtp = @utilizador_smtp, palavra_passe_smtp = @palavra_passe_smtp,
                duracao_sessao_dias = @duracao_sessao_dias, servidor_bd = @servidor_bd,
                porta_bd = @porta_bd, nome_bd = @nome_bd, atualizado_em = @atualizado_em
            WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", config.Id),
            ("@servidor_smtp", config.ServidorSmtp),
            ("@porta_smtp", config.PortaSmtp),
            ("@utilizador_smtp", config.UtilizadorSmtp),
            ("@palavra_passe_smtp", config.PalavraPasseSmtp),
            ("@duracao_sessao_dias", config.DuracaoSessaoDias),
            ("@servidor_bd", config.ServidorBd ?? (object)DBNull.Value),
            ("@porta_bd", config.PortaBd ?? (object)DBNull.Value),
            ("@nome_bd", config.NomeBd ?? (object)DBNull.Value),
            ("@atualizado_em", DateTime.UtcNow));
        
        return linhasAfetadas > 0;
    }
    
    // ==========================================
    // MÉTODOS PRIVADOS DE MAPEAMENTO
    // ==========================================
    
    private Utilizador MapearUtilizador(NpgsqlDataReader reader)
    {
        return new Utilizador
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            HashPalavraPasse = reader.GetString(2),
            Cargo = reader.GetString(3) == "administrador" ? CargoUtilizador.Administrador : CargoUtilizador.Normal,
            UnidadeOrganica = reader.IsDBNull(4) ? null : reader.GetString(4),
            EmailVerificado = reader.GetBoolean(5),
            Ativo = reader.GetBoolean(6),
            CriadoEm = reader.GetDateTime(7),
            AtualizadoEm = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            UltimoLoginEm = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
        };
    }
    
    private Utilizador MapearUtilizadorLista(NpgsqlDataReader reader)
    {
        return new Utilizador
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            Cargo = reader.GetString(2) == "administrador" ? CargoUtilizador.Administrador : CargoUtilizador.Normal,
            UnidadeOrganica = reader.IsDBNull(3) ? null : reader.GetString(3),
            EmailVerificado = reader.GetBoolean(4),
            Ativo = reader.GetBoolean(5),
            CriadoEm = reader.GetDateTime(6),
            UltimoLoginEm = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
        };
    }
    
    private CodigoVerificacao MapearCodigoVerificacao(NpgsqlDataReader reader)
    {
        return new CodigoVerificacao
        {
            Id = reader.GetGuid(0),
            UtilizadorId = reader.GetGuid(1),
            Codigo = reader.GetString(2),
            Proposito = reader.GetString(3),
            ExpiraEm = reader.GetDateTime(4),
            Utilizado = reader.GetBoolean(5),
            CriadoEm = reader.GetDateTime(6)
        };
    }
    
    private ConfiguracaoApp MapearConfiguracaoApp(NpgsqlDataReader reader)
    {
        return new ConfiguracaoApp
        {
            Id = reader.GetGuid(0),
            ServidorSmtp = reader.GetString(1),
            PortaSmtp = reader.GetInt32(2),
            UtilizadorSmtp = reader.GetString(3),
            PalavraPasseSmtp = reader.GetString(4),
            DuracaoSessaoDias = reader.GetInt32(5),
            ServidorBd = reader.IsDBNull(6) ? null : reader.GetString(6),
            PortaBd = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            NomeBd = reader.IsDBNull(8) ? null : reader.GetString(8),
            CriadoEm = reader.GetDateTime(9),
            AtualizadoEm = reader.GetDateTime(10)
        };
    }

    public async Task<List<Contacto>> ObterContactosPublicos(string? termoPesquisa = null, string? unidadeOrganica = null)
    {
        // Esta query garante que vamos buscar contactos apenas a listas públicas (onde tipo = 'publica')
        string sql = @"
            SELECT c.id, c.list_id, c.nome, c.unidade_organica, c.numero_interno, c.empresa, 
                   c.emails, c.numeros_contacto, c.criado_em
            FROM contacts c
            INNER JOIN lists l ON c.list_id = l.id
            WHERE l.tipo = 'publica'";

        var parametros = new List<(string, object)>();

        // Filtro por texto livre (nome ou numero interno)
        if (!string.IsNullOrWhiteSpace(termoPesquisa))
        {
            sql += " AND (c.nome ILIKE @termo OR c.numero_interno ILIKE @termo)";
            parametros.Add(("@termo", $"%{termoPesquisa}%"));
        }

        // Filtro por Unidade Orgânica (se não for a opção 'Todas')
        if (!string.IsNullOrWhiteSpace(unidadeOrganica) && unidadeOrganica != "Todas as unidades orgânicas")
        {
            sql += " AND c.unidade_organica = @unidade";
            parametros.Add(("@unidade", unidadeOrganica));
        }

        sql += " ORDER BY c.nome ASC";

        return await ExecutarQueryLista(sql, MapearContacto, parametros.ToArray());
    }

    private Contacto MapearContacto(NpgsqlDataReader reader)
    {
        return new Contacto
        {
            Id = reader.GetGuid(0),
            ListId = reader.GetGuid(1),
            Nome = reader.GetString(2),
            UnidadeOrganica = reader.IsDBNull(3) ? null : reader.GetString(3),
            NumeroInterno = reader.IsDBNull(4) ? null : reader.GetString(4),
            Empresa = reader.IsDBNull(5) ? null : reader.GetString(5),
            Emails = reader.IsDBNull(6) ? Array.Empty<string>() : (string[])reader.GetValue(6), // Array do Postgres
            NumerosContacto = reader.IsDBNull(7) ? Array.Empty<string>() : (string[])reader.GetValue(7), // Array do Postgres
            CriadoEm = reader.GetDateTime(8)
        };
    }
}
