using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using AppListaTelefonica.Models;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class ConfiguracoesAppWindow : Window
{
    private readonly AdminService _adminService;
    private readonly Utilizador _admin;

    public ConfiguracoesAppWindow() : this(null!)
    {
    }

    public ConfiguracoesAppWindow(Utilizador admin)
    {
        InitializeComponent();
        _adminService = new AdminService();
        _admin = admin;
        _ = CarregarConfiguracoes();
    }

    private async Task CarregarConfiguracoes()
    {
        var config = await _adminService.ObterConfiguracoes(_admin);
        if (config != null)
        {
            TxtServidorSmtp.Text = config.ServidorSmtp;
            TxtPortaSmtp.Text = config.PortaSmtp.ToString();
            TxtEmailSmtp.Text = config.UtilizadorSmtp;
            TxtPasswordSmtp.Text = config.PalavraPasseSmtp;
            TxtDuracaoSessao.Text = config.DuracaoSessaoDias.ToString();
            TxtServidorBd.Text = config.ServidorBd ?? "";
            TxtPortaBd.Text = config.PortaBd?.ToString() ?? "";
            TxtNomeBd.Text = config.NomeBd ?? "";
        }
    }

    private async void BtnGuardarEmail_Click(object? sender, RoutedEventArgs e)
    {
        string servidor = TxtServidorSmtp.Text?.Trim() ?? "";
        string portaStr = TxtPortaSmtp.Text?.Trim() ?? "587";
        string email = TxtEmailSmtp.Text?.Trim() ?? "";
        string password = TxtPasswordSmtp.Text ?? "";

        if (!int.TryParse(portaStr, out int porta)) porta = 587;

        var (sucesso, mensagem) = await _adminService.AtualizarConfiguracoesEmail(servidor, porta, email, password, _admin);
        MostrarMensagem(mensagem ?? "", !sucesso);
    }

    private async void BtnGuardarSessao_Click(object? sender, RoutedEventArgs e)
    {
        string diasStr = TxtDuracaoSessao.Text?.Trim() ?? "7";
        if (!int.TryParse(diasStr, out int dias)) dias = 7;

        var (sucesso, mensagem) = await _adminService.AtualizarDuracaoSessao(dias, _admin);
        MostrarMensagem(mensagem ?? "", !sucesso);
    }

    private async void BtnGuardarBd_Click(object? sender, RoutedEventArgs e)
    {
        string servidor = TxtServidorBd.Text?.Trim() ?? "";
        string portaStr = TxtPortaBd.Text?.Trim() ?? "5432";
        string nomeBd = TxtNomeBd.Text?.Trim() ?? "";

        if (!int.TryParse(portaStr, out int porta)) porta = 5432;

        var (sucesso, mensagem) = await _adminService.AtualizarConfiguracoesBaseDados(servidor, porta, nomeBd, _admin);
        MostrarMensagem(mensagem ?? "", !sucesso);
    }

    private void BtnVoltar_Click(object? sender, RoutedEventArgs e)
    {
        var dashboard = new AdminDashboard(_admin);
        dashboard.Show();
        this.Close();
    }

    private void MostrarMensagem(string mensagem, bool erro)
    {
        TxtMensagem.Text = mensagem;
        TxtMensagem.Foreground = erro ? 
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#DC2626")) : 
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#16A34A"));
        BorderMensagem.BorderBrush = erro ? 
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FCA5A5")) : 
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#86EFAC"));
        BorderMensagem.Background = erro ? 
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FEF2F2")) : 
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F0FDF4"));
        BorderMensagem.IsVisible = true;
    }
}