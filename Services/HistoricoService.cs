using System.IO;
using System.Text.Json;

namespace DFeViewer.Services;

public class HistoricoService
{
    private const int MaxRecentes = 20;
    private static readonly string ArquivoHistorico = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DFeViewer", "historico.json");

    public List<string> CarregarRecentes()
    {
        try
        {
            if (!File.Exists(ArquivoHistorico)) return [];
            var json = File.ReadAllText(ArquivoHistorico);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch { return []; }
    }

    public void AdicionarRecente(string caminho)
    {
        try
        {
            var lista = CarregarRecentes();
            lista.Remove(caminho);
            lista.Insert(0, caminho);
            if (lista.Count > MaxRecentes) lista = lista[..MaxRecentes];
            Directory.CreateDirectory(Path.GetDirectoryName(ArquivoHistorico)!);
            File.WriteAllText(ArquivoHistorico, JsonSerializer.Serialize(lista));
        }
        catch { }
    }

    public void RemoverInexistentes()
    {
        try
        {
            var lista = CarregarRecentes().Where(File.Exists).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(ArquivoHistorico)!);
            File.WriteAllText(ArquivoHistorico, JsonSerializer.Serialize(lista));
        }
        catch { }
    }
}
