using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AppListaTelefonica.Models;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class MainWindow : Window
{
    private readonly DatabaseService _dbService;
    private readonly AuthService _authService;
    private readonly ContactoService _contactoService;
    private Utilizador? _utilizadorLogado;
    
    public ObservableCollection<ContactoPublico> ContactosListaPublica { get; set; }
    public ObservableCollection<ContactoPrivado> ContactosListaPrivada { get; set; }

    public MainWindow() : this(null!)
    {
    }

    public MainWindow(Utilizador? utilizador)
    {
        InitializeComponent();
        
        _dbService = new DatabaseService();
        _authService = new AuthService();
        _contactoService = new ContactoService();
        ContactosListaPublica = new ObservableCollection<ContactoPublico>();
        ContactosListaPrivada = new ObservableCollection<ContactoPrivado>();
        _utilizadorLogado = utilizador;

        var gridPublica = this.FindControl<DataGrid>("GridContactos");
        if (gridPublica != null) gridPublica.ItemsSource = ContactosListaPublica;

        var gridPrivada = this.FindControl<DataGrid>("GridContactosPrivados");
        if (gridPrivada != null) gridPrivada.ItemsSource = ContactosListaPrivada;

        AtualizarEstadoAutenticacao();

        _ = CarregarContactos();
        if (_utilizadorLogado != null)
        {
            _ = CarregarContactosPrivados();
            _ = CarregarIdentificadores();
        }
    }

    private void AtualizarEstadoAutenticacao()
    {
        var panelVisitante = this.FindControl<StackPanel>("PanelVisitante");
        var panelUtilizador = this.FindControl<StackPanel>("PanelUtilizadorLogado");
        var txtUtilizadorLogado = this.FindControl<TextBlock>("TxtUtilizadorLogado");
        var tabPrivada = this.FindControl<TabItem>("TabPrivada");
        var btnAdminDashboard = this.FindControl<Button>("BtnAdminDashboard");

        if (_utilizadorLogado != null)
        {
            if (panelVisitante != null) panelVisitante.IsVisible = false;
            if (panelUtilizador != null) panelUtilizador.IsVisible = true;
            if (txtUtilizadorLogado != null) 
                txtUtilizadorLogado.Text = $"{_utilizadorLogado.Email} ({_utilizadorLogado.Cargo})";
            if (tabPrivada != null) tabPrivada.IsVisible = true;
            if (btnAdminDashboard != null)
                btnAdminDashboard.IsVisible = _utilizadorLogado.Cargo == CargoUtilizador.Administrador;
        }
        else
        {
            if (panelVisitante != null) panelVisitante.IsVisible = true;
            if (panelUtilizador != null) panelUtilizador.IsVisible = false;
            if (tabPrivada != null) tabPrivada.IsVisible = false;
        }
    }

    // ==========================================
    // LISTA PUBLICA
    // ==========================================

    private async Task CarregarContactos()
    {
        string termoPesquisa = this.FindControl<TextBox>("SearchInput")?.Text ?? string.Empty;
        var dropdownUnit = this.FindControl<ComboBox>("UnitFilter");
        string unidadeOrganica = string.Empty;
        
        if (dropdownUnit != null && dropdownUnit.SelectedItem is ComboBoxItem item)
            unidadeOrganica = item.Content?.ToString() ?? string.Empty;

        var resultados = await _dbService.ObterContactosPublicos(termoPesquisa, unidadeOrganica);

        ContactosListaPublica.Clear();
        foreach (var contacto in resultados)
            ContactosListaPublica.Add(contacto);

        var txtContador = this.FindControl<TextBlock>("TxtContador");
        if (txtContador != null)
            txtContador.Text = $"{ContactosListaPublica.Count} contactos";
    }

    private void SearchInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _ = CarregarContactos();
    }

    private void UnitFilter_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = CarregarContactos();
    }

    private void SearchButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = CarregarContactos();
    }

    // ==========================================
    // LISTA PRIVADA
    // ==========================================

    private async Task CarregarContactosPrivados()
    {
        if (_utilizadorLogado == null) return;

        var filtro = this.FindControl<ComboBox>("FiltroIdentificador");
        Guid? identificadorId = null;
        
        if (filtro != null && filtro.SelectedItem is ComboBoxItem item && item.Tag is Guid id)
            identificadorId = id;

        var resultados = await _contactoService.ObterContactosPrivados(_utilizadorLogado.Id, identificadorId);

        ContactosListaPrivada.Clear();
        foreach (var contacto in resultados)
            ContactosListaPrivada.Add(contacto);

        var txtContador = this.FindControl<TextBlock>("TxtContadorPrivado");
        if (txtContador != null)
            txtContador.Text = $"{ContactosListaPrivada.Count} contactos";
    }

    private async Task CarregarIdentificadores()
    {
        if (_utilizadorLogado == null) return;

        var filtro = this.FindControl<ComboBox>("FiltroIdentificador");
        if (filtro == null) return;

        filtro.Items.Clear();
        filtro.Items.Add(new ComboBoxItem { Content = "Todos os identificadores", Tag = null });

        var identificadores = await _contactoService.ObterIdentificadores(_utilizadorLogado.Id);
        foreach (var ident in identificadores)
        {
            filtro.Items.Add(new ComboBoxItem { Content = ident.Nome, Tag = ident.Id });
        }

        filtro.SelectedIndex = 0;
    }

    private void FiltroIdentificador_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = CarregarContactosPrivados();
    }

    private void BtnNovoContactoPrivado_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Abrir formulario de novo contacto privado
    }

    private void BtnEditarPrivado_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Abrir formulario de edicao de contacto privado
    }

    private async void BtnEliminarPrivado_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id && _utilizadorLogado != null)
        {
            var (sucesso, _) = await _contactoService.EliminarContactoPrivado(id, _utilizadorLogado.Id);
            if (sucesso) await CarregarContactosPrivados();
        }
    }

    private void BtnGerirIdentificadores_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Abrir janela de gestao de identificadores
    }

    private async void BtnEliminarLista_Click(object? sender, RoutedEventArgs e)
    {
        if (_utilizadorLogado == null) return;
        
        var (sucesso, _) = await _contactoService.EliminarTodosContactosPrivados(_utilizadorLogado.Id);
        if (sucesso) await CarregarContactosPrivados();
    }

    // ==========================================
    // AUTENTICACAO
    // ==========================================

    private async void BtnLogin_Click(object? sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginWindow();
        await loginWindow.ShowDialog(this);
    }

    private async void BtnCriarConta_Click(object? sender, RoutedEventArgs e)
    {
        var registoWindow = new RegistoWindow();
        await registoWindow.ShowDialog(this);
    }

    private async void BtnLogout_Click(object? sender, RoutedEventArgs e)
    {
        await _authService.TerminarSessaoAsync();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }

    private void BtnAdminDashboard_Click(object? sender, RoutedEventArgs e)
    {
        if (_utilizadorLogado != null)
        {
            var dashboard = new AdminDashboard(_utilizadorLogado);
            dashboard.Show();
            this.Close();
        }
    }
}