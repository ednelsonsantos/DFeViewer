using DFeViewer.Converters;
using DFeViewer.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DFeViewer.Tests;

public class ConvertersTests
{
    private static readonly CultureInfo Ptbr = new("pt-BR");

    // ── ZeroToVisibleConverter ───────────────────────────────────────────────

    [Fact]
    public void ZeroToVisible_Zero_RetornaVisible()
    {
        var c = new ZeroToVisibleConverter();
        c.Convert(0, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void ZeroToVisible_Positivo_RetornaCollapsed()
    {
        var c = new ZeroToVisibleConverter();
        c.Convert(5, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Collapsed);
    }

    // ── NonZeroToVisibleConverter ────────────────────────────────────────────

    [Fact]
    public void NonZeroToVisible_Positivo_RetornaVisible()
    {
        var c = new NonZeroToVisibleConverter();
        c.Convert(3, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void NonZeroToVisible_Zero_RetornaCollapsed()
    {
        var c = new NonZeroToVisibleConverter();
        c.Convert(0, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Collapsed);
    }

    // ── BoolToVisibleConverter ───────────────────────────────────────────────

    [Fact]
    public void BoolToVisible_True_RetornaVisible()
    {
        var c = new BoolToVisibleConverter();
        c.Convert(true, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void BoolToVisible_False_RetornaCollapsed()
    {
        var c = new BoolToVisibleConverter();
        c.Convert(false, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Collapsed);
    }

    // ── BoolToInvisibleConverter ─────────────────────────────────────────────

    [Fact]
    public void BoolToInvisible_True_RetornaCollapsed()
    {
        var c = new BoolToInvisibleConverter();
        c.Convert(true, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void BoolToInvisible_False_RetornaVisible()
    {
        var c = new BoolToInvisibleConverter();
        c.Convert(false, typeof(Visibility), null, Ptbr).Should().Be(Visibility.Visible);
    }

    // ── TipoToColorConverter ─────────────────────────────────────────────────

    [Theory]
    [InlineData(TipoDFe.NFe)]
    [InlineData(TipoDFe.NFCe)]
    [InlineData(TipoDFe.CTe)]
    [InlineData(TipoDFe.MDFe)]
    [InlineData(TipoDFe.Desconhecido)]
    public void TipoToColor_QualquerTipo_RetornaBrush(TipoDFe tipo)
    {
        var c = new TipoToColorConverter();
        var resultado = c.Convert(tipo, typeof(Brush), null, Ptbr);
        resultado.Should().BeAssignableTo<SolidColorBrush>();
    }

    [Fact]
    public void TipoToColor_TiposDistintos_RetornamCoresDiferentes()
    {
        var c = new TipoToColorConverter();
        var corNFe  = ((SolidColorBrush)c.Convert(TipoDFe.NFe,  typeof(Brush), null, Ptbr)).Color;
        var corCTe  = ((SolidColorBrush)c.Convert(TipoDFe.CTe,  typeof(Brush), null, Ptbr)).Color;
        var corMDFe = ((SolidColorBrush)c.Convert(TipoDFe.MDFe, typeof(Brush), null, Ptbr)).Color;
        corNFe.Should().NotBe(corCTe);
        corNFe.Should().NotBe(corMDFe);
        corCTe.Should().NotBe(corMDFe);
    }

    [Fact]
    public void TipoToColor_ValorNulo_RetornaCinza()
    {
        var c = new TipoToColorConverter();
        var resultado = (SolidColorBrush)c.Convert(null!, typeof(Brush), null, Ptbr);
        resultado.Color.Should().Be(Colors.Gray);
    }

    // ── SituacaoToColorConverter ─────────────────────────────────────────────

    [Theory]
    [InlineData("Autorizada")]
    [InlineData("Autorizado")]
    [InlineData("AUTORIZADO o uso")]
    public void SituacaoToColor_Autorizado_RetornaVerde(string situacao)
    {
        var c = new SituacaoToColorConverter();
        var brush = (SolidColorBrush)c.Convert(situacao, typeof(Brush), null, Ptbr);
        brush.Color.G.Should().BeGreaterThan(brush.Color.R); // verde tem G > R
    }

    [Fact]
    public void SituacaoToColor_ComErro_RetornaVermelho()
    {
        var c = new SituacaoToColorConverter();
        var brush = (SolidColorBrush)c.Convert("Erro ao processar", typeof(Brush), null, Ptbr);
        brush.Color.R.Should().BeGreaterThan(brush.Color.G);
    }

    [Fact]
    public void SituacaoToColor_Outro_RetornaLaranja()
    {
        var c = new SituacaoToColorConverter();
        var brush = (SolidColorBrush)c.Convert("cStat: 218", typeof(Brush), null, Ptbr);
        // laranja: R alto, G médio, B baixo
        brush.Color.R.Should().BeGreaterThan(100);
    }

    // ── SomaTotalConverter ───────────────────────────────────────────────────

    [Fact]
    public void SomaTotal_ListaDeItens_RetornaSomaCorreta()
    {
        var c = new SomaTotalConverter();
        var itens = new List<ItemNFe>
        {
            new() { ValorTotal = 100m },
            new() { ValorTotal = 250m },
            new() { ValorTotal = 50m }
        };
        var resultado = c.Convert(itens, typeof(decimal), null, Ptbr);
        resultado.Should().Be(400m);
    }

    [Fact]
    public void SomaTotal_ListaVazia_RetornaZero()
    {
        var c = new SomaTotalConverter();
        var resultado = c.Convert(new List<ItemNFe>(), typeof(decimal), null, Ptbr);
        resultado.Should().Be(0m);
    }

    [Fact]
    public void SomaTotal_ValorNulo_RetornaZero()
    {
        var c = new SomaTotalConverter();
        var resultado = c.Convert(null!, typeof(decimal), null, Ptbr);
        resultado.Should().Be(0m);
    }

    // ── NomeArquivoConverter ─────────────────────────────────────────────────

    [Fact]
    public void NomeArquivo_CaminhoCompleto_RetornaApenasNome()
    {
        var c = new NomeArquivoConverter();
        var resultado = c.Convert(@"C:\pasta\subpasta\nota.xml", typeof(string), null, Ptbr);
        resultado.Should().Be("nota.xml");
    }

    [Fact]
    public void NomeArquivo_SomenteNome_RetornaOMesmo()
    {
        var c = new NomeArquivoConverter();
        var resultado = c.Convert("nota.xml", typeof(string), null, Ptbr);
        resultado.Should().Be("nota.xml");
    }

    [Fact]
    public void NomeArquivo_ValorNaoString_RetornaValorOriginal()
    {
        var c = new NomeArquivoConverter();
        var resultado = c.Convert(42, typeof(string), null, Ptbr);
        resultado.Should().Be(42);
    }
}
