using DFeViewer.Models;
using DFe.Utils;
using NFe.Classes;
using NFe.Utils.NFe;
using System.IO;
using System.Xml.Linq;

namespace DFeViewer.Services;

/// <summary>
/// Lê arquivos XML de DFe usando DFe.NET (NFe/NFCe via FuncoesXml),
/// com fallback via XDocument para CT-e e MDF-e.
/// </summary>
public class DFeReaderService
{
    public DFeDocument LerArquivo(string caminhoArquivo)
    {
        var xml = File.ReadAllText(caminhoArquivo);
        var tipo = DetectarTipo(xml);

        return tipo switch
        {
            TipoDFe.NFe  => LerNFe(caminhoArquivo, xml, false),
            TipoDFe.NFCe => LerNFe(caminhoArquivo, xml, true),
            TipoDFe.CTe  => LerCTeViaXml(caminhoArquivo, xml),
            TipoDFe.MDFe => LerMDFeViaXml(caminhoArquivo, xml),
            _            => new DFeDocument
            {
                Tipo = TipoDFe.Desconhecido,
                CaminhoArquivo = caminhoArquivo,
                XmlConteudo = xml,
                Situacao = "XML não reconhecido como DFe válido"
            }
        };
    }

    private static TipoDFe DetectarTipo(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var raiz = doc.Root?.Name.LocalName ?? string.Empty;
            var ns   = doc.Root?.Name.NamespaceName ?? string.Empty;

            // Detecta pelo nome do elemento raiz ou namespace canônico SEFAZ
            if (raiz is "nfeProc" or "NFe" || ns.Contains("nfe.fazenda.gov.br/nfe"))
            {
                var mod = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "mod")?.Value;
                return mod == "65" ? TipoDFe.NFCe : TipoDFe.NFe;
            }

            if (raiz is "cteProc" or "CTe" || ns.Contains("cte.fazenda.gov.br"))
                return TipoDFe.CTe;

            if (raiz is "mdfeProc" or "MDFe" || ns.Contains("mdfe.fazenda.gov.br"))
                return TipoDFe.MDFe;

            // Fallback: busca elemento raiz dentro de qualquer envelope
            var tagRaiz = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName is "NFe" or "nfeProc"
                                                       or "CTe" or "cteProc"
                                                       or "MDFe" or "mdfeProc");
            if (tagRaiz != null)
            {
                var local = tagRaiz.Name.LocalName;
                if (local is "NFe" or "nfeProc")
                {
                    var mod = doc.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName == "mod")?.Value;
                    return mod == "65" ? TipoDFe.NFCe : TipoDFe.NFe;
                }
                if (local is "CTe" or "cteProc") return TipoDFe.CTe;
                if (local is "MDFe" or "mdfeProc") return TipoDFe.MDFe;
            }
        }
        catch { }

        return TipoDFe.Desconhecido;
    }

    // ── NF-e / NFC-e ──────────────────────────────────────────────────────
    private static DFeDocument LerNFe(string caminho, string xml, bool isNFCe)
    {
        var doc = new DFeDocument
        {
            Tipo = isNFCe ? TipoDFe.NFCe : TipoDFe.NFe,
            CaminhoArquivo = caminho,
            XmlConteudo = xml,
            Situacao = "Sem protocolo"
        };

        try
        {
            nfeProc proc;

            if (xml.Contains("<nfeProc"))
            {
                proc = FuncoesXml.XmlStringParaClasse<nfeProc>(xml);
                var cStat = proc?.protNFe?.infProt?.cStat ?? 0;
                doc.Situacao = (cStat == 100 || cStat == 150) ? "Autorizada" : $"cStat: {cStat}";
            }
            else
            {
                var nfeSemProc = FuncoesXml.XmlStringParaClasse<NFe.Classes.NFe>(xml);
                proc = new nfeProc { NFe = nfeSemProc };
            }

            var nfe = proc?.NFe;
            if (nfe?.infNFe == null) return doc;

            var inf = nfe.infNFe;

            doc.ChaveAcesso     = inf.Id?.Replace("NFe", "") ?? string.Empty;
            doc.NumeroDocumento = inf.ide?.nNF.ToString() ?? string.Empty;
            doc.Serie           = inf.ide?.serie.ToString() ?? string.Empty;
            // dhEmi é DateTimeOffset (não nullable) — acessa via null-check no ide
            doc.DataEmissao     = inf.ide != null ? inf.ide.dhEmi.LocalDateTime : (DateTime?)null;

            doc.EmitenteCnpj        = inf.emit?.CNPJ ?? inf.emit?.CPF ?? string.Empty;
            doc.EmitenteNome        = inf.emit?.xNome ?? string.Empty;
            doc.DestinatarioCnpjCpf = inf.dest?.CNPJ ?? inf.dest?.CPF ?? string.Empty;
            doc.DestinatarioNome    = inf.dest?.xNome ?? string.Empty;
            doc.ValorTotal          = (decimal)(inf.total?.ICMSTot?.vNF ?? 0);

            // Tipo de operação — enum real do DFe.NET é "saida" (minúsculo)
            var tpNFVal = inf.ide?.tpNF.ToString() ?? string.Empty;
            doc.DadosExtras["Natureza da Operação"]   = inf.ide?.natOp ?? string.Empty;
            doc.DadosExtras["Tipo de Operação"]       = tpNFVal.Contains("aida", StringComparison.OrdinalIgnoreCase) ? "Saída" : "Entrada";
            doc.DadosExtras["Emitente - Endereço"]    = FormatarEndereco(
                inf.emit?.enderEmit?.xLgr, inf.emit?.enderEmit?.nro,
                inf.emit?.enderEmit?.xMun, inf.emit?.enderEmit?.UF.ToString());
            doc.DadosExtras["Destinatário - Endereço"] = FormatarEndereco(
                inf.dest?.enderDest?.xLgr, inf.dest?.enderDest?.nro,
                inf.dest?.enderDest?.xMun, inf.dest?.enderDest?.UF.ToString());
            doc.DadosExtras["Valor dos Produtos"]     = $"R$ {inf.total?.ICMSTot?.vProd:N2}";
            doc.DadosExtras["Valor do ICMS"]          = $"R$ {inf.total?.ICMSTot?.vICMS:N2}";
            doc.DadosExtras["Valor do PIS"]           = $"R$ {inf.total?.ICMSTot?.vPIS:N2}";
            doc.DadosExtras["Valor do COFINS"]        = $"R$ {inf.total?.ICMSTot?.vCOFINS:N2}";
            doc.DadosExtras["Valor do Frete"]         = $"R$ {inf.total?.ICMSTot?.vFrete:N2}";
            doc.DadosExtras["Informações Adicionais"] = inf.infAdic?.infCpl ?? string.Empty;

            // Itens
            if (inf.det != null)
            {
                int num = 1;
                foreach (var det in inf.det)
                {
                    doc.Itens.Add(new ItemNFe
                    {
                        Numero        = num++,
                        Codigo        = det.prod?.cProd ?? string.Empty,
                        Descricao     = det.prod?.xProd ?? string.Empty,
                        Ncm           = det.prod?.NCM ?? string.Empty,
                        Cfop          = det.prod?.CFOP.ToString() ?? string.Empty,
                        Unidade       = det.prod?.uCom ?? string.Empty,
                        Quantidade    = (decimal)(det.prod?.qCom ?? 0),
                        ValorUnitario = (decimal)(det.prod?.vUnCom ?? 0),
                        ValorTotal    = (decimal)(det.prod?.vProd ?? 0)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            doc.Situacao = $"Erro ao ler: {ex.Message}";
        }

        return doc;
    }

    // ── CT-e via XDocument (fallback seguro sem conflito de extension methods) ──
    private static DFeDocument LerCTeViaXml(string caminho, string xml)
    {
        var doc = new DFeDocument
        {
            Tipo = TipoDFe.CTe,
            CaminhoArquivo = caminho,
            XmlConteudo = xml,
            Situacao = "Sem protocolo"
        };

        try
        {
            var xdoc = XDocument.Parse(xml);

            string G(string tag) => xdoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == tag)?.Value ?? string.Empty;

            doc.ChaveAcesso     = G("Id").Replace("CTe", "");
            doc.NumeroDocumento = G("nCT");
            doc.Serie           = G("serie");
            doc.EmitenteCnpj    = G("CNPJ");
            doc.EmitenteNome    = G("xNome");
            doc.ValorTotal      = ParseDecimal(G("vTPrest"));

            if (DateTime.TryParse(G("dhEmi"), out var dt)) doc.DataEmissao = dt;

            // Destinatário: segundo bloco CNPJ/xNome (depois do emitente)
            var cnpjs  = xdoc.Descendants().Where(e => e.Name.LocalName == "CNPJ").ToList();
            var xNomes = xdoc.Descendants().Where(e => e.Name.LocalName == "xNome").ToList();
            if (cnpjs.Count  > 1) doc.DestinatarioCnpjCpf = cnpjs[1].Value;
            if (xNomes.Count > 1) doc.DestinatarioNome     = xNomes[1].Value;

            doc.DadosExtras["Modal"]              = G("modal");
            doc.DadosExtras["CFOP"]               = G("CFOP");
            doc.DadosExtras["Município Início"]   = G("xMunIni");
            doc.DadosExtras["Município Fim"]      = G("xMunFim");
            doc.DadosExtras["UF Início"]          = G("UFIni");
            doc.DadosExtras["UF Fim"]             = G("UFFim");
            doc.DadosExtras["Valor Total"]        = $"R$ {doc.ValorTotal:N2}";
            doc.DadosExtras["Valor Recebedor"]    = $"R$ {ParseDecimal(G("vRec")):N2}";
            doc.DadosExtras["Tipo de Serviço"]    = G("tpServ");
            doc.DadosExtras["Observações"]        = G("xObs");

            var cStat = G("cStat");
            doc.Situacao = cStat == "100" ? "Autorizado" :
                           string.IsNullOrEmpty(cStat) ? "Sem protocolo" : $"cStat: {cStat}";
        }
        catch (Exception ex)
        {
            doc.Situacao = $"Erro ao ler CT-e: {ex.Message}";
        }

        return doc;
    }

    // ── MDF-e via XDocument ──────────────────────────────────────────────────
    private static DFeDocument LerMDFeViaXml(string caminho, string xml)
    {
        var doc = new DFeDocument
        {
            Tipo = TipoDFe.MDFe,
            CaminhoArquivo = caminho,
            XmlConteudo = xml,
            Situacao = "Sem protocolo"
        };

        try
        {
            var xdoc = XDocument.Parse(xml);

            string G(string tag) => xdoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == tag)?.Value ?? string.Empty;

            doc.ChaveAcesso     = G("Id").Replace("MDFe", "");
            doc.NumeroDocumento = G("nMDF");
            doc.Serie           = G("serie");
            doc.EmitenteCnpj    = G("CNPJ");
            doc.EmitenteNome    = G("xNome");
            doc.ValorTotal      = ParseDecimal(G("vCarga"));

            if (DateTime.TryParse(G("dhEmi"), out var dt)) doc.DataEmissao = dt;

            doc.DadosExtras["Modal"]      = G("modal");
            doc.DadosExtras["UF Início"]  = G("UFIni");
            doc.DadosExtras["UF Fim"]     = G("UFFim");
            doc.DadosExtras["Qtd CT-e"]   = G("qCTe");
            doc.DadosExtras["Qtd NF-e"]   = G("qNFe");
            doc.DadosExtras["Placa"]      = G("placa");
            doc.DadosExtras["RENAVAM"]    = G("RENAVAM");
            doc.DadosExtras["Peso Total"] = $"{ParseDecimal(G("qCarga")):N3} kg";

            var cStat = G("cStat");
            doc.Situacao = cStat == "100" ? "Autorizado" :
                           string.IsNullOrEmpty(cStat) ? "Sem protocolo" : $"cStat: {cStat}";
        }
        catch (Exception ex)
        {
            doc.Situacao = $"Erro ao ler MDF-e: {ex.Message}";
        }

        return doc;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string FormatarEndereco(string? logradouro, string? numero, string? municipio, string? uf)
        => string.Join(", ", new[] { logradouro, numero, municipio, uf }
            .Where(p => !string.IsNullOrWhiteSpace(p)));

    private static decimal ParseDecimal(string valor)
        => decimal.TryParse(valor,
               System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
}
