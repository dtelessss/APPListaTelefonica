using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace AppListaTelefonica.Views;

public partial class VerificacaoEmailWindow : Window
{
    private readonly Services.AuthService _authService;
    private Guid _utilizadorId;
    
    public VerificacaoEmailWindow() : this(Guid.Empty)
    {
    }
    
    public VerificacaoEmailWindow(Guid utilizadorId)
    {
        InitializeComponent();
        _authService = new Services.AuthService();
        _utilizadorId = utilizadorId;
    }
    
    private async void BtnVerificar_Click(object? sender, RoutedEventArgs e)
    {
        EsconderMensagens();
        DesabilitarBotoes();
        
        string codigo = TxtCodigo.Text?.Trim() ?? "";
        
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
        {
            MostrarErro("Por favor, introduza o codigo de 6 digitos.");
            HabilitarBotoes();
            return;
        }
        
        var (sucesso, mensagemErro) = await _authService.VerificarEmailAsync(_utilizadorId, codigo);
        
        if (sucesso)
        {
            MostrarSucesso("Email verificado com sucesso! Ja pode fazer login.");
            
            var timer = new Avalonia.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, args) => { timer.Stop(); this.Close(); };
            timer.Start();
        }
        else
        {
            MostrarErro(mensagemErro ?? "Erro ao verificar codigo.");
            HabilitarBotoes();
        }
    }
    
    private async void BtnReenviar_Click(object? sender, RoutedEventArgs e)
    {
        EsconderMensagens();
        DesabilitarBotoes();
        
        var (sucesso, mensagemErro) = await _authService.ReenviarCodigoVerificacaoAsync(_utilizadorId);
        
        if (sucesso)
        {
            MostrarSucesso("Novo codigo enviado! Verifique o seu email.");
        }
        else
        {
            MostrarErro(mensagemErro ?? "Erro ao reenviar codigo.");
        }
        
        HabilitarBotoes();
    }
    
    private void MostrarErro(string mensagem)
    {
        TxtErro.Text = mensagem;
        BorderErro.IsVisible = true;
        BorderSucesso.IsVisible = false;
    }
    
    private void MostrarSucesso(string mensagem)
    {
        TxtSucesso.Text = mensagem;
        BorderSucesso.IsVisible = true;
        BorderErro.IsVisible = false;
    }
    
    private void EsconderMensagens()
    {
        BorderErro.IsVisible = false;
        BorderSucesso.IsVisible = false;
    }
    
    private void DesabilitarBotoes()
    {
        BtnVerificar.IsEnabled = false;
        BtnReenviar.IsEnabled = false;
    }
    
    private void HabilitarBotoes()
    {
        BtnVerificar.IsEnabled = true;
        BtnReenviar.IsEnabled = true;
    }
}