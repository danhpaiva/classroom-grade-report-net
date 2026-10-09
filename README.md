<div align="center">

# 📊 ClassroomGradeReport

**Relatório de notas do Google Classroom direto no terminal — somente leitura, rápido e pronto para o Excel.**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)
![Google Classroom API](https://img.shields.io/badge/Google%20Classroom-API%20v1-34A853?logo=googleclassroom&logoColor=white)
![OAuth 2.0](https://img.shields.io/badge/OAuth-2.0%20Desktop-4285F4?logo=google&logoColor=white)
![Somente leitura](https://img.shields.io/badge/acesso-somente%20leitura-success)
![Spectre.Console](https://img.shields.io/badge/UI-Spectre.Console-blueviolet)
![Testes](https://img.shields.io/badge/testes-xUnit-5E2750)
![Plataforma](https://img.shields.io/badge/plataforma-Windows-0078D6?logo=windows&logoColor=white)
[![Licença: MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-yellow.svg)](LICENSE)

[Exemplo](#-exemplo-de-saída) •
[Configuração](#-configuração-no-google-cloud) •
[Como rodar](#-como-rodar) •
[Problemas?](#-solução-de-problemas) •
[Testes](#-testes)

</div>

---

## ✨ Recursos

- 🎓 Lista suas turmas (professor) e deixa escolher em um menu — ou use `--course` / `--all-courses`.
- 🧮 Uma linha por aluno, uma coluna por atividade, com **total**, **total possível** e **percentual**.
- 🚫 *Sem nota não é zero*: aparece como `-` (ou use `--missing-as-zero`).
- 📝 Notas devolvidas (`assignedGrade`) por padrão; rascunhos opcionais com `--include-drafts`.
- 📁 Exporta **CSV** pronto para o Excel pt-BR (`;`, vírgula decimal, UTF-8 com BOM), **Excel (`.xlsx`)** com números reais e formatação, **texto (`.txt`)** alinhado e/ou **página HTML** autocontida, inclusive um arquivo por turma.
- 🔒 **Somente leitura**: nenhum escopo de escrita; nada sai da máquina além das chamadas ao Google.
- 🔁 Paginação completa, retry com backoff para 429/5xx e mensagens de erro claras em português.
- 🧩 Cálculo puro e testável, com ponto de extensão para pesos/categorias (`IGradingPolicy`).

## 📋 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` deve mostrar 10.x).
- Windows (os caminhos padrão usam `%APPDATA%`; em outros sistemas informe `--credentials` e adapte o local do token).
- Uma conta Google com turmas no Classroom em que você é **professor(a)**.
- Um projeto no Google Cloud com a credencial OAuth (veja a seção "Configuração no Google Cloud").

Para obter o código e compilar:

```bash
git clone https://github.com/danhpaiva/classroom-grade-report-net.git
cd classroom-grade-report-net
dotnet build
```

## 🖥️ Exemplo de saída

```
Turma: Matemática 9A
Critério de nota: somente assignedGrade (nota devolvida); sem nota ('-') não é somada
╭───────────────┬───────┬──────────┬───────┬──────────┬───────╮
│ Aluno         │ Prova │ Trabalho │ Total │ Possível │     % │
│               │ (/10) │     (/5) │       │          │       │
├───────────────┼───────┼──────────┼───────┼──────────┼───────┤
│ Ana Souza     │   7,5 │        - │   7,5 │       10 │ 75,0% │
│ Bruno Lima    │     9 │        4 │    13 │       15 │ 86,7% │
╰───────────────┴───────┴──────────┴───────┴──────────┴───────╯
2 aluno(s), 2 atividade(s).
```

## 📐 Regras de cálculo

- **Nota considerada:** por padrão, `assignedGrade` (nota já devolvida ao aluno). Com `--include-drafts`, usa `draftGrade` quando `assignedGrade` estiver ausente. O critério usado aparece no cabeçalho do relatório.
- **Sem nota não é zero:** aparece como `-` e não entra na soma. O **total possível** de cada aluno soma o `maxPoints` apenas das atividades *com nota* para ele. Com `--missing-as-zero`, atividade sem nota vale 0 e o `maxPoints` entra no total possível.
- Atividades sem `maxPoints` (ou 0) são ignoradas com um aviso; atividades em `DRAFT`/`DELETED` são ignoradas.
- Submissões de alunos que não estão mais na turma são ignoradas.
- Alunos em ordem alfabética (sem diferenciar acentos/maiúsculas).
- Só entram atividades **publicadas** (é o que a API lista por padrão) e turmas em que você é **professor(a)** (`teacherId=me`); por padrão, apenas turmas ativas.
- Percentual = `total / total possível × 100`. Se não houver nenhuma nota para o aluno, o percentual aparece como `-`.
- A API **não expõe a nota geral** calculada pelo Classroom; a soma é feita por esta aplicação. Pesos/categorias podem ser adicionados implementando `IGradingPolicy` (`Reporting/IGradingPolicy.cs`).

## 🔐 Por que preciso de credenciais?

A Google Classroom API **não é pública**: as turmas, alunos e notas pertencem à sua conta, e o Google só entrega esses dados a um aplicativo **identificado** e **autorizado por você**. Isso exige duas coisas diferentes:

| O quê | Para quê | Onde fica |
|---|---|---|
| **`credentials.json`** (cliente OAuth Desktop) | **Identifica o aplicativo** perante o Google (qual projeto, quais escopos, quota). Sozinho **não dá acesso a nenhum dado**. | Você baixa do Google Cloud e salva em `%APPDATA%\ClassroomGradeReport\credentials.json` (ou passa `--credentials <caminho>`) |
| **Token** (gerado na autorização) | Prova que **você** permitiu que o app leia suas turmas (somente leitura). É renovado automaticamente. | Criado na primeira execução em `%APPDATA%\ClassroomGradeReport\token\` |

Por isso o app pede o `credentials.json` toda vez que precisa autenticar, e por isso o `--credentials` existe: para você guardar o arquivo onde preferir. Não há chave de API nem senha envolvidas, e o `credentials.json` **não deve ser compartilhado nem commitado** (ele contém o segredo do cliente do seu projeto).

## ☁️ Configuração no Google Cloud

1. Acesse o [Google Cloud Console](https://console.cloud.google.com/) e crie um **projeto**.
2. Em **APIs e serviços > Biblioteca**, ative a **Google Classroom API**.
3. Em **APIs e serviços > Tela de permissão OAuth** (Google Auth Platform), configure o app:
   - Tipo de usuário: **Externo** (ou **Interno**, se sua conta for Google Workspace).
   - Adicione os escopos (somente leitura):
     - `https://www.googleapis.com/auth/classroom.courses.readonly`
     - `https://www.googleapis.com/auth/classroom.rosters.readonly`
     - `https://www.googleapis.com/auth/classroom.coursework.students.readonly`
   - Em modo *Testing*, adicione seu e-mail como **usuário de teste**.
4. Em **Credenciais > Criar credenciais**, escolha a API **Google Classroom API** e o tipo de dado **Dados do usuário** (isso cria um *cliente OAuth*; "Dados do aplicativo" criaria uma conta de serviço, que não enxerga suas turmas). No tipo de aplicativo, escolha **App para computador (Desktop app)** — o tipo "Aplicativo da Web" não funciona.
5. Baixe o JSON e salve como `%APPDATA%\ClassroomGradeReport\credentials.json` (ou aponte outro caminho com `--credentials` ou a variável `CLASSROOM_CREDENTIALS_PATH`).

> **Não precisa de chave de API** (API key) nem de conta de serviço: os dados do Classroom pertencem à conta do usuário e exigem OAuth. Ignore avisos de permissão sobre "Chaves de API". A conta dona do projeto não precisa ser a mesma do Classroom, mas a conta que autoriza no navegador é a que tem as turmas e, em modo *Testing*, precisa estar em **Usuários de teste**.

> **Atenção — modo "Testing":** com o app em *Testing* e tipo de usuário Externo, o **refresh token expira em 7 dias** e será preciso autorizar de novo. Para contornar: publique o app como **"In production"** (uso pessoal; o Google mostrará um aviso de "app não verificado", que você pode aceitar pois o app é seu) ou, se a conta for Workspace, use o tipo **"Internal"**, sem expiração.

## 🔑 Autorização (primeira execução)

Na primeira execução o app **imprime no console o link de autorização** (ele não abre o navegador sozinho). Copie o link e abra no navegador/perfil — ou janela anônima — onde está logada a conta que tem as turmas. O link só contém o ID do cliente e os escopos, nenhum token. Ao concluir, o Google redireciona para `http://127.0.0.1:<porta>/authorize/` e o app recebe o código; por isso o navegador precisa estar na **mesma máquina** do app.

Na tela "O Google não verificou este app", clique em **Avançado > Acessar ClassroomGradeReport**; o aviso é normal para apps pessoais.

O token fica em `%APPDATA%\ClassroomGradeReport\token\` e é reutilizado nas execuções seguintes. Se expirar ou for revogado, o app apaga o token e refaz o fluxo automaticamente.

**Trocar de conta:** rode `dotnet run --project ClassroomGradeReport -- --logout` (ou apague a pasta `token`) e execute de novo, autorizando com a conta desejada.

**Nunca commite** `credentials.json` nem o diretório do token (o `.gitignore` também cobre `client_secret*.json`, `credenciais*.json`, `token/` e os relatórios gerados: `*.csv`, `*.xlsx`, `*.txt`, `*.html`).

### Segurança e privacidade

- **Somente leitura:** o app pede apenas os 3 escopos `readonly` e não consegue alterar nada no Classroom.
- **Nada sai da máquina** além das chamadas à API do Google. Tokens e notas não são impressos em logs nem mensagens de erro.
- **Relatórios contêm dados de alunos:** guarde-os em local protegido e não os commite nem os envie a terceiros sem necessidade.
- Para revogar o acesso por completo: `--logout` (apaga o token local) e remova o app em [myaccount.google.com/permissions](https://myaccount.google.com/permissions).

## 🛠️ Solução de problemas

| Sintoma | Causa / solução |
|---|---|
| `O caminho do arquivo fornecido não existe: ClassroomGradeReport.` (mensagem do `dotnet`) | Você está dentro da pasta do projeto e usou `--project ClassroomGradeReport`. Rode da raiz do repositório ou omita o `--project` (veja [Onde executar os comandos](#onde-executar-os-comandos)). |
| `Arquivo credentials.json não encontrado` | Salve o JSON em `%APPDATA%\ClassroomGradeReport\credentials.json` ou use `--credentials <caminho>` / `CLASSROOM_CREDENTIALS_PATH`. |
| `Erro 403: access_denied` / "app não concluiu a verificação" | App em *Testing*: adicione o e-mail em **Público-alvo > Usuários de teste** (ou publique o app). |
| `Nenhuma turma encontrada em que você seja professor(a)` | Você autorizou com uma conta sem turmas (ou só com turmas arquivadas: use `--include-inactive`). Rode `--logout` e autorize com a conta certa. |
| API não habilitada (403) | Ative a **Google Classroom API** no projeto. |
| Autorização expirou após ~7 dias | App em *Testing*; publique como "In production" ou refaça a autorização (`--logout`). |
| O link de autorização termina em erro de conexão com `127.0.0.1` | O navegador precisa estar na **mesma máquina** do app e a porta não pode estar bloqueada pelo firewall; refaça o fluxo com `--logout`. |
| Criei `--csv relatorios` para uma turma e saiu um arquivo sem extensão | Para uma turma o caminho é um **arquivo** (`notas.csv`); use uma **pasta** só com `--all-courses`. |
| 429 / cota | O app tenta de novo com backoff; se persistir, aguarde e execute novamente. |

## 🚀 Como rodar

### Onde executar os comandos

O caminho passado em `--project` é **relativo à pasta em que você está**. Os exemplos deste README assumem a **raiz do repositório** (a pasta que contém `ClassroomGradeReport.slnx`):

```
classroom-grade-report-net/        <- rode os comandos aqui
├── ClassroomGradeReport.slnx
├── ClassroomGradeReport/          <- projeto (Program.cs, .csproj)
└── ClassroomGradeReport.Tests/
```

Se estiver **dentro da pasta do projeto** (`ClassroomGradeReport/`), omita o `--project`:

```bash
# na raiz do repositório
dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios

# dentro de ClassroomGradeReport/
dotnet run -- --all-courses --csv relatorios
```

Exemplo completo, de dentro de `ClassroomGradeReport/`, usando um `credenciais.json` guardado ao lado do `Program.cs` e salvando todas as turmas em CSV, Excel e TXT na pasta `relatorios`:

```bash
dotnet run -- --credentials credenciais.json --all-courses --csv relatorios --xlsx relatorios --txt relatorios
```

O mesmo comando, a partir da raiz do repositório:

```bash
dotnet run --project ClassroomGradeReport -- --credentials ClassroomGradeReport\credenciais.json --all-courses --csv relatorios --xlsx relatorios --txt relatorios
```

> Se o `credentials.json` estiver no local padrão (`%APPDATA%\ClassroomGradeReport\`), o `--credentials` pode ser omitido. Acrescente `--html relatorios` para gerar também a página HTML.

Nos dois casos, os caminhos relativos (`--csv relatorios`, `--credentials credenciais.json`) são resolvidos a partir da pasta atual, ou seja, os arquivos são criados nela. Prefira caminhos absolutos quando quiser um local fixo, por exemplo `--csv "C:\Users\voce\Documents\relatorios"`.

### Ajuda

```bash
dotnet run --project ClassroomGradeReport -- --help
```

| Opção | Descrição |
|---|---|
| `--course <id\|nome>` | Turma (ID ou parte do nome); pula o menu |
| `--csv <caminho>` | Exporta CSV (`;`, vírgula decimal, UTF-8 com BOM). Para uma turma, `<caminho>` é o **arquivo** (ex.: `notas.csv`); com `--all-courses`, é uma **pasta** |
| `--xlsx <caminho>` | Exporta Excel (`.xlsx`); pode ser usado junto com `--csv`. Com `--all-courses`, é uma pasta |
| `--txt <caminho>` | Exporta texto simples (`.txt`, tabela alinhada, UTF-8 com BOM); combina com `--csv`/`--xlsx`. Com `--all-courses`, é uma pasta |
| `--html <caminho>` | Exporta uma página HTML autocontida (abre em qualquer navegador, sem internet); combina com os demais formatos. Com `--all-courses`, é uma pasta |
| `--all-courses` | Relatório de todas as turmas, sem menu; com `--csv`, `--xlsx`, `--txt` ou `--html` apontando para uma pasta, gera um arquivo por turma (`Nome-ID.csv` / `.xlsx` / `.txt` / `.html`). Não combina com `--course` |
| `--include-drafts` | Usa `draftGrade` quando não houver `assignedGrade` |
| `--missing-as-zero` | Atividade sem nota vale 0 |
| `--include-inactive` | Inclui turmas não ativas (arquivadas etc.); por padrão só as ACTIVE |
| `--credentials <caminho>` | Caminho do `credentials.json`. Precedência: `--credentials` → variável `CLASSROOM_CREDENTIALS_PATH` → `%APPDATA%\ClassroomGradeReport\credentials.json` |
| `--logout` | Apaga o token salvo; a próxima execução pede autorização de novo (útil para trocar de conta) |
| `-h`, `--help` | Ajuda |

Exemplos:

```bash
dotnet run --project ClassroomGradeReport
dotnet run --project ClassroomGradeReport -- --course "Matemática" --csv notas.csv
dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios
# todos os formatos ao mesmo tempo
dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios --xlsx relatorios --txt relatorios --html relatorios
# credencial em outro local (ex.: dentro da pasta do projeto, rodando da raiz do repositório) + todas as turmas + um CSV por turma na pasta "relatorios"
dotnet run --project ClassroomGradeReport -- --credentials ClassroomGradeReport\credenciais.json --all-courses --csv relatorios --xlsx relatorios --txt relatorios
dotnet run --project ClassroomGradeReport -- --all-courses --include-inactive
dotnet run --project ClassroomGradeReport -- --logout
dotnet run --project ClassroomGradeReport -- --course 123456789 --include-drafts --missing-as-zero
```

**Menu de turmas:** sem `--course` nem `--all-courses`, o app lista as suas turmas; escolha com as setas ↑/↓ e **Enter**. Para sair, use **Ctrl+C**.

**Desempenho:** alunos, atividades e entregas de cada turma são buscados ao mesmo tempo, e com `--all-courses` até **3 turmas** carregam em paralelo (limite para respeitar a cota da API); os resultados continuam saindo na ordem original. As páginas de cada lista são lidas em sequência (paginação por `nextPageToken`).

**Erros e saída:** erros de API 429/5xx são repetidos com backoff exponencial (até 5 tentativas). Mensagens de erro vão em português e o app termina com código `1` (erro), `130` (cancelado com Ctrl+C) ou `0` (sucesso).

**Excel (`.xlsx`):** a aba **Notas** tem cabeçalho formatado, linha e colunas de aluno/e-mail congeladas, e **números reais** (dá para somar, ordenar e filtrar; o percentual é uma fração formatada como `0,0%`). Notas ausentes ficam como `-`. A aba **Critério** registra a turma, o critério de nota usado e os avisos. Nomes e títulos são gravados sempre como texto, nunca como fórmula.

**HTML:** página única e autocontida (CSS embutido, sem JavaScript e sem requisições externas, com uma política de segurança de conteúdo restritiva). Tem cabeçalho e coluna de nomes fixos ao rolar, percentual colorido por faixa (≥ 70% verde, ≥ 50% amarelo, abaixo vermelho), tema claro/escuro automático e layout próprio para impressão (Ctrl+P, ou "Salvar como PDF"). Todo texto vindo da API é escapado.

**TXT:** cabeçalho com a turma, o critério de nota e os avisos, seguido de uma tabela com colunas alinhadas (nomes à esquerda, números à direita), própria para ler no Bloco de Notas ou colar em e-mails. Notas ausentes aparecem como `-`.

**CSV:** colunas `Aluno`, `E-mail`, uma por atividade (`Título (/máximo)`), `Total`, `Total possível` e `Percentual`. Textos que começam com `=`, `+` ou `@` recebem um `'` na frente para evitar injeção de fórmula no Excel. Os arquivos contêm dados de alunos: o `.gitignore` ignora `*.csv`, `*.xlsx`, `*.txt` e `*.html`; evite gravá-los em pastas sincronizadas ou compartilhadas.

## ✅ Testes

```bash
dotnet test
```

Os testes cobrem o cálculo (nota ausente vs. zero, draft vs. assigned, atividades sem `maxPoints`, aluno removido, percentual, ordenação), o CSV, o Excel, o TXT, o HTML (incluindo escape de texto) e o parser de argumentos. Não chamam a API real.

## 🗂️ Estrutura

```
ClassroomGradeReport/
├── Program.cs     orquestração enxuta (CLI → API → cálculo → saídas)
├── Cli/           parser dos argumentos e texto do --help
├── Auth/          OAuth Desktop (escopos readonly), caminhos, link impresso no console
├── Api/           cliente fino sobre Google.Apis.Classroom.v1: paginação, retry/backoff, erros em pt-BR
├── Domain/        records: Course, Student, Assignment, Submission
├── Reporting/     cálculo do relatório (lógica pura, sem I/O) e IGradingPolicy
├── Rendering/     saídas: console (Spectre.Console), CSV, Excel, TXT e HTML
└── Errors/        exceções com mensagens para o usuário
ClassroomGradeReport.Tests/   testes xUnit (cálculo e formatos de saída)
```

Pacotes principais: `Google.Apis.Classroom.v1`, `Google.Apis.Auth`, `Spectre.Console`, `ClosedXML` e `Microsoft.Extensions.DependencyInjection`.

## 📄 Licença

Distribuído sob a licença [MIT](LICENSE).
