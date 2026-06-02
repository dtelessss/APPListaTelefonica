using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AppListaTelefonica.Models;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class MainWindow : Window
{
    private readonly DatabaseService _dbService;
    public ObservableCollection<Contacto> ContactosListaPublica { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        
        // Instancia o serviço que já tem a ligação configurada na tua app
        _dbService = new DatabaseService();
        ContactosListaPublica = new ObservableCollection<Contacto>();

        var grid = this.FindControl<DataGrid>("GridContactos");
        if (grid != null)
        {
            grid.ItemsSource = ContactosListaPublica;
        }

        // Carrega os dados da base de dados assim que a janela é inicializada
        _ = CarregarContactos(); 
    }

    // Função central para ir buscar os dados à BD (usa a pesquisa e filtros ativos)
    private async Task CarregarContactos()
    {
        string termoPesquisa = this.FindControl<TextBox>("SearchInput")?.Text ?? string.Empty;
        
        var dropdownUnit = this.FindControl<ComboBox>("UnitFilter");
        string unidadeOrganica = string.Empty;
        
        if (dropdownUnit != null && dropdownUnit.SelectedItem is ComboBoxItem item)
        {
            unidadeOrganica = item.Content?.ToString() ?? string.Empty;
        }

        // Vai à Base de Dados
        var resultados = await _dbService.ObterContactosPublicos(termoPesquisa, unidadeOrganica);

        // Atualiza a tabela na UI
        ContactosListaPublica.Clear();
        foreach (var contato in resultados)
        {
            ContactosListaPublica.Add(contato);
        }

        // Atualiza a contagem visual
        var txtContador = this.FindControl<TextBlock>("TxtContador");
        if (txtContador != null)
        {
            txtContador.Text = $"{ContactosListaPublica.Count} contactos";
        }
    }

    // Evento quando o utilizador escreve na barra de pesquisa
    private void SearchInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        // Poderias usar um "debounce" aqui, mas para já, pesquisa logo ao escrever.
        _ = CarregarContactos();
    }

    // Evento quando o utilizador muda o dropdown de Unidade Orgânica
    private void UnitFilter_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = CarregarContactos();
    }

    // Evento quando o utilizador clica explicitamente no botão "Buscar"
    private void SearchButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = CarregarContactos();
    }
}
