using DFeViewer.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace DFeViewer.Views;

public partial class MainWindow : Window
{
    private MainViewModel VM => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var arquivos = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var arquivo in arquivos.Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            _ = VM.CarregarArquivoAsync(arquivo);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        VM.LimparTemporarios();
    }
}
