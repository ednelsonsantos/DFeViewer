using DFeViewer.Models;
using DFeViewer.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DFeViewer.ViewModels;

public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DFeReaderService _reader  = new();
    private readonly PdfExportService _pdf     = new();
    private readonly HistoricoService _historico = new();
    private readonly TemaService      _tema    = new();

    // PDFs temporários gerados na sessão para limpeza ao fechar
    private readonly List<string> _tempPdfs = [];

    private DFeDocument? _documentoSelecionado;
    private bool         _carregando;
    private string       _statusMsg = "Arraste um XML aqui ou clique em 'Abrir XML'";
    private string       _filtroTexto = string.Empty;

    // ── Coleção fonte e view filtrada ────────────────────────────────────────
    public ObservableCollection<DFeDocument> Documentos { get; } = new();
    public ICollectionView DocumentosView { get; }

    // ── Histórico de arquivos recentes ───────────────────────────────────────
    public ObservableCollection<string> Recentes { get; } = new();

    public DFeDocument? DocumentoSelecionado
    {
        get => _documentoSelecionado;
        set { _documentoSelecionado = value; OnPropertyChanged(); OnPropertyChanged(nameof(TemDocumento)); }
    }

    public bool TemDocumento => _documentoSelecionado != null;

    public bool Carregando
    {
        get => _carregando;
        set { _carregando = value; OnPropertyChanged(); }
    }

    public string StatusMsg
    {
        get => _statusMsg;
        set { _statusMsg = value; OnPropertyChanged(); }
    }

    public string FiltroTexto
    {
        get => _filtroTexto;
        set
        {
            _filtroTexto = value;
            OnPropertyChanged();
            DocumentosView.Refresh();
        }
    }

    public bool TemaEscuro => _tema.TemaEscuro;

    // ── Comandos (readonly — instância única por ViewModel) ──────────────────
    public ICommand AbrirXmlCommand      { get; }
    public ICommand ImprimirCommand      { get; }
    public ICommand ExportarCommand      { get; }
    public ICommand RemoverCommand       { get; }
    public ICommand LimparTudoCommand    { get; }
    public ICommand AlternarTemaCommand  { get; }
    public ICommand CopiarChaveCommand   { get; }
    public ICommand AbrirRecenteCommand  { get; }
    public ICommand LimparFiltroCommand  { get; }

    public MainViewModel()
    {
        AbrirXmlCommand     = new RelayCommand(_ => _ = AbrirXmlAsync());
        ImprimirCommand     = new RelayCommand(_ => _ = ImprimirAsync(),    _ => TemDocumento);
        ExportarCommand     = new RelayCommand(_ => _ = ExportarPdfAsync(), _ => TemDocumento);
        RemoverCommand      = new RelayCommand(doc => RemoverDocumento(doc as DFeDocument));
        LimparTudoCommand   = new RelayCommand(_ => LimparTudo());
        AlternarTemaCommand = new RelayCommand(_ => AlternarTema());
        CopiarChaveCommand  = new RelayCommand(_ => CopiarChave(), _ => TemDocumento);
        AbrirRecenteCommand = new RelayCommand(path => _ = CarregarArquivoAsync(path as string ?? string.Empty));
        LimparFiltroCommand = new RelayCommand(_ => FiltroTexto = string.Empty);

        DocumentosView = CollectionViewSource.GetDefaultView(Documentos);
        DocumentosView.Filter = FiltrarDocumento;

        CarregarRecentes();
        _historico.RemoverInexistentes();
    }

    // ── Filtro ───────────────────────────────────────────────────────────────
    private bool FiltrarDocumento(object obj)
    {
        if (string.IsNullOrWhiteSpace(_filtroTexto)) return true;
        if (obj is not DFeDocument doc) return false;
        var f = _filtroTexto.Trim();
        return doc.EmitenteNome.Contains(f, StringComparison.OrdinalIgnoreCase)
            || doc.DestinatarioNome.Contains(f, StringComparison.OrdinalIgnoreCase)
            || doc.NumeroDocumento.Contains(f, StringComparison.OrdinalIgnoreCase)
            || doc.ChaveAcesso.Contains(f, StringComparison.OrdinalIgnoreCase)
            || doc.TipoLabel.Contains(f, StringComparison.OrdinalIgnoreCase);
    }

    // ── Ações ─────────────────────────────────────────────────────────────────
    private async Task AbrirXmlAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title       = "Selecionar XML de DFe",
            Filter      = "Arquivos XML (*.xml)|*.xml|Todos os arquivos (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog() != true) return;

        foreach (var arquivo in dlg.FileNames)
            await CarregarArquivoAsync(arquivo);
    }

    public async Task CarregarArquivoAsync(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return;

        try
        {
            Carregando = true;
            StatusMsg  = $"Lendo {Path.GetFileName(caminho)}...";

            var doc = await Task.Run(() => _reader.LerArquivo(caminho));

            // Detecta duplicata por chave de acesso
            if (!string.IsNullOrEmpty(doc.ChaveAcesso)
                && Documentos.Any(d => d.ChaveAcesso == doc.ChaveAcesso))
            {
                StatusMsg = $"Documento já carregado: {doc.TipoLabel} Nº {doc.NumeroDocumento}";
                return;
            }

            Documentos.Add(doc);
            DocumentoSelecionado = doc;
            StatusMsg = $"✓ {doc.TipoLabel} Nº {doc.NumeroDocumento} carregado — {doc.EmitenteNome}";

            _historico.AdicionarRecente(caminho);
            CarregarRecentes();
        }
        catch (Exception ex)
        {
            StatusMsg = $"Erro ao carregar: {ex.Message}";
            MessageBox.Show($"Não foi possível ler o arquivo:\n\n{ex.Message}",
                            "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task ImprimirAsync()
    {
        if (_documentoSelecionado == null) return;
        var pdfPath = await GerarPdfAsync(_documentoSelecionado);
        if (pdfPath != null)
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
    }

    private async Task ExportarPdfAsync()
    {
        if (_documentoSelecionado == null) return;

        var dlg = new SaveFileDialog
        {
            Title      = "Salvar DANFE em PDF",
            Filter     = "PDF (*.pdf)|*.pdf",
            FileName   = $"DANFE_{_documentoSelecionado.TipoLabel}_{_documentoSelecionado.NumeroDocumento}.pdf",
            DefaultExt = ".pdf"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var pdfTemp = await GerarPdfAsync(_documentoSelecionado);
            if (pdfTemp == null) return;

            File.Copy(pdfTemp, dlg.FileName, overwrite: true);
            StatusMsg = $"PDF salvo em: {dlg.FileName}";

            var result = MessageBox.Show("PDF salvo com sucesso! Deseja abri-lo agora?",
                                         "Sucesso", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar PDF:\n\n{ex.Message}", "Erro",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<string?> GerarPdfAsync(DFeDocument doc)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(),
                $"DFeViewer_{doc.TipoLabel}_{doc.NumeroDocumento}_{Guid.NewGuid():N}.pdf");

            await Task.Run(() =>
            {
                var bytes = _pdf.GerarPdf(doc);
                File.WriteAllBytes(tempPath, bytes);
            });

            _tempPdfs.Add(tempPath);
            return tempPath;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar DANFE:\n\n{ex.Message}",
                            "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private void RemoverDocumento(DFeDocument? doc)
    {
        if (doc == null) return;
        Documentos.Remove(doc);
        if (DocumentoSelecionado == doc)
            DocumentoSelecionado = Documentos.FirstOrDefault();
    }

    private void LimparTudo()
    {
        Documentos.Clear();
        DocumentoSelecionado = null;
        StatusMsg = "Lista limpa.";
    }

    private void AlternarTema()
    {
        _tema.Alternar();
        OnPropertyChanged(nameof(TemaEscuro));
    }

    private void CopiarChave()
    {
        if (_documentoSelecionado?.ChaveAcesso is { Length: > 0 } chave)
        {
            Clipboard.SetText(chave);
            StatusMsg = "Chave de acesso copiada para a área de transferência.";
        }
    }

    private void CarregarRecentes()
    {
        Recentes.Clear();
        foreach (var r in _historico.CarregarRecentes())
            Recentes.Add(r);
    }

    // ── Limpeza de temporários ao encerrar ───────────────────────────────────
    public void LimparTemporarios()
    {
        foreach (var tmp in _tempPdfs)
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); }
            catch { }
        }
        _tempPdfs.Clear();
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
