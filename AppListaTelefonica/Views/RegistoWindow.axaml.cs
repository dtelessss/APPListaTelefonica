using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace AppListaTelefonica.Views;

public partial class RegistoWindow : Window
{
    private readonly Services.AuthService _authService;
    
    public event EventHandler<RegistoEventArgs>? RegistoConcluido;
    
    public RegistoWindow()
    {
        InitializeComponent();
        _authService = new Services.AuthService();
    }
    
    private async void BtnRegistar_Click(object? sender, RoutedEventArgs e)
    {
        EsconderMensagens();
        DesabilitarBotoes();
        
        string email = TxtEmail.Text?.Trim() ?? "";
        string password = TxtPassword.Text ?? "";
        string confirmarPassword = TxtConfirmarPassword.Text ?? "";
        string? unidadeOrganica = string.IsNullOrWhiteSpace(TxtUnidadeOrganica.Text) ? null : TxtUnidadeOrganica.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmarPassword))
        {
            MostrarErro("Por favor, preencha todos os campos obrigatorios.");
            HabilitarBotoes();
            return;
        }
        
        if (password != confirmarPassword)
        {
            MostrarErro("As palavras-passe nao coincidem.");
            HabilitarBotoes();
            return;
        }
        
        var (sucesso, mensagemErro, utilizadorId) = await _authService.RegistarUtilizadorAsync(email, password, unidadeOrganica);
        
        if (sucesso && utilizadorId.HasValue)
        {
            MostrarSucesso(mensagemErro ?? "Conta criada com sucesso! Verifique o seu email.");
            RegistoConcluido?.Invoke(this, new RegistoEventArgs(utilizadorId.Value));
            
            var timer = new Avalonia.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, args) => { timer.Stop(); this.Close(); };
            timer.Start();
        }
        else
        {
            MostrarErro(mensagemErro ?? "Erro ao criar conta.");
            HabilitarBotoes();
        }
    }
    
    private void BtnCancelar_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    private void MostrarErro(string mensagem)
    {
        TxtErro.Text = mensagem;
        BorderErro.IsVisible = true;
    }
    
    private void MostrarSucesso(string mensagem)
    {
        TxtSucesso.Text = mensagem;
        BorderSucesso.IsVisible = true;
    }
    
    private void EsconderMensagens()
    {
        BorderErro.IsVisible = false;
        BorderSucesso.IsVisible = false;
    }
    
    private void DesabilitarBotoes()
    {
        BtnRegistar.IsEnabled = false;
        BtnCancelar.IsEnabled = false;
    }
    
    private void HabilitarBotoes()
    {
        BtnRegistar.IsEnabled = true;
        BtnCancelar.IsEnabled = true;
    }
}