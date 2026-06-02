using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AppListaTelefonica.Models;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class GestaoContactosPublicosWindow : Window
{
    private readonly ContactoService _contactoService;
    private readonly Utilizador _admin;
    private ObservableCollection<ContactoPublico> _contactosLista;
    private Guid? _editandoId;

    public GestaoContactosPublicosWindow() : this(null!)
    {
    }

    public GestaoContactosPublicosWindow(Utilizador admin)
    {
        InitializeComponent();
        _contactoService = new ContactoService();
        _admin = admin;
        _contactosLista = new ObservableCollection<ContactoPublico>();
        _editandoId = null;

        var grid = this.FindControl<DataGrid>("GridContactos");
        if (grid != null) grid.ItemsSource = _contactosLista;

        _ = CarregarContactos();
    }

    private async Task CarregarContactos()
    {
        var contactos = await _contactoService.ObterContactosPublicos();
        _contactosLista.Clear();
        foreach (var c in contactos)
            _contactosLista.Add(c);
        
        TxtContador.Text = $"{_contactosLista.Count} contactos";
    }

    private async void BtnAdicionar_Click(object? sender, RoutedEventArgs e)
    {
        string nome = TxtNome.Text?.Trim() ?? "";
        string email = TxtEmail.Text?.Trim() ?? "";
        string numeroInterno = TxtNumeroInterno.Text?.Trim() ?? "";
        string telemovel = TxtTelemovel.Text?.Trim() ?? "";
        string unidadeOrganica = TxtUnidadeOrganica.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nome))
        {
            MostrarMensagem("O nome e obrigatorio.", true);
            return;
        }

        if (_editandoId.HasValue)
        {
            var contacto = new ContactoPublico
            {
                Id = _editandoId.Value,
                Nome = nome,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                NumeroInterno = string.IsNullOrWhiteSpace(numeroInterno) ? null : numeroInterno,
                Telemovel = string.IsNullOrWhiteSpace(telemovel) ? null : telemovel,
                UnidadeOrganica = string.IsNullOrWhiteSpace(unidadeOrganica) ? null : unidadeOrganica
            };

            var (sucesso, mensagem) = await _contactoService.AtualizarContactoPublico(contacto, _admin);
            MostrarMensagem(mensagem ?? "", !sucesso);
            if (sucesso) { LimparFormulario(); await CarregarContactos(); }
        }
        else
        {
            var contacto = new ContactoPublico
            {
                Nome = nome,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                NumeroInterno = string.IsNullOrWhiteSpace(numeroInterno) ? null : numeroInterno,
                Telemovel = string.IsNullOrWhiteSpace(telemovel) ? null : telemovel,
                UnidadeOrganica = string.IsNullOrWhiteSpace(unidadeOrganica) ? null : unidadeOrganica
            };

            var (sucesso, mensagem) = await _contactoService.CriarContactoPublico(contacto, _admin);
            MostrarMensagem(mensagem ?? "", !sucesso);
            if (sucesso) { LimparFormulario(); await CarregarContactos(); }
        }
    }

    private void BtnEditar_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            var contacto = _contactosLista.FirstOrDefault(c => c.Id == id);
            if (contacto != null)
            {
                TxtNome.Text = contacto.Nome;
                TxtEmail.Text = contacto.Email ?? "";
                TxtNumeroInterno.Text = contacto.NumeroInterno ?? "";
                TxtTelemovel.Text = contacto.Telemovel ?? "";
                TxtUnidadeOrganica.Text = contacto.UnidadeOrganica ?? "";
                _editandoId = id;
                BtnAdicionar.Content = "Atualizar";
            }
        }
    }

    private async void BtnEliminar_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            var (sucesso, mensagem) = await _contactoService.EliminarContactoPublico(id, _admin);
            MostrarMensagem(mensagem ?? "", !sucesso);
            if (sucesso) await CarregarContactos();
        }
    }

    private void BtnVoltar_Click(object? sender, RoutedEventArgs e)
    {
        var dashboard = new AdminDashboard(_admin);
        dashboard.Show();
        this.Close();
    }

    private void LimparFormulario()
    {
        TxtNome.Text = "";
        TxtEmail.Text = "";
        TxtNumeroInterno.Text = "";
        TxtTelemovel.Text = "";
        TxtUnidadeOrganica.Text = "";
        _editandoId = null;
        BtnAdicionar.Content = "Adicionar";
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