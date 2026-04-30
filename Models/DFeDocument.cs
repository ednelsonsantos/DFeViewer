using System.IO;

namespace DFeViewer.Models;

/// <summary>Tipo de Documento Fiscal Eletrônico</summary>
public enum TipoDFe
{
    NFe,
    NFCe,
    CTe,
    MDFe,
    Desconhecido
}

/// <summary>Modelo genérico que representa qualquer DFe carregado</summary>
public class DFeDocument
{
    public TipoDFe Tipo { get; set; }
    public string CaminhoArquivo { get; set; } = string.Empty;
    public string NomeArquivo => Path.GetFileName(CaminhoArquivo);
    public string ChaveAcesso { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public DateTime? DataEmissao { get; set; }
    public string EmitenteCnpj { get; set; } = string.Empty;
    public string EmitenteNome { get; set; } = string.Empty;
    public string DestinatarioCnpjCpf { get; set; } = string.Empty;
    public string DestinatarioNome { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public string Situacao { get; set; } = string.Empty;
    public string XmlConteudo { get; set; } = string.Empty;

    // Dados específicos por tipo (armazenados dinamicamente)
    public Dictionary<string, string> DadosExtras { get; set; } = new();

    // Para NF-e: lista de itens
    public List<ItemNFe> Itens { get; set; } = new();

    public string TipoLabel => Tipo switch
    {
        TipoDFe.NFe  => "NF-e",
        TipoDFe.NFCe => "NFC-e",
        TipoDFe.CTe  => "CT-e",
        TipoDFe.MDFe => "MDF-e",
        _            => "Desconhecido"
    };
}

public class ItemNFe
{
    public int Numero { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string Cfop { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}
