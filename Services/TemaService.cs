using MaterialDesignThemes.Wpf;
using System.IO;
using System.Text.Json;

namespace DFeViewer.Services;

public class TemaService
{
    private static readonly string ArquivoConfig = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DFeViewer", "config.json");

    public bool TemaEscuro { get; private set; }

    public TemaService()
    {
        TemaEscuro = CarregarPreferencia();
        AplicarTema(TemaEscuro);
    }

    public void Alternar()
    {
        TemaEscuro = !TemaEscuro;
        AplicarTema(TemaEscuro);
        SalvarPreferencia(TemaEscuro);
    }

    private static void AplicarTema(bool escuro)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(escuro ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }

    private bool CarregarPreferencia()
    {
        try
        {
            if (!File.Exists(ArquivoConfig)) return false;
            var json = File.ReadAllText(ArquivoConfig);
            var cfg = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
            return cfg?.GetValueOrDefault("temaEscuro", false) ?? false;
        }
        catch { return false; }
    }

    private void SalvarPreferencia(bool escuro)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ArquivoConfig)!);
            File.WriteAllText(ArquivoConfig, JsonSerializer.Serialize(new Dictionary<string, bool> { ["temaEscuro"] = escuro }));
        }
        catch { }
    }
}
