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
    
    // ==========================================
    // MÉTODOS AUXILIARES CENTRALIZADOS
    // ==========================================
    
    private async Task<T?> ExecutarQueryUnica<T>(string sql, Func<NpgsqlDataReader, T> mapear, params (string nome, object? valor)[] parametros)
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

    private async Task<List<T>> ExecutarQueryLista<T>(string sql, Func<NpgsqlDataReader, T> mapear, params (string nome, object? valor)[] parametros)
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
    
    private async Task<int> ExecutarComando(string sql, params (string nome, object? valor)[] parametros)
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
    
    private async Task<object?> ExecutarScalar(string sql, params (string nome, object? valor)[] parametros)
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

    
    
    // ==========================================
    // SECÇÃO: UTILIZADORES
    // ==========================================

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
            FROM utilizadores WHERE cargo = CAST(@cargo AS cargo_utilizador) ORDER BY email";
    
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
        const string sql = "SELECT COUNT(*) FROM utilizadores WHERE cargo = CAST(@cargo AS cargo_utilizador)";
        var resultado = await ExecutarScalar(sql, ("@cargo", cargoStr));
        return resultado != null ? Convert.ToInt32(resultado) : 0;
    }
 
    public async Task<bool> CriarUtilizador(Utilizador utilizador, string hashPalavraPasse)
    {
        const string sql = @"
            INSERT INTO utilizadores (id, email, hash_palavra_passe, cargo, unidade_organica, 
                                  email_verificado, ativo, criado_em)
            VALUES (@id, @email, @hash_palavra_passe, CAST(@cargo AS cargo_utilizador), @unidade_organica, 
                    @email_verificado, @ativo, @criado_em)";
    
        int linhasAfetadas = await ExecutarComando(sql,
            ("@id", utilizador.Id),
            ("@email", utilizador.Email),
            ("@hash_palavra_passe", hashPalavraPasse),
            ("@cargo", utilizador.Cargo == CargoUtilizador.Administrador ? "administrador" : "normal"),
            ("@unidade_organica", (object?)utilizador.UnidadeOrganica),
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
        const string sql = "UPDATE utilizadores SET cargo = CAST(@cargo AS cargo_utilizador), atualizado_em = @atualizado_em WHERE id = @id";
    
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

    // No fim da secção UTILIZADORES, antes da secção CÓDIGOS DE VERIFICAÇÃO:

    public async Task<EstatisticasAdmin> ObterEstatisticasAdmin()
    {
        var stats = new EstatisticasAdmin();
    
        stats.TotalUtilizadores = await ObterTotalUtilizadores();
        stats.TotalAdministradores = await ObterTotalUtilizadoresPorCargo(CargoUtilizador.Administrador);
        stats.TotalNormais = await ObterTotalUtilizadoresPorCargo(CargoUtilizador.Normal);
    
        const string sqlPublicos = "SELECT COUNT(*) FROM contactos_publicos";
        var resultPublicos = await ExecutarScalar(sqlPublicos);
        stats.TotalContactosPublicos = resultPublicos != null ? Convert.ToInt32(resultPublicos) : 0;
    
        const string sqlPrivados = "SELECT COUNT(*) FROM contactos_privados";
        var resultPrivados = await ExecutarScalar(sqlPrivados);
        stats.TotalContactosPrivados = resultPrivados != null ? Convert.ToInt32(resultPrivados) : 0;
    
        return stats;
    }
    
    // ==========================================
    // SECÇÃO: CÓDIGOS DE VERIFICAÇÃO
    // ==========================================

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
        
        await AtualizarCodigoComoUtilizado(resultado.Id);
        
        return true;
    }
    
    public async Task<bool> AtualizarCodigoComoUtilizado(Guid codigoId)
    {
        const string sql = "UPDATE codigos_verificacao SET utilizado = true WHERE id = @id";
        
        int linhasAfetadas = await ExecutarComando(sql, ("@id", codigoId));
        return linhasAfetadas > 0;
    }
    
    public async Task<int> EliminarCodigosExpirados(Guid utilizadorId)
    {
        const string sql = "DELETE FROM codigos_verificacao WHERE utilizador_id = @utilizador_id AND expira_em < @agora";
        
        int linhasAfetadas = await ExecutarComando(sql,
            ("@utilizador_id", utilizadorId),
            ("@agora", DateTime.UtcNow));
        
        return linhasAfetadas;
    }
    
    // ==========================================
    // SECÇÃO: CONFIGURAÇÕES DA APLICAÇÃO
    // ==========================================
    
    public async Task<ConfiguracaoApp?> ObterConfiguracoesApp()
    {
        const string sql = @"
            SELECT id, servidor_smtp, porta_smtp, utilizador_smtp, palavra_passe_smtp, 
                   duracao_sessao_dias, servidor_bd, porta_bd, nome_bd, criado_em, atualizado_em
            FROM configuracoes_app LIMIT 1";
        
        return await ExecutarQueryUnica(sql, MapearConfiguracaoApp);
    }
    
    public async Task<bool> VerificarExistenciaConfiguracoes()
    {
        const string sql = "SELECT COUNT(*) FROM configuracoes_app";
        var resultado = await ExecutarScalar(sql);
        return resultado != null && (long)resultado > 0;
    }
    
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
            ("@servidor_bd", (object?)config.ServidorBd),
            ("@porta_bd", (object?)config.PortaBd),
            ("@nome_bd", (object?)config.NomeBd),
            ("@criado_em", config.CriadoEm),
            ("@atualizado_em", config.AtualizadoEm));
        
        return linhasAfetadas > 0;
    }
    
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
            ("@servidor_bd", (object?)config.ServidorBd),
            ("@porta_bd", (object?)config.PortaBd),
            ("@nome_bd", (object?)config.NomeBd),
            ("@atualizado_em", DateTime.UtcNow));
        
        return linhasAfetadas > 0;
    }

    // ==========================================
// SECÇÃO: CONTACTOS PÚBLICOS
// ==========================================

public async Task<List<ContactoPublico>> ObterContactosPublicos(string? termoPesquisa = null, string? unidadeOrganica = null)
{
    var sql = @"
        SELECT id, nome, email, numero_interno, telemovel, unidade_organica, criado_em, atualizado_em
        FROM contactos_publicos
        WHERE 1=1";
    
    var parametros = new List<(string nome, object? valor)>();
    
    if (!string.IsNullOrWhiteSpace(termoPesquisa))
    {
        sql += " AND (nome ILIKE @termo OR email ILIKE @termo OR numero_interno ILIKE @termo)";
        parametros.Add(("@termo", $"%{termoPesquisa}%"));
    }
    
    if (!string.IsNullOrWhiteSpace(unidadeOrganica) && unidadeOrganica != "Todas as unidades orgânicas")
    {
        sql += " AND unidade_organica = @unidade";
        parametros.Add(("@unidade", unidadeOrganica));
    }
    
    sql += " ORDER BY nome ASC";
    
    return await ExecutarQueryLista(sql, MapearContactoPublico, parametros.ToArray());
}

public async Task<ContactoPublico?> ObterContactoPublicoPorId(Guid id)
{
    const string sql = @"
        SELECT id, nome, email, numero_interno, telemovel, unidade_organica, criado_em, atualizado_em
        FROM contactos_publicos WHERE id = @id";
    
    return await ExecutarQueryUnica(sql, MapearContactoPublico, ("@id", id));
}

public async Task<bool> CriarContactoPublico(ContactoPublico contacto)
{
    const string sql = @"
        INSERT INTO contactos_publicos (id, nome, email, numero_interno, telemovel, unidade_organica, criado_em)
        VALUES (@id, @nome, @email, @numero_interno, @telemovel, @unidade_organica, @criado_em)";
    
    int linhasAfetadas = await ExecutarComando(sql,
        ("@id", contacto.Id == Guid.Empty ? Guid.NewGuid() : contacto.Id),
        ("@nome", contacto.Nome),
        ("@email", (object?)contacto.Email),
        ("@numero_interno", (object?)contacto.NumeroInterno),
        ("@telemovel", (object?)contacto.Telemovel),
        ("@unidade_organica", (object?)contacto.UnidadeOrganica),
        ("@criado_em", DateTime.UtcNow));
    
    return linhasAfetadas > 0;
}

public async Task<bool> AtualizarContactoPublico(ContactoPublico contacto)
{
    const string sql = @"
        UPDATE contactos_publicos 
        SET nome = @nome, email = @email, numero_interno = @numero_interno, 
            telemovel = @telemovel, unidade_organica = @unidade_organica, atualizado_em = @atualizado_em
        WHERE id = @id";
    
    int linhasAfetadas = await ExecutarComando(sql,
        ("@id", contacto.Id),
        ("@nome", contacto.Nome),
        ("@email", (object?)contacto.Email),
        ("@numero_interno", (object?)contacto.NumeroInterno),
        ("@telemovel", (object?)contacto.Telemovel),
        ("@unidade_organica", (object?)contacto.UnidadeOrganica),
        ("@atualizado_em", DateTime.UtcNow));
    
    return linhasAfetadas > 0;
}

public async Task<bool> EliminarContactoPublico(Guid id)
{
    const string sql = "DELETE FROM contactos_publicos WHERE id = @id";
    int linhasAfetadas = await ExecutarComando(sql, ("@id", id));
    return linhasAfetadas > 0;
}

// ==========================================
// SECÇÃO: CONTACTOS PRIVADOS
// ==========================================

public async Task<List<ContactoPrivado>> ObterContactosPrivados(Guid utilizadorId, Guid? identificadorId = null)
{
    var sql = @"
        SELECT cp.id, cp.utilizador_id, cp.identificador_id, cp.nome, cp.empresa, 
               cp.email, cp.telefone, cp.numero_interno, cp.criado_em, cp.atualizado_em,
               i.nome AS identificador_nome
        FROM contactos_privados cp
        LEFT JOIN identificadores i ON cp.identificador_id = i.id
        WHERE cp.utilizador_id = @utilizador_id";
    
    var parametros = new List<(string nome, object? valor)>
    {
        ("@utilizador_id", utilizadorId)
    };
    
    if (identificadorId.HasValue)
    {
        sql += " AND cp.identificador_id = @identificador_id";
        parametros.Add(("@identificador_id", identificadorId.Value));
    }
    
    sql += " ORDER BY cp.nome ASC";
    
    return await ExecutarQueryLista(sql, MapearContactoPrivado, parametros.ToArray());
}

public async Task<ContactoPrivado?> ObterContactoPrivadoPorId(Guid id, Guid utilizadorId)
{
    const string sql = @"
        SELECT cp.id, cp.utilizador_id, cp.identificador_id, cp.nome, cp.empresa, 
               cp.email, cp.telefone, cp.numero_interno, cp.criado_em, cp.atualizado_em,
               i.nome AS identificador_nome
        FROM contactos_privados cp
        LEFT JOIN identificadores i ON cp.identificador_id = i.id
        WHERE cp.id = @id AND cp.utilizador_id = @utilizador_id";
    
    return await ExecutarQueryUnica(sql, MapearContactoPrivado, 
        ("@id", id), ("@utilizador_id", utilizadorId));
}

public async Task<bool> CriarContactoPrivado(ContactoPrivado contacto)
{
    const string sql = @"
        INSERT INTO contactos_privados (id, utilizador_id, identificador_id, nome, empresa, 
                                        email, telefone, numero_interno, criado_em)
        VALUES (@id, @utilizador_id, @identificador_id, @nome, @empresa, 
                @email, @telefone, @numero_interno, @criado_em)";
    
    int linhasAfetadas = await ExecutarComando(sql,
        ("@id", contacto.Id == Guid.Empty ? Guid.NewGuid() : contacto.Id),
        ("@utilizador_id", contacto.UtilizadorId),
        ("@identificador_id", (object?)contacto.IdentificadorId),
        ("@nome", contacto.Nome),
        ("@empresa", (object?)contacto.Empresa),
        ("@email", (object?)contacto.Email),
        ("@telefone", (object?)contacto.Telefone),
        ("@numero_interno", (object?)contacto.NumeroInterno),
        ("@criado_em", DateTime.UtcNow));
    
    return linhasAfetadas > 0;
}

public async Task<bool> AtualizarContactoPrivado(ContactoPrivado contacto)
{
    const string sql = @"
        UPDATE contactos_privados 
        SET identificador_id = @identificador_id, nome = @nome, empresa = @empresa, 
            email = @email, telefone = @telefone, numero_interno = @numero_interno, 
            atualizado_em = @atualizado_em
        WHERE id = @id AND utilizador_id = @utilizador_id";
    
    int linhasAfetadas = await ExecutarComando(sql,
        ("@id", contacto.Id),
        ("@utilizador_id", contacto.UtilizadorId),
        ("@identificador_id", (object?)contacto.IdentificadorId),
        ("@nome", contacto.Nome),
        ("@empresa", (object?)contacto.Empresa),
        ("@email", (object?)contacto.Email),
        ("@telefone", (object?)contacto.Telefone),
        ("@numero_interno", (object?)contacto.NumeroInterno),
        ("@atualizado_em", DateTime.UtcNow));
    
    return linhasAfetadas > 0;
}

public async Task<bool> EliminarContactoPrivado(Guid id, Guid utilizadorId)
{
    const string sql = "DELETE FROM contactos_privados WHERE id = @id AND utilizador_id = @utilizador_id";
    int linhasAfetadas = await ExecutarComando(sql, ("@id", id), ("@utilizador_id", utilizadorId));
    return linhasAfetadas > 0;
}

public async Task<bool> EliminarTodosContactosPrivados(Guid utilizadorId)
{
    const string sql = "DELETE FROM contactos_privados WHERE utilizador_id = @utilizador_id";
    int linhasAfetadas = await ExecutarComando(sql, ("@utilizador_id", utilizadorId));
    return linhasAfetadas > 0;
}

// ==========================================
// SECÇÃO: IDENTIFICADORES
// ==========================================

public async Task<List<Identificador>> ObterIdentificadores(Guid utilizadorId)
{
    const string sql = @"
        SELECT id, utilizador_id, nome
        FROM identificadores
        WHERE utilizador_id = @utilizador_id
        ORDER BY nome ASC";
    
    return await ExecutarQueryLista(sql, MapearIdentificador, ("@utilizador_id", utilizadorId));
}

public async Task<Identificador?> ObterIdentificadorPorId(Guid id, Guid utilizadorId)
{
    const string sql = @"
        SELECT id, utilizador_id, nome
        FROM identificadores
        WHERE id = @id AND utilizador_id = @utilizador_id";
    
    return await ExecutarQueryUnica(sql, MapearIdentificador, ("@id", id), ("@utilizador_id", utilizadorId));
}

public async Task<bool> CriarIdentificador(Identificador identificador)
{
    const string sql = @"
        INSERT INTO identificadores (id, utilizador_id, nome)
        VALUES (@id, @utilizador_id, @nome)";
    
    int linhasAfetadas = await ExecutarComando(sql,
        ("@id", identificador.Id == Guid.Empty ? Guid.NewGuid() : identificador.Id),
        ("@utilizador_id", identificador.UtilizadorId),
        ("@nome", identificador.Nome));
    
    return linhasAfetadas > 0;
}

public async Task<bool> AtualizarIdentificador(Identificador identificador)
{
    const string sql = @"
        UPDATE identificadores 
        SET nome = @nome
        WHERE id = @id AND utilizador_id = @utilizador_id";
    
    int linhasAfetadas = await ExecutarComando(sql,
        ("@id", identificador.Id),
        ("@utilizador_id", identificador.UtilizadorId),
        ("@nome", identificador.Nome));
    
    return linhasAfetadas > 0;
}

public async Task<bool> EliminarIdentificador(Guid id, Guid utilizadorId)
{
    const string sql = "DELETE FROM identificadores WHERE id = @id AND utilizador_id = @utilizador_id";
    int linhasAfetadas = await ExecutarComando(sql, ("@id", id), ("@utilizador_id", utilizadorId));
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


    private ContactoPublico MapearContactoPublico(NpgsqlDataReader reader)
{
    return new ContactoPublico
    {
        Id = reader.GetGuid(0),
        Nome = reader.GetString(1),
        Email = reader.IsDBNull(2) ? null : reader.GetString(2),
        NumeroInterno = reader.IsDBNull(3) ? null : reader.GetString(3),
        Telemovel = reader.IsDBNull(4) ? null : reader.GetString(4),
        UnidadeOrganica = reader.IsDBNull(5) ? null : reader.GetString(5),
        CriadoEm = reader.GetDateTime(6),
        AtualizadoEm = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    };
}

private ContactoPrivado MapearContactoPrivado(NpgsqlDataReader reader)
{
    return new ContactoPrivado
    {
        Id = reader.GetGuid(0),
        UtilizadorId = reader.GetGuid(1),
        IdentificadorId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
        Nome = reader.GetString(3),
        Empresa = reader.IsDBNull(4) ? null : reader.GetString(4),
        Email = reader.IsDBNull(5) ? null : reader.GetString(5),
        Telefone = reader.IsDBNull(6) ? null : reader.GetString(6),
        NumeroInterno = reader.IsDBNull(7) ? null : reader.GetString(7),
        CriadoEm = reader.GetDateTime(8),
        AtualizadoEm = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
        IdentificadorNome = reader.IsDBNull(10) ? null : reader.GetString(10)
    };
}

private Identificador MapearIdentificador(NpgsqlDataReader reader)
{
    return new Identificador
    {
        Id = reader.GetGuid(0),
        UtilizadorId = reader.GetGuid(1),
        Nome = reader.GetString(2)
    };
}

}