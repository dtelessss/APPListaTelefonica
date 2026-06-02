// Services/ConfiguracaoService.cs
using System.Threading.Tasks;
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Services
{
    public class ConfiguracaoService
    {
        private readonly DatabaseService _db;

        public ConfiguracaoService()
        {
            _db = new DatabaseService();
        }

        public async Task<int> ObterDuracaoSessaoDias()
        {
            var config = await _db.ObterConfiguracoesApp();
            return config?.DuracaoSessaoDias ?? 7; // Valor padrão 7 dias
        }

        public async Task<ConfiguracaoApp?> ObterConfiguracoes()
        {
            return await _db.ObterConfiguracoesApp();
        }
    }
}