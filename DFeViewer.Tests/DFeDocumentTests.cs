using DFeViewer.Models;

namespace DFeViewer.Tests;

public class DFeDocumentTests
{
    // ── TipoLabel ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TipoDFe.NFe,         "NF-e")]
    [InlineData(TipoDFe.NFCe,        "NFC-e")]
    [InlineData(TipoDFe.CTe,         "CT-e")]
    [InlineData(TipoDFe.MDFe,        "MDF-e")]
    [InlineData(TipoDFe.Desconhecido,"Desconhecido")]
    public void TipoLabel_RetornaStringCorreta(TipoDFe tipo, string esperado)
    {
        var doc = new DFeDocument { Tipo = tipo };
        doc.TipoLabel.Should().Be(esperado);
    }

    // ── NomeArquivo ───────────────────────────────────────────────────────────

    [Fact]
    public void NomeArquivo_RetornaApenasNomeDoArquivo()
    {
        var doc = new DFeDocument { CaminhoArquivo = @"C:\pasta\subpasta\nota.xml" };
        doc.NomeArquivo.Should().Be("nota.xml");
    }

    [Fact]
    public void NomeArquivo_CaminhoVazio_RetornaVazio()
    {
        var doc = new DFeDocument { CaminhoArquivo = string.Empty };
        doc.NomeArquivo.Should().Be(string.Empty);
    }

    // ── Valores padrão ────────────────────────────────────────────────────────

    [Fact]
    public void NovoDocumento_ValorTotal_EhZero()
    {
        var doc = new DFeDocument();
        doc.ValorTotal.Should().Be(0m);
    }

    [Fact]
    public void NovoDocumento_Itens_EhListaVazia()
    {
        var doc = new DFeDocument();
        doc.Itens.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void NovoDocumento_DadosExtras_EhDicionarioVazio()
    {
        var doc = new DFeDocument();
        doc.DadosExtras.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void NovoDocumento_DataEmissao_EhNula()
    {
        var doc = new DFeDocument();
        doc.DataEmissao.Should().BeNull();
    }

    // ── ItemNFe ───────────────────────────────────────────────────────────────

    [Fact]
    public void ItemNFe_ValoresDecimais_SaoZeroPorPadrao()
    {
        var item = new ItemNFe();
        item.Quantidade.Should().Be(0m);
        item.ValorUnitario.Should().Be(0m);
        item.ValorTotal.Should().Be(0m);
    }

    [Fact]
    public void ItemNFe_StringsVaziasPorPadrao()
    {
        var item = new ItemNFe();
        item.Codigo.Should().BeEmpty();
        item.Descricao.Should().BeEmpty();
        item.Ncm.Should().BeEmpty();
        item.Cfop.Should().BeEmpty();
        item.Unidade.Should().BeEmpty();
    }
}
