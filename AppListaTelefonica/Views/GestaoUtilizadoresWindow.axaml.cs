using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AppListaTelefonica.Models;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class GestaoUtilizadoresWindow : Window
{
    private readonly AdminService _adminService;
    private readonly Utilizador _admin;
    private ObservableCollection<Utilizador> _utilizadoresLista;

    public GestaoUtilizadoresWindow() : this(null!)
    {
    }

    public GestaoUtilizadoresWindow(Utilizador admin)
    {
        InitializeComponent();
        _adminService = new AdminService();
        _admin = admin;
        _utilizadoresLista = new ObservableCollection<Utilizador>();

        var grid = this.FindControl<DataGrid>("GridUtilizadores");
        if (grid != null) grid.ItemsSource = _utilizadoresLista;

        _ = CarregarUtilizadores();
    }

    private async Task CarregarUtilizadores()
    {
        var utilizadores = await _adminService.ListarUtilizadores(_admin);
        _utilizadoresLista.Clear();
        foreach (var u in utilizadores)
            _utilizadoresLista.Add(u);
        
        TxtContador.Text = $"{_utilizadoresLista.Count} utilizadores";
    }

    private async void BtnTornarAdmin_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            var (sucesso, mensagem) = await _adminService.AlterarCargoUtilizador(id, CargoUtilizador.Administrador, _admin);
            MostrarMensagem(mensagem ?? "", !sucesso);
            if (sucesso) await CarregarUtilizadores();
        }
    }

    private async void BtnTornarNormal_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            var (sucesso, mensagem) = await _adminService.AlterarCargoUtilizador(id, CargoUtilizador.Normal, _admin);
            MostrarMensagem(mensagem ?? "", !sucesso);
            if (sucesso) await CarregarUtilizadores();
        }
    }

    private async void BtnToggleAtivo_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            var utilizador = _utilizadoresLista.FirstOrDefault(u => u.Id == id);
            if (utilizador != null)
            {
                var (sucesso, mensagem) = await _adminService.AtivarDesativarUtilizador(id, !utilizador.Ativo, _admin);
                MostrarMensagem(mensagem ?? "", !sucesso);
                if (sucesso) await CarregarUtilizadores();
            }
        }
    }

    private async void BtnEliminar_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            var (sucesso, mensagem) = await _adminService.EliminarUtilizador(id, _admin);
            MostrarMensagem(mensagem ?? "", !sucesso);
            if (sucesso) await CarregarUtilizadores();
        }
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