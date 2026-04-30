using DFeViewer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DFeViewer.Services;

/// <summary>
/// Gera PDFs no padrão DACTE (CT-e) e DAMDFE (MDF-e) usando QuestPDF.
/// Layout baseado no Manual de Orientação do Contribuinte SEFAZ v3.00a.
/// Para NF-e e NFC-e a geração é feita pelo OpenFastReport (DANFE oficial).
/// </summary>
public class PdfExportService
{
    // Paleta de cores SEFAZ-inspired (tons neutros, profissional)
    private const string CorPrimaria   = "#1A237E"; // azul escuro cabeçalhos
    private const string CorSecundaria = "#E8EAF6"; // azul clarinho fundo de campos
    private const string CorBorda      = "#9FA8DA"; // azul médio bordas
    private const string CorTextoLabel = "#37474F"; // cinza escuro labels
    private const string CorBranco     = "#FFFFFF";
    private const string CorCinzaClaro = "#F5F5F5";
    private const string CorVerde      = "#1B5E20";
    private const string CorLaranja    = "#E65100";

    public byte[] GerarPdf(DFeDocument doc)
    {
        return doc.Tipo switch
        {
            TipoDFe.CTe  => GerarDacte(doc),
            TipoDFe.MDFe => GerarDamdfe(doc),
            _            => GerarGenerico(doc)
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DACTE — Documento Auxiliar do CT-e
    // Layout: A4 retrato, conforme MOC CT-e v3.00a Anexo II
    // ═══════════════════════════════════════════════════════════════════════
    private byte[] GerarDacte(DFeDocument doc)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                page.Header().Element(c => CabecalhoDacte(c, doc));
                page.Content().Element(c => ConteudoDacte(c, doc));
                page.Footer().Element(c => RodapePadrao(c, doc));
            });
        }).GeneratePdf();
    }

    private static void CabecalhoDacte(IContainer container, DFeDocument doc)
    {
        container.Column(col =>
        {
            // ── Linha 1: Título DACTE centralizado ──────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Row(row =>
            {
                // Quadro emitente (2/3 da largura)
                row.RelativeItem(2).Border(1).BorderColor(CorBorda).Padding(4).Column(c =>
                {
                    c.Item().Text("DACTE").Bold().FontSize(14).AlignCenter().FontColor(CorPrimaria);
                    c.Item().Text("DOCUMENTO AUXILIAR DO CONHECIMENTO DE TRANSPORTE ELETRÔNICO")
                        .Bold().FontSize(7).AlignCenter().FontColor(CorTextoLabel);
                    c.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Column(cc =>
                        {
                            cc.Item().Text(t => { t.Span("EMITENTE: ").Bold().FontColor(CorPrimaria); });
                            cc.Item().Text(doc.EmitenteNome).Bold().FontSize(10);
                            cc.Item().Text($"CNPJ: {FormatCnpj(doc.EmitenteCnpj)}").FontSize(8);
                            cc.Item().Text(doc.DadosExtras.GetValueOrDefault("Emitente - Endereço", "")).FontSize(7);
                        });
                    });
                });

                // Quadro número/série/folha (1/3)
                row.RelativeItem(1).Border(1).BorderColor(CorBorda).Column(c =>
                {
                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("MODELO / SÉRIE / NÚMERO").FontSize(7).Bold().FontColor(CorBranco).AlignCenter();
                    c.Item().Padding(4)
                        .Text($"57 / {doc.Serie} / {doc.NumeroDocumento}").Bold().FontSize(12).AlignCenter();

                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("DATA E HORA DE EMISSÃO").FontSize(7).Bold().FontColor(CorBranco).AlignCenter();
                    c.Item().Padding(4)
                        .Text(doc.DataEmissao?.ToString("dd/MM/yyyy HH:mm") ?? "-").AlignCenter().FontSize(9);

                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("SITUAÇÃO").FontSize(7).Bold().FontColor(CorBranco).AlignCenter();
                    var corSit = doc.Situacao.Contains("Autoriza") ? CorVerde : CorLaranja;
                    c.Item().Padding(4).Text(doc.Situacao).Bold().FontColor(corSit).AlignCenter().FontSize(9);
                });
            });

            // ── Linha 2: Chave de acesso ─────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Padding(3).Row(row =>
            {
                row.ConstantItem(120).Text("CHAVE DE ACESSO").Bold().FontSize(7).FontColor(CorPrimaria);
                row.RelativeItem().Text(FormatChave(doc.ChaveAcesso)).FontFamily("Courier New").FontSize(8);
            });
        });
    }

    private static void ConteudoDacte(IContainer container, DFeDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(2);

            // ── Remetente / Expedidor ────────────────────────────────────────
            col.Item().Element(c => QuadroDuplo(c,
                "REMETENTE",
                doc.EmitenteNome, FormatCnpj(doc.EmitenteCnpj),
                doc.DadosExtras.GetValueOrDefault("Emitente - Endereço", ""),
                "DESTINATÁRIO / RECEBEDOR",
                doc.DestinatarioNome, FormatCnpj(doc.DestinatarioCnpjCpf),
                doc.DadosExtras.GetValueOrDefault("Destinatário - Endereço", "")));

            // ── Dados do serviço ─────────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(3)
                    .Text("DADOS DO SERVIÇO DE TRANSPORTE").Bold().FontSize(8).FontColor(CorBranco);

                c.Item().Padding(4).Row(row =>
                {
                    row.Spacing(4);
                    CampoRow(row, 1, "CFOP", doc.DadosExtras.GetValueOrDefault("CFOP", "-"));
                    CampoRow(row, 1, "MODAL", doc.DadosExtras.GetValueOrDefault("Modal", "-"));
                    CampoRow(row, 1, "TIPO DE SERVIÇO", doc.DadosExtras.GetValueOrDefault("Tipo de Serviço", "-"));
                    CampoRow(row, 1, "NATUREZA DA PRESTAÇÃO", doc.DadosExtras.GetValueOrDefault("Natureza da Operação", "-"));
                });
                c.Item().Padding(4).Row(row =>
                {
                    row.Spacing(4);
                    CampoRow(row, 3, "MUN. INÍCIO", doc.DadosExtras.GetValueOrDefault("Município Início", "-"));
                    CampoRow(row, 1, "UF", doc.DadosExtras.GetValueOrDefault("UF Início", "-"));
                    CampoRow(row, 3, "MUN. FIM", doc.DadosExtras.GetValueOrDefault("Município Fim", "-"));
                    CampoRow(row, 1, "UF", doc.DadosExtras.GetValueOrDefault("UF Fim", "-"));
                    CampoRow(row, 4, "TOMADOR DO SERVIÇO", doc.DadosExtras.GetValueOrDefault("Tomador", "-"));
                });
            });

            // ── Valores da prestação ─────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(3)
                    .Text("VALORES DA PRESTAÇÃO DO SERVIÇO").Bold().FontSize(8).FontColor(CorBranco);

                c.Item().Padding(4).Row(row =>
                {
                    row.Spacing(4);
                    CampoRow(row, 1, "VALOR TOTAL DO SERVIÇO", $"R$ {doc.ValorTotal:N2}", negrito: true, corValor: CorPrimaria);
                    CampoRow(row, 1, "VALOR A RECEBER", doc.DadosExtras.GetValueOrDefault("Valor Recebedor", "-"));
                    CampoRow(row, 1, "FORMA DE PAGAMENTO", doc.DadosExtras.GetValueOrDefault("Forma Pagamento", "-"));
                });
            });

            // ── Documentos originários ───────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(3)
                    .Text("DOCUMENTOS ORIGINÁRIOS").Bold().FontSize(8).FontColor(CorBranco);
                c.Item().Padding(6)
                    .Text(doc.DadosExtras.GetValueOrDefault("Documentos Originários",
                        "Consulte o XML para verificar os documentos fiscais vinculados a este CT-e."))
                    .FontSize(8).FontColor(CorTextoLabel);
            });

            // ── Observações / Informações adicionais ─────────────────────────
            var obs = doc.DadosExtras.GetValueOrDefault("Observações", "")
                    + " " + doc.DadosExtras.GetValueOrDefault("Informações Adicionais", "");
            if (!string.IsNullOrWhiteSpace(obs.Trim()))
            {
                col.Item().Border(1).BorderColor(CorBorda).Column(c =>
                {
                    c.Item().Background(CorSecundaria).Padding(3)
                        .Text("OBSERVAÇÕES / INFORMAÇÕES ADICIONAIS").Bold().FontSize(7).FontColor(CorPrimaria);
                    c.Item().Padding(6).Text(obs.Trim()).FontSize(8);
                });
            }

            // ── Protocolo ───────────────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Background(CorSecundaria).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PROTOCOLO DE AUTORIZAÇÃO DE USO").Bold().FontSize(7).FontColor(CorPrimaria);
                    c.Item().Text(doc.Situacao.Contains("Autoriza")
                        ? $"CT-e AUTORIZADO — {doc.DataEmissao:dd/MM/yyyy HH:mm}"
                        : doc.Situacao).FontSize(9).Bold();
                });
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DAMDFE — Documento Auxiliar do MDF-e
    // Layout: A4 retrato, conforme MOC MDF-e
    // ═══════════════════════════════════════════════════════════════════════
    private byte[] GerarDamdfe(DFeDocument doc)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                page.Header().Element(c => CabecalhoDamdfe(c, doc));
                page.Content().Element(c => ConteudoDamdfe(c, doc));
                page.Footer().Element(c => RodapePadrao(c, doc));
            });
        }).GeneratePdf();
    }

    private static void CabecalhoDamdfe(IContainer container, DFeDocument doc)
    {
        container.Column(col =>
        {
            col.Item().Border(1).BorderColor(CorBorda).Row(row =>
            {
                // Emitente
                row.RelativeItem(2).Border(1).BorderColor(CorBorda).Padding(5).Column(c =>
                {
                    c.Item().Text("DAMDFE").Bold().FontSize(14).AlignCenter().FontColor(CorPrimaria);
                    c.Item().Text("DOCUMENTO AUXILIAR DO MANIFESTO ELETRÔNICO DE DOCUMENTOS FISCAIS")
                        .Bold().FontSize(7).AlignCenter().FontColor(CorTextoLabel);
                    c.Item().PaddingTop(4).Column(cc =>
                    {
                        cc.Item().Text(doc.EmitenteNome).Bold().FontSize(11);
                        cc.Item().Text($"CNPJ: {FormatCnpj(doc.EmitenteCnpj)}");
                        cc.Item().Text(doc.DadosExtras.GetValueOrDefault("Emitente - Endereço", "")).FontSize(7);
                    });
                });

                // Número/série/modal
                row.RelativeItem(1).Border(1).BorderColor(CorBorda).Column(c =>
                {
                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("MODELO / SÉRIE / NÚMERO").FontSize(7).Bold().FontColor(CorBranco).AlignCenter();
                    c.Item().Padding(4)
                        .Text($"58 / {doc.Serie} / {doc.NumeroDocumento}").Bold().FontSize(11).AlignCenter();

                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("DATA E HORA DE EMISSÃO").FontSize(7).Bold().FontColor(CorBranco).AlignCenter();
                    c.Item().Padding(4)
                        .Text(doc.DataEmissao?.ToString("dd/MM/yyyy HH:mm") ?? "-").AlignCenter();

                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("MODAL").FontSize(7).Bold().FontColor(CorBranco).AlignCenter();
                    c.Item().Padding(4)
                        .Text(doc.DadosExtras.GetValueOrDefault("Modal", "-")).AlignCenter().Bold().FontSize(10);

                    var corSit = doc.Situacao.Contains("Autoriza") ? CorVerde : CorLaranja;
                    c.Item().Background(corSit).Padding(3)
                        .Text(doc.Situacao).FontSize(8).Bold().FontColor(CorBranco).AlignCenter();
                });
            });

            // Chave de acesso
            col.Item().Border(1).BorderColor(CorBorda).Padding(3).Row(row =>
            {
                row.ConstantItem(120).Text("CHAVE DE ACESSO").Bold().FontSize(7).FontColor(CorPrimaria);
                row.RelativeItem().Text(FormatChave(doc.ChaveAcesso)).FontFamily("Courier New").FontSize(8);
            });
        });
    }

    private static void ConteudoDamdfe(IContainer container, DFeDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(2);

            // ── UF Início / UF Fim / Percurso ────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(3)
                    .Text("PERCURSO").Bold().FontSize(8).FontColor(CorBranco);
                c.Item().Padding(4).Row(row =>
                {
                    row.Spacing(4);
                    CampoRow(row, 1, "UF INÍCIO", doc.DadosExtras.GetValueOrDefault("UF Início", "-"), negrito: true);
                    CampoRow(row, 1, "UF FIM", doc.DadosExtras.GetValueOrDefault("UF Fim", "-"), negrito: true);
                    CampoRow(row, 4, "PERCURSO (UFs)", doc.DadosExtras.GetValueOrDefault("Percurso", "-"));
                });
            });

            // ── Veículo de tração ─────────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(3)
                    .Text("VEÍCULO DE TRAÇÃO").Bold().FontSize(8).FontColor(CorBranco);
                c.Item().Padding(4).Row(row =>
                {
                    row.Spacing(4);
                    CampoRow(row, 1, "PLACA", doc.DadosExtras.GetValueOrDefault("Placa", "-"), negrito: true);
                    CampoRow(row, 1, "RENAVAM", doc.DadosExtras.GetValueOrDefault("RENAVAM", "-"));
                    CampoRow(row, 1, "UF DA PLACA", doc.DadosExtras.GetValueOrDefault("UF Placa", "-"));
                    CampoRow(row, 1, "TARA (KG)", doc.DadosExtras.GetValueOrDefault("Tara", "-"));
                });
            });

            // ── Totalizadores ────────────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(3)
                    .Text("TOTAIS").Bold().FontSize(8).FontColor(CorBranco);
                c.Item().Padding(4).Row(row =>
                {
                    row.Spacing(4);
                    CampoRow(row, 1, "QTD CT-e", doc.DadosExtras.GetValueOrDefault("Qtd CT-e", "0"), negrito: true);
                    CampoRow(row, 1, "QTD NF-e", doc.DadosExtras.GetValueOrDefault("Qtd NF-e", "0"), negrito: true);
                    CampoRow(row, 1, "PESO TOTAL", doc.DadosExtras.GetValueOrDefault("Peso Total", "-"), negrito: true);
                    CampoRow(row, 1, "VALOR TOTAL DA CARGA", $"R$ {doc.ValorTotal:N2}", negrito: true, corValor: CorPrimaria);
                });
            });

            // ── Conductores ──────────────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorSecundaria).Padding(3)
                    .Text("CONDUTOR(ES)").Bold().FontSize(7).FontColor(CorPrimaria);
                c.Item().Padding(6).Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3);
                        cd.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Background(CorPrimaria).Padding(3)
                            .Text("NOME").FontSize(7).Bold().FontColor(CorBranco);
                        h.Cell().Background(CorPrimaria).Padding(3)
                            .Text("CPF").FontSize(7).Bold().FontColor(CorBranco);
                    });
                    var nome = doc.DadosExtras.GetValueOrDefault("Condutor Nome", "-");
                    var cpf  = doc.DadosExtras.GetValueOrDefault("Condutor CPF", "-");
                    table.Cell().Padding(3).Text(nome).FontSize(8);
                    table.Cell().Padding(3).Text(cpf).FontSize(8);
                });
            });

            // ── Lista de documentos fiscais ──────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorSecundaria).Padding(3)
                    .Text("DOCUMENTOS FISCAIS VINCULADOS").Bold().FontSize(7).FontColor(CorPrimaria);
                c.Item().Padding(6)
                    .Text(doc.DadosExtras.GetValueOrDefault("Documentos Fiscais",
                        "Consulte o XML para a lista de documentos fiscais vinculados a este MDF-e."))
                    .FontSize(8).FontColor(CorTextoLabel);
            });

            // ── Protocolo ───────────────────────────────────────────────────
            col.Item().Border(1).BorderColor(CorBorda).Background(CorSecundaria).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PROTOCOLO DE AUTORIZAÇÃO DE USO").Bold().FontSize(7).FontColor(CorPrimaria);
                    c.Item().Text(doc.Situacao.Contains("Autoriza")
                        ? $"MDF-e AUTORIZADO — {doc.DataEmissao:dd/MM/yyyy HH:mm}"
                        : doc.Situacao).FontSize(9).Bold();
                });
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Genérico (NF-e/NFC-e sem OpenFastReport — fallback)
    // ═══════════════════════════════════════════════════════════════════════
    private byte[] GerarGenerico(DFeDocument doc)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));
                page.Header().Element(c => CabecalhoGenerico(c, doc));
                page.Content().Element(c => ConteudoGenerico(c, doc));
                page.Footer().Element(c => RodapePadrao(c, doc));
            });
        }).GeneratePdf();
    }

    private static void CabecalhoGenerico(IContainer container, DFeDocument doc)
    {
        container.Column(col =>
        {
            col.Item().Background(CorPrimaria).Padding(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"DANFE — {doc.TipoLabel}").Bold().FontSize(14).FontColor(CorBranco);
                    c.Item().Text(doc.EmitenteNome).FontSize(10).FontColor(CorBranco);
                    c.Item().Text($"CNPJ: {FormatCnpj(doc.EmitenteCnpj)}").FontSize(8).FontColor("#B3E5FC");
                });
                row.ConstantItem(130).Column(c =>
                {
                    c.Item().Text($"Nº {doc.NumeroDocumento} — Série {doc.Serie}").Bold().FontSize(10).FontColor(CorBranco).AlignRight();
                    c.Item().Text(doc.DataEmissao?.ToString("dd/MM/yyyy HH:mm") ?? "-").FontSize(8).FontColor("#B3E5FC").AlignRight();
                    var corSit = doc.Situacao.Contains("Autoriza") ? "#A5D6A7" : "#FFCC80";
                    c.Item().Text(doc.Situacao).Bold().FontSize(9).FontColor(corSit).AlignRight();
                    c.Item().Text($"R$ {doc.ValorTotal:N2}").Bold().FontSize(14).FontColor(CorBranco).AlignRight();
                });
            });
            col.Item().Background(CorSecundaria).Padding(3).Row(row =>
            {
                row.ConstantItem(100).Text("CHAVE DE ACESSO:").Bold().FontSize(7).FontColor(CorPrimaria);
                row.RelativeItem().Text(FormatChave(doc.ChaveAcesso)).FontFamily("Courier New").FontSize(7);
            });
        });
    }

    private static void ConteudoGenerico(IContainer container, DFeDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(3);

            // Emitente / Destinatário
            col.Item().Element(c => QuadroDuplo(c,
                "EMITENTE", doc.EmitenteNome, FormatCnpj(doc.EmitenteCnpj),
                doc.DadosExtras.GetValueOrDefault("Emitente - Endereço", ""),
                "DESTINATÁRIO", doc.DestinatarioNome, FormatCnpj(doc.DestinatarioCnpjCpf),
                doc.DadosExtras.GetValueOrDefault("Destinatário - Endereço", "")));

            // Dados extras
            if (doc.DadosExtras.Any())
            {
                col.Item().Border(1).BorderColor(CorBorda).Column(c =>
                {
                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("DADOS DO DOCUMENTO").Bold().FontSize(7).FontColor(CorBranco);
                    c.Item().Padding(4).Column(col2 =>
                    {
                        var campos = doc.DadosExtras
                            .Where(k => !string.IsNullOrWhiteSpace(k.Value)
                                     && k.Key != "Emitente - Endereço"
                                     && k.Key != "Destinatário - Endereço"
                                     && k.Key != "Informações Adicionais")
                            .ToList();
                        for (int i = 0; i < campos.Count; i += 3)
                        {
                            var grupo = campos.Skip(i).Take(3).ToList();
                            col2.Item().Row(row =>
                            {
                                row.Spacing(4);
                                foreach (var kvp in grupo)
                                    CampoRow(row, 1, kvp.Key, kvp.Value);
                                for (int p = grupo.Count; p < 3; p++)
                                    row.RelativeItem();
                            });
                        }
                    });
                });
            }

            // Itens
            if (doc.Itens.Any())
            {
                col.Item().Border(1).BorderColor(CorBorda).Column(c =>
                {
                    c.Item().Background(CorPrimaria).Padding(3)
                        .Text("PRODUTOS / SERVIÇOS").Bold().FontSize(7).FontColor(CorBranco);
                    c.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(22);
                            cd.ConstantColumn(55);
                            cd.RelativeColumn(4);
                            cd.ConstantColumn(48);
                            cd.ConstantColumn(40);
                            cd.ConstantColumn(30);
                            cd.ConstantColumn(50);
                            cd.ConstantColumn(58);
                            cd.ConstantColumn(58);
                        });
                        string[] heads = { "Nº", "Código", "Descrição", "NCM", "CFOP", "UN", "Qtd", "V. Unit.", "V. Total" };
                        foreach (var h in heads)
                            table.Cell().Background(CorPrimaria).Padding(3)
                                .Text(h).FontSize(7).Bold().FontColor(CorBranco).AlignCenter();

                        bool alt = false;
                        foreach (var item in doc.Itens)
                        {
                            var bg = alt ? CorCinzaClaro : CorBranco; alt = !alt;
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Numero.ToString()).AlignCenter();
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Codigo);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Descricao);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Ncm);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Cfop);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Unidade);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text(item.Quantidade.ToString("N3")).AlignRight();
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text($"R$ {item.ValorUnitario:N2}").AlignRight();
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E0E0E0").Padding(2).Text($"R$ {item.ValorTotal:N2}").AlignRight();
                        }
                        var tot = doc.Itens.Sum(i => i.ValorTotal);
                        table.Cell().ColumnSpan(8).Background(CorSecundaria).Padding(3)
                            .Text("TOTAL DOS PRODUTOS").Bold().FontSize(7).AlignRight().FontColor(CorPrimaria);
                        table.Cell().Background(CorSecundaria).Padding(3)
                            .Text($"R$ {tot:N2}").Bold().AlignRight().FontColor(CorPrimaria);
                    });
                });
            }

            // Inf adicionais
            var inf = doc.DadosExtras.GetValueOrDefault("Informações Adicionais", "");
            if (!string.IsNullOrWhiteSpace(inf))
            {
                col.Item().Border(1).BorderColor(CorBorda).Column(c =>
                {
                    c.Item().Background(CorSecundaria).Padding(3)
                        .Text("INFORMAÇÕES ADICIONAIS").Bold().FontSize(7).FontColor(CorPrimaria);
                    c.Item().Padding(5).Text(inf).FontSize(8);
                });
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers compartilhados
    // ═══════════════════════════════════════════════════════════════════════
    private static void RodapePadrao(IContainer container, DFeDocument doc)
    {
        container.BorderTop(1).BorderColor(CorBorda).PaddingTop(3).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("Gerado por DFeViewer  ·  ").FontSize(7).FontColor("#9E9E9E");
                t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(7).FontColor("#9E9E9E");
            });
            row.ConstantItem(80).AlignRight().Text(t =>
            {
                t.Span("Pág. ").FontSize(7).FontColor("#9E9E9E");
                t.CurrentPageNumber().FontSize(7).FontColor("#9E9E9E");
                t.Span("/").FontSize(7).FontColor("#9E9E9E");
                t.TotalPages().FontSize(7).FontColor("#9E9E9E");
            });
        });
    }

    private static void QuadroDuplo(IContainer container,
        string tit1, string nome1, string cnpj1, string end1,
        string tit2, string nome2, string cnpj2, string end2)
    {
        container.Border(1).BorderColor(CorBorda).Row(row =>
        {
            row.RelativeItem().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(2).Text(tit1).Bold().FontSize(7).FontColor(CorBranco);
                c.Item().Padding(4).Column(cc =>
                {
                    cc.Item().Text(nome1).Bold().FontSize(9);
                    cc.Item().Text($"CNPJ/CPF: {cnpj1}").FontSize(8);
                    cc.Item().Text(end1).FontSize(7).FontColor(CorTextoLabel);
                });
            });
            row.RelativeItem().Border(1).BorderColor(CorBorda).Column(c =>
            {
                c.Item().Background(CorPrimaria).Padding(2).Text(tit2).Bold().FontSize(7).FontColor(CorBranco);
                c.Item().Padding(4).Column(cc =>
                {
                    cc.Item().Text(nome2).Bold().FontSize(9);
                    cc.Item().Text($"CNPJ/CPF: {cnpj2}").FontSize(8);
                    cc.Item().Text(end2).FontSize(7).FontColor(CorTextoLabel);
                });
            });
        });
    }

    private static void CampoRow(RowDescriptor row, float peso, string k, string v,
        bool negrito = false, string? corValor = null)
    {
        row.RelativeItem(peso).Column(c =>
        {
            c.Item().Text(k).FontSize(6).FontColor(CorTextoLabel).Bold();
            var t = c.Item().Background(CorSecundaria).Padding(2).Text(v).FontSize(8);
            if (negrito) t.Bold();
            if (corValor != null) t.FontColor(corValor);
        });
    }

    private static string FormatChave(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave)) return string.Empty;
        var s = chave.Replace(" ", "");
        return string.Join(" ", Enumerable.Range(0, s.Length / 4).Select(i => s.Substring(i * 4, 4)));
    }

    private static string FormatCnpj(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
        var s = System.Text.RegularExpressions.Regex.Replace(valor, @"\D", "");
        return s.Length == 14
            ? $"{s[..2]}.{s[2..5]}.{s[5..8]}/{s[8..12]}-{s[12..]}"
            : s.Length == 11
                ? $"{s[..3]}.{s[3..6]}.{s[6..9]}-{s[9..]}"
                : valor;
    }
}
