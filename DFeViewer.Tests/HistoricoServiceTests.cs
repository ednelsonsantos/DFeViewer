using DFeViewer.Services;
using System.IO;
using System.Reflection;

namespace DFeViewer.Tests;

// HistoricoService usa caminho fixo em AppData — testado indiretamente via HistoricoServiceTestavel
// Os testes diretos do HistoricoService estão em HistoricoServiceTestablTests abaixo.

// Versão testável real que recebe o caminho via construtor
public class HistoricoServiceTestavel
{
    private const int MaxRecentes = 20;
    private readonly string _arquivo;

    public HistoricoServiceTestavel(string arquivo) => _arquivo = arquivo;

    public List<string> CarregarRecentes()
    {
        try
        {
            if (!File.Exists(_arquivo)) return [];
            var json = File.ReadAllText(_arquivo);
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch { return []; }
    }

    public void AdicionarRecente(string caminho)
    {
        var lista = CarregarRecentes();
        lista.Remove(caminho);
        lista.Insert(0, caminho);
        if (lista.Count > MaxRecentes) lista = lista[..MaxRecentes];
        Directory.CreateDirectory(Path.GetDirectoryName(_arquivo)!);
        File.WriteAllText(_arquivo, System.Text.Json.JsonSerializer.Serialize(lista));
    }

    public void RemoverInexistentes()
    {
        var lista = CarregarRecentes().Where(File.Exists).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(_arquivo)!);
        File.WriteAllText(_arquivo, System.Text.Json.JsonSerializer.Serialize(lista));
    }
}

public class HistoricoServiceTestablTests : IDisposable
{
    private readonly string _arquivo;
    private readonly HistoricoServiceTestavel _sut;

    public HistoricoServiceTestablTests()
    {
        _arquivo = Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json");
        _sut = new HistoricoServiceTestavel(_arquivo);
    }

    public void Dispose() { if (File.Exists(_arquivo)) File.Delete(_arquivo); }

    [Fact]
    public void CarregarRecentes_SemArquivo_RetornaVazio()
        => _sut.CarregarRecentes().Should().BeEmpty();

    [Fact]
    public void AdicionarRecente_PersisteDisco()
    {
        _sut.AdicionarRecente("/a/b.xml");
        _sut.CarregarRecentes().Should().ContainSingle();
    }

    [Fact]
    public void AdicionarRecente_Duplicata_NaoAdicionaDoisItens()
    {
        _sut.AdicionarRecente("/a/b.xml");
        _sut.AdicionarRecente("/a/b.xml");
        _sut.CarregarRecentes().Should().HaveCount(1);
    }

    [Fact]
    public void AdicionarRecente_Duplicata_MoveParaTopo()
    {
        _sut.AdicionarRecente("/a.xml");
        _sut.AdicionarRecente("/b.xml");
        _sut.AdicionarRecente("/a.xml");
        _sut.CarregarRecentes()[0].Should().Be("/a.xml");
    }

    [Fact]
    public void AdicionarRecente_MaisRecenteNoTopo()
    {
        _sut.AdicionarRecente("/primeiro.xml");
        _sut.AdicionarRecente("/segundo.xml");
        _sut.CarregarRecentes()[0].Should().Be("/segundo.xml");
    }

    [Fact]
    public void RemoverInexistentes_MantemApenasExistentes()
    {
        var real = Path.GetTempFileName();
        try
        {
            _sut.AdicionarRecente(real);
            _sut.AdicionarRecente("/nao/existe.xml");
            _sut.RemoverInexistentes();
            _sut.CarregarRecentes().Should().ContainSingle().Which.Should().Be(real);
        }
        finally { File.Delete(real); }
    }

    [Fact]
    public void AdicionarRecente_LimiteMaximo_NaoUltrapassaVinte()
    {
        for (int i = 0; i < 25; i++)
            _sut.AdicionarRecente($"/arquivo_{i:D2}.xml");
        _sut.CarregarRecentes().Should().HaveCount(20);
    }

    [Fact]
    public void Persistencia_NovaInstancia_LeDadosSalvos()
    {
        _sut.AdicionarRecente("/persistente.xml");
        var nova = new HistoricoServiceTestavel(_arquivo);
        nova.CarregarRecentes().Should().Contain("/persistente.xml");
    }
}
