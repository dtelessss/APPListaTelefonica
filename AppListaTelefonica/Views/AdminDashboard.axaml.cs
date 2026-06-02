using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AppListaTelefonica.Models;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class AdminDashboard : Window
{
    private readonly DatabaseService _dbService;
    private readonly AuthService _authService;
    private readonly Utilizador _admin;
    private ObservableCollection<Utilizador> _utilizadoresLista;

    public AdminDashboard() : this(null!)
    {
    }

    public AdminDashboard(Utilizador admin)
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        _authService = new AuthService();
        _admin = admin;
        _utilizadoresLista = new ObservableCollection<Utilizador>();

        TxtAdminEmail.Text = admin.Email;

        var grid = this.FindControl<DataGrid>("GridUtilizadores");
        if (grid != null)
            grid.ItemsSource = _utilizadoresLista;

        _ = CarregarDashboard();
    }

    private async Task CarregarDashboard()
    {
        var stats = await _dbService.ObterEstatisticasAdmin();
        var utilizadores = await _dbService.ObterTodosUtilizadores();

        // Atualizar Cards
        TxtTotalUtilizadores.Text = stats.TotalUtilizadores.ToString();
        TxtTotalAdmins.Text = stats.TotalAdministradores.ToString();
        TxtTotalPublicos.Text = stats.TotalContactosPublicos.ToString();
        TxtTotalPrivados.Text = stats.TotalContactosPrivados.ToString();
        TxtDistribuicao.Text = $"{stats.TotalAdministradores} admins / {stats.TotalNormais} normais";
        TxtTotalTabela.Text = $"{stats.TotalUtilizadores} total";

        // Atualizar Grafico
        if (stats.TotalUtilizadores > 0)
        {
            double percentAdmins = (double)stats.TotalAdministradores / stats.TotalUtilizadores * 100;
            double percentNormais = (double)stats.TotalNormais / stats.TotalUtilizadores * 100;
            
            BarAdmins.Width = Math.Max(percentAdmins * 2, 4);
            BarNormais.Width = Math.Max(percentNormais * 2, 4);
            TxtPercentAdmins.Text = $"{percentAdmins:F0}%";
            TxtPercentNormais.Text = $"{percentNormais:F0}%";
        }

        int maxContactos = Math.Max(stats.TotalContactosPublicos, stats.TotalContactosPrivados);
        if (maxContactos > 0)
        {
            BarPublicos.Width = Math.Max((double)stats.TotalContactosPublicos / maxContactos * 100, 4);
            BarPrivados.Width = Math.Max((double)stats.TotalContactosPrivados / maxContactos * 100, 4);
        }
        TxtPercentContactos.Text = $"{stats.TotalContactosPublicos} / {stats.TotalContactosPrivados}";

        // Atualizar Tabela
        _utilizadoresLista.Clear();
        foreach (var u in utilizadores)
        {
            _utilizadoresLista.Add(u);
        }
    }

    private void BtnVoltar_Click(object? sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow(_admin);
        mainWindow.Show();
        this.Close();
    }

    private async void BtnTerminarSessao_Click(object? sender, RoutedEventArgs e)
    {
        await _authService.TerminarSessaoAsync();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }

    private void BtnGerirPublicos_Click(object? sender, RoutedEventArgs e)
    {
        var janela = new GestaoContactosPublicosWindow(_admin);
        janela.Show();
        this.Close();
    }   

    private void BtnGerirUtilizadores_Click(object? sender, RoutedEventArgs e)  
    {
        var janela = new GestaoUtilizadoresWindow(_admin);
        janela.Show();
        this.Close();
    }

    private void BtnConfiguracoes_Click(object? sender, RoutedEventArgs e)
    {
        var janela = new ConfiguracoesAppWindow(_admin);
        janela.Show();
        this.Close();
    }
}