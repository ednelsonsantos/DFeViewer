using DFeViewer.Models;
using DFeViewer.Services;
using QuestPDF.Infrastructure;

namespace DFeViewer.Tests;

public class PdfExportServiceTests
{
    private readonly PdfExportService _sut = new();

    static PdfExportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static DFeDocument CriarDocumentoNFe() => new()
    {
        Tipo = TipoDFe.NFe,
        NumeroDocumento = "999",
        Serie = "1",
        EmitenteNome = "EMPRESA TESTE",
        EmitenteCnpj = "12345678000195",
        DestinatarioNome = "CLIENTE TESTE",
        DestinatarioCnpjCpf = "98765432000188",
        ValorTotal = 1500.00m,
        Situacao = "Autorizada",
        DataEmissao = new DateTime(2024, 1, 15, 10, 30, 0),
        ChaveAcesso = "35240112345678000195550010000009991234567890",
        DadosExtras = new Dictionary<string, string>
        {
            ["Natureza da Operação"] = "VENDA",
            ["Tipo de Operação"] = "Saída",
            ["Emitente - Endereço"] = "RUA TESTE, 100, SP",
            ["Destinatário - Endereço"] = "AV EXEMPLO, 200, RJ",
            ["Valor dos Produtos"] = "R$ 1500,00",
            ["Valor do ICMS"] = "R$ 180,00",
            ["Informações Adicionais"] = "Nota de teste"
        },
        Itens =
        [
            new ItemNFe { Numero = 1, Codigo = "001", Descricao = "PRODUTO A", Ncm = "84713012",
                          Cfop = "5102", Unidade = "UN", Quantidade = 2, ValorUnitario = 500, ValorTotal = 1000 },
            new ItemNFe { Numero = 2, Codigo = "002", Descricao = "PRODUTO B", Ncm = "84713012",
                          Cfop = "5102", Unidade = "KG", Quantidade = 10, ValorUnitario = 50, ValorTotal = 500 }
        ]
    };

    private static DFeDocument CriarDocumentoCTe() => new()
    {
        Tipo = TipoDFe.CTe,
        NumeroDocumento = "45",
        Serie = "1",
        EmitenteNome = "TRANSPORTADORA LTDA",
        EmitenteCnpj = "12345678000195",
        DestinatarioNome = "DESTINATARIO SA",
        DestinatarioCnpjCpf = "22222222000200",
        ValorTotal = 350.00m,
        Situacao = "Autorizado",
        DataEmissao = new DateTime(2024, 2, 20),
        DadosExtras = new Dictionary<string, string>
        {
            ["CFOP"] = "5353",
            ["Modal"] = "1",
            ["Município Início"] = "SAO PAULO",
            ["Município Fim"] = "RIO DE JANEIRO",
            ["UF Início"] = "SP",
            ["UF Fim"] = "RJ",
            ["Tipo de Serviço"] = "0",
            ["Valor Recebedor"] = "R$ 350,00"
        }
    };

    private static DFeDocument CriarDocumentoMDFe() => new()
    {
        Tipo = TipoDFe.MDFe,
        NumeroDocumento = "12",
        Serie = "1",
        EmitenteNome = "TRANSPORTADORA LTDA",
        EmitenteCnpj = "12345678000195",
        ValorTotal = 5000.00m,
        Situacao = "Autorizado",
        DataEmissao = new DateTime(2024, 3, 10),
        DadosExtras = new Dictionary<string, string>
        {
            ["Modal"] = "1",
            ["UF Início"] = "SP",
            ["UF Fim"] = "MG",
            ["Qtd CT-e"] = "1",
            ["Qtd NF-e"] = "0",
            ["Peso Total"] = "1000,000 kg",
            ["Placa"] = "ABC1234",
            ["RENAVAM"] = "12345678901"
        }
    };

    // ── Geração de PDF (smoke tests) ─────────────────────────────────────────

    [Fact]
    public void GerarPdf_DocumentoNFe_RetornaBytesNaoVazios()
    {
        var pdf = _sut.GerarPdf(CriarDocumentoNFe());
        pdf.Should().NotBeNullOrEmpty();
        pdf.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GerarPdf_DocumentoCTe_RetornaBytesNaoVazios()
    {
        var pdf = _sut.GerarPdf(CriarDocumentoCTe());
        pdf.Should().NotBeNullOrEmpty();
        pdf.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GerarPdf_DocumentoMDFe_RetornaBytesNaoVazios()
    {
        var pdf = _sut.GerarPdf(CriarDocumentoMDFe());
        pdf.Should().NotBeNullOrEmpty();
        pdf.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void GerarPdf_DocumentoDesconhecido_RetornaBytesNaoVazios()
    {
        var doc = CriarDocumentoNFe();
        doc.Tipo = TipoDFe.Desconhecido;
        var pdf = _sut.GerarPdf(doc);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GerarPdf_ResultadoEhPdf_ComeçaComHeaderPDF()
    {
        var pdf = _sut.GerarPdf(CriarDocumentoNFe());
        // Header mágico de PDF: %PDF
        var header = System.Text.Encoding.ASCII.GetString(pdf[..4]);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GerarPdf_ComItens_NaoLancaExcecao()
    {
        var doc = CriarDocumentoNFe();
        var acao = () => _sut.GerarPdf(doc);
        acao.Should().NotThrow();
    }

    [Fact]
    public void GerarPdf_DocumentoSemItens_NaoLancaExcecao()
    {
        var doc = CriarDocumentoNFe();
        doc.Itens.Clear();
        var acao = () => _sut.GerarPdf(doc);
        acao.Should().NotThrow();
    }

    [Fact]
    public void GerarPdf_DocumentoSemDadosExtras_NaoLancaExcecao()
    {
        var doc = CriarDocumentoNFe();
        doc.DadosExtras.Clear();
        var acao = () => _sut.GerarPdf(doc);
        acao.Should().NotThrow();
    }

    [Fact]
    public void GerarPdf_DocumentoComValorZero_NaoLancaExcecao()
    {
        var doc = CriarDocumentoCTe();
        doc.ValorTotal = 0m;
        var acao = () => _sut.GerarPdf(doc);
        acao.Should().NotThrow();
    }

    [Fact]
    public void GerarPdf_ChamadasConsecutivas_ProduzemResultadosIndependentes()
    {
        var pdf1 = _sut.GerarPdf(CriarDocumentoNFe());
        var pdf2 = _sut.GerarPdf(CriarDocumentoCTe());
        pdf1.Should().NotEqual(pdf2);
    }
}
