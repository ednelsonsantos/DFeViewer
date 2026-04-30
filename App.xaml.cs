using DFeViewer.ViewModels;
using DFeViewer.Views;
using QuestPDF.Infrastructure;
using System.Windows;

namespace DFeViewer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        // Abre arquivos passados como argumento de linha de comando
        if (e.Args.Length > 0)
        {
            var vm = (MainViewModel)window.DataContext;
            foreach (var arg in e.Args.Where(a => a.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                                               && System.IO.File.Exists(a)))
                _ = vm.CarregarArquivoAsync(arg);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Limpa PDFs temporários gerados na sessão
        if (MainWindow?.DataContext is MainViewModel vm)
            vm.LimparTemporarios();

        base.OnExit(e);
    }
}
