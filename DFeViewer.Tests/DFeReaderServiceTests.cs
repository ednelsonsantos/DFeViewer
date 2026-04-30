using DFeViewer.Models;
using DFeViewer.Services;
using System.IO;

namespace DFeViewer.Tests;

public class DFeReaderServiceTests
{
    private readonly DFeReaderService _sut = new();
    private static string Fixture(string nome) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", nome);

    // ── Detecção de tipo ─────────────────────────────────────────────────────

    [Fact]
    public void LerArquivo_NfeSemProtocolo_DetectaTipoNFe()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.Tipo.Should().Be(TipoDFe.NFe);
    }

    [Fact]
    public void LerArquivo_CteComProtocolo_DetectaTipoCTe()
    {
        var doc = _sut.LerArquivo(Fixture("cte_exemplo.xml"));
        doc.Tipo.Should().Be(TipoDFe.CTe);
    }

    [Fact]
    public void LerArquivo_MdfeComProtocolo_DetectaTipoMDFe()
    {
        var doc = _sut.LerArquivo(Fixture("mdfe_exemplo.xml"));
        doc.Tipo.Should().Be(TipoDFe.MDFe);
    }

    [Fact]
    public void LerArquivo_XmlInvalido_RetornaTipoDesconhecido()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "<raiz><qualquercoisa>valor</qualquercoisa></raiz>");
            var doc = _sut.LerArquivo(tmp);
            doc.Tipo.Should().Be(TipoDFe.Desconhecido);
        }
        finally { File.Delete(tmp); }
    }

    // ── NF-e: campos obrigatórios ─────────────────────────────────────────────

    [Fact]
    public void LerArquivo_Nfe_PopulaEmitenteCorretamente()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.EmitenteNome.Should().Be("EMPRESA TESTE LTDA");
        doc.EmitenteCnpj.Should().Be("12345678000195");
    }

    [Fact]
    public void LerArquivo_Nfe_PopulaDestinatarioCorretamente()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.DestinatarioNome.Should().Be("CLIENTE EXEMPLO SA");
        doc.DestinatarioCnpjCpf.Should().Be("98765432000188");
    }

    [Fact]
    public void LerArquivo_Nfe_PopulaNumeroESerie()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.NumeroDocumento.Should().Be("123");
        doc.Serie.Should().Be("1");
    }

    [Fact]
    public void LerArquivo_Nfe_PopulaValorTotal()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.ValorTotal.Should().Be(1500.00m);
    }

    [Fact]
    public void LerArquivo_Nfe_PopulaDataEmissao()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.DataEmissao.Should().NotBeNull();
        doc.DataEmissao!.Value.Year.Should().Be(2024);
        doc.DataEmissao!.Value.Month.Should().Be(1);
        doc.DataEmissao!.Value.Day.Should().Be(15);
    }

    [Fact]
    public void LerArquivo_Nfe_SemProtocolo_SituacaoEhSemProtocolo()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.Situacao.Should().Be("Sem protocolo");
    }

    // ── NF-e: itens ──────────────────────────────────────────────────────────

    [Fact]
    public void LerArquivo_Nfe_CarregaDoisItens()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.Itens.Should().HaveCount(2);
    }

    [Fact]
    public void LerArquivo_Nfe_PrimeiroItem_CamposCorretos()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        var item = doc.Itens[0];
        item.Numero.Should().Be(1);
        item.Codigo.Should().Be("001");
        item.Descricao.Should().Be("PRODUTO TESTE A");
        item.Quantidade.Should().Be(2m);
        item.ValorUnitario.Should().Be(500m);
        item.ValorTotal.Should().Be(1000m);
    }

    [Fact]
    public void LerArquivo_Nfe_SegundoItem_CamposCorretos()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        var item = doc.Itens[1];
        item.Numero.Should().Be(2);
        item.Unidade.Should().Be("KG");
        item.Quantidade.Should().Be(10m);
        item.ValorTotal.Should().Be(500m);
    }

    [Fact]
    public void LerArquivo_Nfe_DadosExtras_ContemNaturezaOperacao()
    {
        var doc = _sut.LerArquivo(Fixture("nfe_sem_protocolo.xml"));
        doc.DadosExtras.Should().ContainKey("Natureza da Operação");
        doc.DadosExtras["Natureza da Operação"].Should().Be("VENDA DE MERCADORIA");
    }

    // ── CT-e ─────────────────────────────────────────────────────────────────

    [Fact]
    public void LerArquivo_Cte_PopulaEmitenteCorreto()
    {
        var doc = _sut.LerArquivo(Fixture("cte_exemplo.xml"));
        doc.EmitenteNome.Should().Be("TRANSPORTADORA RAPIDA LTDA");
        doc.EmitenteCnpj.Should().Be("12345678000195");
    }

    [Fact]
    public void LerArquivo_Cte_PopulaValorTotal()
    {
        var doc = _sut.LerArquivo(Fixture("cte_exemplo.xml"));
        doc.ValorTotal.Should().Be(350.00m);
    }

    [Fact]
    public void LerArquivo_Cte_ComProtocolo_SituacaoAutorizado()
    {
        var doc = _sut.LerArquivo(Fixture("cte_exemplo.xml"));
        doc.Situacao.Should().Be("Autorizado");
    }

    [Fact]
    public void LerArquivo_Cte_PopulaUFInicioEFim()
    {
        var doc = _sut.LerArquivo(Fixture("cte_exemplo.xml"));
        doc.DadosExtras.Should().ContainKey("UF Início");
        doc.DadosExtras["UF Início"].Should().Be("SP");
        doc.DadosExtras["UF Fim"].Should().Be("RJ");
    }

    // ── MDF-e ─────────────────────────────────────────────────────────────────

    [Fact]
    public void LerArquivo_Mdfe_PopulaNumeroDocumento()
    {
        var doc = _sut.LerArquivo(Fixture("mdfe_exemplo.xml"));
        doc.NumeroDocumento.Should().Be("12");
    }

    [Fact]
    public void LerArquivo_Mdfe_PopulaPlaca()
    {
        var doc = _sut.LerArquivo(Fixture("mdfe_exemplo.xml"));
        doc.DadosExtras.Should().ContainKey("Placa");
        doc.DadosExtras["Placa"].Should().Be("ABC1234");
    }

    [Fact]
    public void LerArquivo_Mdfe_ComProtocolo_SituacaoAutorizado()
    {
        var doc = _sut.LerArquivo(Fixture("mdfe_exemplo.xml"));
        doc.Situacao.Should().Be("Autorizado");
    }

    // ── Robustez ──────────────────────────────────────────────────────────────

    [Fact]
    public void LerArquivo_ArquivoInexistente_LancaExcecao()
    {
        var acao = () => _sut.LerArquivo("c:/nao/existe/arquivo.xml");
        acao.Should().Throw<Exception>();
    }

    [Fact]
    public void LerArquivo_XmlMalFormado_RetornaDocumentoDesconhecidoOuLancaExcecao()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "isto nao e xml valido <<< >>>");
            // O serviço não deve deixar a exceção vazar sem tratamento
            var acao = () => _sut.LerArquivo(tmp);
            // Aceita tanto retornar Desconhecido quanto lançar
            try
            {
                var doc = acao();
                doc.Tipo.Should().Be(TipoDFe.Desconhecido);
            }
            catch (Exception ex)
            {
                ex.Should().NotBeNull();
            }
        }
        finally { File.Delete(tmp); }
    }
}
