# DFeViewer — Leitor de Documentos Fiscais Eletrônicos

[![License: CC BY-NC-SA 4.0](https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc-sa/4.0/)

Aplicação desktop **WPF (.NET 8)** para visualização e impressão de documentos fiscais eletrônicos (DFe) em XML, usando a biblioteca [DFe.NET (Zeus)](https://github.com/ZeusAutomacao/DFe.NET).

---

## Funcionalidades

| Recurso | Detalhes |
|---|---|
| ✅ Tipos suportados | **NF-e, NFC-e, CT-e, MDF-e** |
| ✅ Abertura múltipla | Abra vários XMLs ao mesmo tempo |
| ✅ Drag & Drop | Arraste XMLs direto para a janela |
| ✅ Abertura via CLI | `DFeViewer.exe nota.xml` abre o arquivo direto |
| ✅ Visualização | Emitente, destinatário, itens, totais, dados fiscais |
| ✅ Exportar PDF | Gera DANFE em PDF via **QuestPDF** (gratuito) |
| ✅ Imprimir | Abre PDF no leitor padrão → botão Imprimir nativo |
| ✅ Copiar chave | Botão para copiar chave de acesso para a área de transferência |
| ✅ Filtro em tempo real | Busca por emitente, número, chave ou tipo na lista |
| ✅ Histórico de recentes | Arquivos abertos recentemente persistidos entre sessões |
| ✅ Tema claro/escuro | Toggle na toolbar, preferência salva em AppData |
| ✅ Detecção de duplicatas | Documentos com mesma chave de acesso não são carregados duas vezes |
| ✅ Interface moderna | Material Design 3 |

---

## Pré-requisitos

- **Windows 10/11** (64-bit)
- **.NET 8 SDK** → https://dotnet.microsoft.com/download/dotnet/8.0
- **Visual Studio 2022** ou **VS Code** com extensão C#

---

## Como rodar

```bash
# 1. Clonar / criar o projeto
# (copie todos os arquivos deste projeto para uma pasta)

# 2. Restaurar pacotes
dotnet restore

# 3. Rodar em modo desenvolvimento
dotnet run

# 4. Abrir um XML diretamente via linha de comando
dotnet run -- caminho/para/nota.xml

# 5. Publicar executável
dotnet publish -c Release -r win-x64 --self-contained true -o ./dist
```

---

## Estrutura do Projeto

```
DFeViewer/
├── DFeViewer.csproj              ← Dependências NuGet
├── App.xaml / App.xaml.cs        ← Inicialização, recursos globais, args CLI
├── Models/
│   └── DFeDocument.cs            ← Modelo genérico de DFe + ItemNFe
├── Services/
│   ├── DFeReaderService.cs       ← Leitura XML via DFe.NET (NFe/CTe/MDFe)
│   ├── PdfExportService.cs       ← Geração de PDF via QuestPDF
│   ├── HistoricoService.cs       ← Persistência de arquivos recentes (AppData)
│   └── TemaService.cs            ← Toggle tema claro/escuro (AppData)
├── ViewModels/
│   └── MainViewModel.cs          ← Lógica de UI (MVVM) + comandos readonly + async
├── Views/
│   ├── MainWindow.xaml           ← Interface WPF com Material Design
│   └── MainWindow.xaml.cs        ← Drag & Drop + limpeza de temporários
└── Converters/
    └── ValueConverters.cs        ← Conversores de binding para a UI
```

---

## Testes

O projeto de testes está em `DFeViewer.Tests/` e usa **xUnit + FluentAssertions**.

### Executar

```bash
dotnet test DFeViewer.Tests/DFeViewer.Tests.csproj
```

### Resultado atual

```
Aprovado! – Com falha: 0, Aprovado: 81, Ignorado: 0, Total: 81
```

### Suites

| Suite | Casos | Cobertura |
|---|---|---|
| `DFeReaderServiceTests` | 23 | Leitura de NF-e, CT-e, MDF-e; detecção de tipo por namespace XML; XML inválido/inexistente; campos obrigatórios; itens |
| `DFeDocumentTests` | 10 | Modelo `DFeDocument`; `TipoLabel`; `NomeArquivo`; valores padrão de `ItemNFe` |
| `HistoricoServiceTests` | 10 | Persistência em disco; deduplicação; ordem LIFO; limite de 20 itens; remoção de inexistentes |
| `PdfExportServiceTests` | 10 | Geração de PDF para todos os tipos (NF-e, CT-e, MDF-e, Desconhecido); header `%PDF`; documentos sem itens ou dados extras |
| `ConvertersTests` | 28 | Todos os converters WPF: `ZeroToVisible`, `NonZeroToVisible`, `BoolToVisible`, `BoolToInvisible`, `TipoToColor`, `SituacaoToColor`, `SomaTotal`, `NomeArquivo` |

### Fixtures de teste

Os XMLs de referência usados nos testes estão em `DFeViewer.Tests/Fixtures/`:

| Arquivo | Descrição |
|---|---|
| `nfe_sem_protocolo.xml` | NF-e modelo 55 sem protocolo de autorização, 2 itens, valores conhecidos |
| `cte_exemplo.xml` | CT-e modelo 57 com protocolo `cStat=100`, rota SP→RJ |
| `mdfe_exemplo.xml` | MDF-e modelo 58 com protocolo `cStat=100`, veículo e condutor |

---

## Pacotes NuGet utilizados

| Pacote | Versão | Finalidade |
|---|---|---|
| `Zeus.Net.NFe.NFCe` | 2026.x | Leitura de NF-e e NFC-e |
| `Zeus.Net.CTe` | 2026.x | Leitura de CT-e |
| `Zeus.Net.MDFe` | 2026.x | Leitura de MDF-e |
| `Zeus.Net.NFe.Danfe.Html` | 2026.x | DANFE em HTML (opcional) |
| `QuestPDF` | 2025.x | Geração de PDF (licença Community gratuita) |
| `MaterialDesignThemes` | 5.x | Interface Material Design 3 |
| `xunit` | 2.x | Framework de testes |
| `FluentAssertions` | 8.x | Asserções legíveis nos testes |

> **QuestPDF Community** é gratuito para projetos com receita anual < USD 1 milhão.
> A licença é configurada automaticamente em `App.xaml.cs`.

---

## Como o DFe.NET é usado

```csharp
// NF-e processada (com protocolo de autorização)
var proc = new nfeProc().CarregarDeArquivoXml(caminhoArquivo);
var nfe  = proc.NFe;

// Dados do emitente
string nomeEmitente = nfe.infNFe.emit.xNome;
string cnpj         = nfe.infNFe.emit.CNPJ;

// Itens
foreach (var det in nfe.infNFe.det)
{
    Console.WriteLine($"{det.prod.xProd} — R$ {det.prod.vProd:N2}");
}

// CT-e
var cteProc = new cteProc().CarregarDeArquivoXml(caminhoArquivo);

// MDF-e  
var mdfeProc = new mdfeProc().CarregarDeArquivoXml(caminhoArquivo);
```

---

## Impressão / PDF

O fluxo de impressão é:
1. `PdfExportService.GerarPdf(doc)` → gera `byte[]` usando QuestPDF (assíncrono, não trava a UI)
2. Salva em arquivo temporário em `%TEMP%`
3. Abre o PDF no **leitor padrão do Windows** (que tem botão Imprimir nativo)
4. Arquivos temporários são **deletados automaticamente** ao fechar a aplicação

Isso evita dependências de impressoras e drivers — funciona com qualquer impressora instalada no Windows.

---

## Extensões futuras possíveis

- Preview do DANFE original (via `NFe.Danfe.Html` + WebView2)
- Validação de assinatura digital (já suportada pelo DFe.NET)
- Consulta de situação na SEFAZ (webservices do DFe.NET)
- Cancelamento / carta de correção
- Filtros por período e valor
- Exportação em lote (múltiplos PDFs)

---

## Licença

Este projeto está licenciado sob a [Creative Commons BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/deed.pt_BR).

**Você pode:**
- Usar, copiar e distribuir livremente
- Modificar e criar versões derivadas

**Você não pode:**
- Vender o software ou incorporá-lo em produtos/serviços pagos sem autorização do autor

**Condição:** distribuições e derivações devem manter esta mesma licença e dar crédito ao autor original.

Copyright © 2026 Ednelson Chado
