using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService;
    
    public LoginWindow()
    {
        InitializeComponent();
        _authService = new AuthService();
        this.Loaded += OnLoadedAsync;
    }
    
    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        await VerificarSessaoPersistenteAsync();
    }
    
    private async Task VerificarSessaoPersistenteAsync()
    {
        var (sessaoValida, utilizador) = await _authService.VerificarSessaoPersistenteAsync();
        
        if (sessaoValida && utilizador != null)
        {
            await AbrirMainWindowAsync(utilizador);
        }
    }
    
    private async void BtnLogin_Click(object? sender, RoutedEventArgs e)
    {
        EsconderErro();
        DesabilitarBotoes();
        
        string email = TxtEmail.Text?.Trim() ?? "";
        string password = TxtPassword.Text ?? "";
        
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            MostrarErro("Por favor, preencha todos os campos.");
            HabilitarBotoes();
            return;
        }
        
        bool manterSessao = ChkManterSessao.IsChecked ?? false;
        
        var (sucesso, mensagemErro, utilizador) = await _authService.LoginAsync(email, password, manterSessao);
        
        if (sucesso && utilizador != null)
        {
            await AbrirMainWindowAsync(utilizador);
        }
        else
        {
            MostrarErro(mensagemErro ?? "Erro ao fazer login.");
            HabilitarBotoes();
        }
    }
    
    private async void BtnRegistar_Click(object? sender, RoutedEventArgs e)
    {
        var registoWindow = new RegistoWindow();
        registoWindow.RegistoConcluido += RegistoWindow_RegistoConcluido;
        await registoWindow.ShowDialog(this);
    }
    
    private async void RegistoWindow_RegistoConcluido(object? sender, RegistoEventArgs e)
    {
        var verificacaoWindow = new VerificacaoEmailWindow(e.UtilizadorId);
        await verificacaoWindow.ShowDialog(this);
    }
    
    private async void BtnRecuperarPassword_Click(object? sender, RoutedEventArgs e)
    {
        var recuperacaoWindow = new RecuperacaoPasswordWindow();
        await recuperacaoWindow.ShowDialog(this);
    }
    
    private async Task AbrirMainWindowAsync(Models.Utilizador utilizador)
    {
        var mainWindow = new MainWindow(utilizador);
        mainWindow.Show();
        this.Close();
    }
    
    private void MostrarErro(string mensagem)
    {
        TxtErro.Text = mensagem;
        BorderErro.IsVisible = true;
    }
    
    private void EsconderErro()
    {
        BorderErro.IsVisible = false;
    }
    
    private void DesabilitarBotoes()
    {
        BtnLogin.IsEnabled = false;
        BtnRegistar.IsEnabled = false;
        BtnRecuperarPassword.IsEnabled = false;
    }
    
    private void HabilitarBotoes()
    {
        BtnLogin.IsEnabled = true;
        BtnRegistar.IsEnabled = true;
        BtnRecuperarPassword.IsEnabled = true;
    }
}

public class RegistoEventArgs : EventArgs
{
    public Guid UtilizadorId { get; }
    
    public RegistoEventArgs(Guid utilizadorId)
    {
        UtilizadorId = utilizadorId;
    }
}