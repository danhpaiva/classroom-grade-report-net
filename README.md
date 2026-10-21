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

[Início rápido](#-início-rápido) •
[Exemplo](#-exemplo-de-saída) •
[Provas](#-provas) •
[Configuração](#-arquivo-de-configuração) •
[Google Cloud](#-configuração-no-google-cloud) •
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
- 🧪 **Provas separadas:** atividades com "PROVA" no título (A1, A2…) nunca são somadas às demais nem entre si; cada uma tem relatório próprio, com **estatísticas da turma** (média, maior, menor, abaixo da média). Veja [Provas](#-provas).
- ⚖️ **Pesos por palavra no título** e padrões das opções em um [arquivo de configuração](#-arquivo-de-configuração) JSON opcional.
- 📁 Exporta **CSV** pronto para o Excel pt-BR (`;`, vírgula decimal, UTF-8 com BOM), **Excel (`.xlsx`)** com números reais e formatação, **texto (`.txt`)** alinhado e/ou **página HTML** autocontida, organizados em uma pasta por turma (`Geral/` e `Provas/`).
- ⏱️ Carrega várias turmas em paralelo, com barra de progresso, respostas parciais da API (`fields`) e cancelamento das demais se uma falhar.
- 🔒 **Somente leitura**: nenhum escopo de escrita; nada sai da máquina além das chamadas ao Google.
- 🔁 Paginação completa, retry com backoff para 429/5xx e mensagens de erro claras em português.
- 🧩 Cálculo puro e testável, com ponto de extensão para pesos/categorias (`IGradingPolicy`).

## 📋 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` deve mostrar 10.x).
- Windows (os caminhos padrão usam `%APPDATA%`; em outros sistemas informe `--credentials` e adapte o local do token).
- Uma conta Google com turmas no Classroom em que você é **professor(a)**.
- Um projeto no Google Cloud com a credencial OAuth (veja a seção "Configuração no Google Cloud").

## 🚀 Início rápido

```bash
git clone https://github.com/danhpaiva/classroom-grade-report-net.git
cd classroom-grade-report-net
dotnet build
```

1. Crie a credencial OAuth no Google Cloud e salve em `%APPDATA%\ClassroomGradeReport\credentials.json` (passo a passo em [Configuração no Google Cloud](#-configuração-no-google-cloud)).
2. Rode e escolha a turma no menu:

   ```bash
   dotnet run --project ClassroomGradeReport
   ```

3. Na primeira vez o app imprime um link; abra-o no navegador com a conta que tem as turmas e autorize ([Autorização](#-autorização-primeira-execução)).
4. Para gerar arquivos de todas as turmas (geral + provas separadas):

   ```bash
   dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios --xlsx relatorios
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

Cada prova sai em um relatório próprio, com as estatísticas da turma abaixo da tabela:

```
Turma: Matemática 9A — Prova A1
╭────────────┬──────────┬───────┬──────────┬────────╮
│ Aluno      │ Prova A1 │ Total │ Possível │      % │
│            │ (/10)    │       │          │        │
├────────────┼──────────┼───────┼──────────┼────────┤
│ Ana Souza  │        4 │     4 │       10 │  40,0% │
│ Bruno Lima │       10 │    10 │       10 │ 100,0% │
╰────────────┴──────────┴───────┴──────────┴────────╯
2 aluno(s), 1 atividade(s).
Avaliados: 2 de 2 aluno(s)
Média da turma: 7
Maior nota: 10
Menor nota: 4
Abaixo da média: 1 aluno(s)
```

## 📐 Regras de cálculo

- **Nota considerada:** por padrão, `assignedGrade` (nota já devolvida ao aluno). Com `--include-drafts`, usa `draftGrade` quando `assignedGrade` estiver ausente. O critério usado aparece no cabeçalho do relatório.
- **Sem nota não é zero:** aparece como `-` e não entra na soma. O **total possível** de cada aluno soma o `maxPoints` apenas das atividades *com nota* para ele. Com `--missing-as-zero`, atividade sem nota vale 0 e o `maxPoints` entra no total possível.
- Atividades sem `maxPoints` (ou 0) são ignoradas com um aviso (inclusive provas, por exemplo simulados); atividades em `DRAFT`/`DELETED` são ignoradas.
- **Provas não entram no relatório geral** e cada uma é calculada sozinha (veja [Provas](#-provas)).
- Submissões de alunos que não estão mais na turma são ignoradas.
- Alunos em ordem alfabética (sem diferenciar acentos/maiúsculas).
- Só entram atividades **publicadas** (é o que a API lista por padrão) e turmas em que você é **professor(a)** (`teacherId=me`); por padrão, apenas turmas ativas.
- Percentual = `total / total possível × 100`. Se não houver nenhuma nota para o aluno, o percentual aparece como `-`.
- A API **não expõe a nota geral** calculada pelo Classroom; a soma é feita por esta aplicação. Pesos por palavra no título podem ser definidos no [arquivo de configuração](#-arquivo-de-configuração); regras mais sofisticadas podem ser adicionadas implementando `IGradingPolicy` (`Reporting/IGradingPolicy.cs`).

## 🔐 Por que preciso de credenciais?

A Google Classroom API **não é pública**: as turmas, alunos e notas pertencem à sua conta, e o Google só entrega esses dados a um aplicativo **identificado** e **autorizado por você**. Isso exige duas coisas diferentes:

| O quê                                          | Para quê                                                                                                                    | Onde fica                                                                                                                    |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **`credentials.json`** (cliente OAuth Desktop) | **Identifica o aplicativo** perante o Google (qual projeto, quais escopos, quota). Sozinho **não dá acesso a nenhum dado**. | Você baixa do Google Cloud e salva em `%APPDATA%\ClassroomGradeReport\credentials.json` (ou passa `--credentials <caminho>`) |
| **Token** (gerado na autorização)              | Prova que **você** permitiu que o app leia suas turmas (somente leitura). É renovado automaticamente.                       | Criado na primeira execução em `%APPDATA%\ClassroomGradeReport\token\`                                                       |

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

| Sintoma                                                                                   | Causa / solução                                                                                                                                                                                  |
| ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `O caminho do arquivo fornecido não existe: ClassroomGradeReport.` (mensagem do `dotnet`) | Você está dentro da pasta do projeto e usou `--project ClassroomGradeReport`. Rode da raiz do repositório ou omita o `--project` (veja [Onde executar os comandos](#onde-executar-os-comandos)). |
| `Arquivo credentials.json não encontrado`                                                 | Salve o JSON em `%APPDATA%\ClassroomGradeReport\credentials.json` ou use `--credentials <caminho>` / `CLASSROOM_CREDENTIALS_PATH`.                                                               |
| `Erro 403: access_denied` / "app não concluiu a verificação"                              | App em *Testing*: adicione o e-mail em **Público-alvo > Usuários de teste** (ou publique o app).                                                                                                 |
| `Nenhuma turma encontrada em que você seja professor(a)`                                  | Você autorizou com uma conta sem turmas (ou só com turmas arquivadas: use `--include-inactive`). Rode `--logout` e autorize com a conta certa.                                                   |
| API não habilitada (403)                                                                  | Ative a **Google Classroom API** no projeto.                                                                                                                                                     |
| Autorização expirou após ~7 dias                                                          | App em *Testing*; publique como "In production" ou refaça a autorização (`--logout`).                                                                                                            |
| O link de autorização termina em erro de conexão com `127.0.0.1`                          | O navegador precisa estar na **mesma máquina** do app e a porta não pode estar bloqueada pelo firewall; refaça o fluxo com `--logout`.                                                           |
| Criei `--csv relatorios` para uma turma e saiu um arquivo sem extensão                    | Para uma turma o caminho é um **arquivo** (`notas.csv`); use uma **pasta** só com `--all-courses`.                                                                                               |
| Pedi `--csv notas.csv` e não achei `notas.csv` na pasta                                   | O arquivo fica em `notas/Geral/notas.csv` (e as provas em `notas/Provas/`). Veja [Provas](#-provas).                                                                                              |
| Minha prova não aparece                                                                   | O título precisa conter a palavra-chave (padrão `PROVA`; mude com `--exam-keyword`) e a atividade precisa ter pontuação máxima: sem `maxPoints` ela é ignorada com um aviso (ex.: simulados).      |
| `nenhuma atividade com "PROVA" no título` (com `--exams-only`)                            | A turma não tem provas com esse nome; confira o título no Classroom ou use `--exam-keyword`.                                                                                                      |
| `Configuração inválida em ...`                                                            | O JSON do `--config` tem erro de sintaxe, ou um peso sem `keyword`/`weight` maior que zero. Veja [Arquivo de configuração](#-arquivo-de-configuração).                                   |
| 429 / cota                                                                                | O app tenta de novo com backoff; se persistir, aguarde e execute novamente.                                                                                                                      |

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

| Opção                     | Descrição                                                                                                                                                                                                       |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--course <id\|nome>`     | Turma (ID ou parte do nome); pula o menu                                                                                                                                                                        |
| `--csv <caminho>`         | Exporta CSV (`;`, vírgula decimal, UTF-8 com BOM). Para uma turma, `<caminho>` é o **arquivo** (ex.: `notas.csv`); com `--all-courses`, é uma **pasta**                                                         |
| `--xlsx <caminho>`        | Exporta Excel (`.xlsx`); pode ser usado junto com `--csv`. Com `--all-courses`, é uma pasta                                                                                                                     |
| `--txt <caminho>`         | Exporta texto simples (`.txt`, tabela alinhada, UTF-8 com BOM); combina com `--csv`/`--xlsx`. Com `--all-courses`, é uma pasta                                                                                  |
| `--html <caminho>`        | Exporta uma página HTML autocontida (abre em qualquer navegador, sem internet); combina com os demais formatos. Com `--all-courses`, é uma pasta                                                                |
| `--all-courses`           | Relatório de todas as turmas, sem menu; os caminhos de exportação são **pastas** e cada turma ganha a sua (veja [Provas](#-provas)). Não combina com `--course`                                    |
| `--exam-keyword <texto>`  | Atividades cujo título contém o texto (sem diferenciar maiúsculas/acentos; padrão `PROVA`) são **provas**: cada uma tem relatório próprio e nunca é somada às demais atividades nem a outras provas                |
| `--exams-only`            | Gera somente os relatórios das provas                                                                                                                                                                           |
| `--no-exams`              | Não gera relatórios de prova (só as atividades gerais)                                                                                                                                                          |
| `--config <caminho>`      | Arquivo JSON com valores padrão e pesos (veja "Arquivo de configuração"). Padrão: `%APPDATA%\ClassroomGradeReport\config.json`, se existir. A linha de comando sobrescreve o arquivo                         |
| `--include-drafts`        | Usa `draftGrade` quando não houver `assignedGrade`                                                                                                                                                              |
| `--missing-as-zero`       | Atividade sem nota vale 0                                                                                                                                                                                       |
| `--include-inactive`      | Inclui turmas não ativas (arquivadas etc.); por padrão só as ACTIVE                                                                                                                                             |
| `--credentials <caminho>` | Caminho do `credentials.json`. Precedência: `--credentials` → variável `CLASSROOM_CREDENTIALS_PATH` → `%APPDATA%\ClassroomGradeReport\credentials.json`                                                         |
| `--logout`                | Apaga o token salvo; a próxima execução pede autorização de novo (útil para trocar de conta)                                                                                                                    |
| `-h`, `--help`            | Ajuda                                                                                                                                                                                                           |

Exemplos:

```bash
dotnet run --project ClassroomGradeReport
# uma turma: gera notas/Geral/notas.csv e notas/Provas/<prova>.csv
dotnet run --project ClassroomGradeReport -- --course "Matemática" --csv notas.csv
dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios
# todos os formatos ao mesmo tempo
dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios --xlsx relatorios --txt relatorios --html relatorios
# credencial em outro local (ex.: dentro da pasta do projeto, rodando da raiz do repositório) + todas as turmas + uma pasta por turma em "relatorios"
dotnet run --project ClassroomGradeReport -- --credentials ClassroomGradeReport\credenciais.json --all-courses --csv relatorios --xlsx relatorios --txt relatorios
dotnet run --project ClassroomGradeReport -- --all-courses --include-inactive
# só as provas, procurando "Avaliação" no título, usando um arquivo de configuração
dotnet run --project ClassroomGradeReport -- --all-courses --exams-only --exam-keyword "Avaliação" --config config.json --csv relatorios
dotnet run --project ClassroomGradeReport -- --logout
dotnet run --project ClassroomGradeReport -- --course 123456789 --include-drafts --missing-as-zero
```

**Menu de turmas:** sem `--course` nem `--all-courses`, o app lista as suas turmas; escolha com as setas ↑/↓ e **Enter**. Para sair, use **Ctrl+C**.

## 🧪 Provas

Por padrão, atividades com **"PROVA"** no título (em qualquer posição, sem diferenciar maiúsculas/acentos: `Prova - A1`, `PROVA A02`, `Prova Bimestral`…) são tratadas como provas:

- saem do relatório **geral** (não somam com as outras atividades);
- cada prova vira **um relatório próprio** (A1 e A2 nunca são somadas entre si);
- prova **sem pontuação máxima** (ex.: simulado) é ignorada, com um aviso, como qualquer atividade sem `maxPoints`.

Troque a palavra com `--exam-keyword "Avaliação"` (ou `examKeyword` no [arquivo de configuração](#-arquivo-de-configuração)). Use `--exams-only` para gerar só as provas ou `--no-exams` para só o geral.

### Organização dos arquivos

Com `--all-courses`, cada turma ganha uma pasta com o geral e as provas separados:

```
relatorios/
└── Matemática-123/
    ├── Geral/
    │   └── Matemática-123.csv
    └── Provas/
        ├── Prova - A1.csv
        └── Prova - A2.csv
```

Com `--course` e um arquivo (ex.: `notas.csv`), a mesma estrutura é criada ao lado dele: `notas/Geral/notas.csv` e `notas/Provas/<prova>.csv`. Provas com o mesmo título ganham o sufixo ` (2)`, ` (3)`… para não se sobrescreverem, e caracteres inválidos em nomes de arquivo viram `_`.

### Estatísticas por prova

Cada relatório de prova mostra, no console, no TXT, no HTML e na aba **Critério** do Excel:

| Estatística     | Significado                                                                        |
| --------------- | ---------------------------------------------------------------------------------- |
| Avaliados       | Alunos com nota, de quantos há na turma                                            |
| Média da turma  | Média das notas dos avaliados                                                      |
| Maior / menor   | Maior e menor nota                                                                 |
| Abaixo da média | Avaliados com nota **menor que a média da turma** (não é uma nota de corte como 6) |

Só entram alunos com nota (com `--missing-as-zero`, quem não tem nota conta como 0). O CSV segue só com os dados, sem estatísticas.

## ⚙️ Arquivo de configuração

Um JSON **opcional** com os padrões das opções e os pesos das atividades. É procurado em `%APPDATA%\ClassroomGradeReport\config.json` ou no caminho dado em `--config <caminho>` (se você informar um caminho e ele não existir, é erro). Comentários (`//`) e vírgula final são aceitos. **O que for passado na linha de comando sempre vence o arquivo.**

```json
{
  "examKeyword": "Prova",
  "includeDrafts": false,
  "missingAsZero": false,
  "includeInactive": false,
  "csv": "relatorios",
  "xlsx": "relatorios",
  "txt": "relatorios",
  "html": "relatorios",
  "credentials": "C:\\caminho\\credentials.json",
  "weights": [
    { "keyword": "Trabalho", "weight": 1 }
  ]
}
```

| Campo                             | Equivale a                                  |
| --------------------------------- | ------------------------------------------- |
| `examKeyword`                     | `--exam-keyword`                            |
| `includeDrafts` / `missingAsZero` | `--include-drafts` / `--missing-as-zero`    |
| `includeInactive`                 | `--include-inactive`                        |
| `csv`, `xlsx`, `txt`, `html`      | `--csv`, `--xlsx`, `--txt`, `--html`        |
| `credentials`                     | `--credentials`                             |
| `weights`                         | (só no arquivo) pesos por palavra no título |

**Pesos:** por padrão **toda atividade tem peso 1**: a nota exibida e somada é sempre a nota real lançada para o aluno. `weights` é opcional; se você criar uma regra, ela multiplica a nota **e** a pontuação máxima das atividades cujo título contém a palavra (sem diferenciar maiúsculas/acentos). Vale a **primeira** regra que combinar; sem regra, o peso é 1. Mantenha `weight: 1` (ou omita `weights`) para não alterar nenhuma nota. Cada peso precisa de `keyword` e de `weight` maior que zero. Os pesos valem também para os relatórios de prova.

## ⏱️ Desempenho e erros

- Alunos, atividades e entregas de cada turma são buscados **ao mesmo tempo**; com `--all-courses`, até **3 turmas** carregam em paralelo (limite para respeitar a cota da API), com uma **barra de progresso** por turma. Os relatórios são impressos e exportados na ordem original quando todas terminam.
- Se uma turma falha, as que ainda carregam são **canceladas** e o erro real é mostrado.
- Os pedidos à API usam **respostas parciais** (`fields`), trazendo só os campos usados. As páginas de cada lista são lidas em sequência (`nextPageToken`).
- Erros 429/5xx são repetidos com **backoff exponencial** (até 5 tentativas); os avisos de nova tentativa aparecem depois da barra, para não bagunçá-la.
- Mensagens de erro vão em português. Códigos de saída: `0` sucesso, `1` erro, `130` cancelado com Ctrl+C.

## 📁 Formatos de saída

- **Excel (`.xlsx`):** a aba **Notas** tem cabeçalho formatado, linha e colunas de aluno/e-mail congeladas, e **números reais** (dá para somar, ordenar e filtrar; o percentual é uma fração formatada como `0,0%`). Notas ausentes ficam como `-`. A aba **Critério** registra a turma, o critério de nota usado, os avisos e (nas provas) as estatísticas. Nomes e títulos são gravados sempre como texto, nunca como fórmula.
- **HTML:** página única e autocontida (CSS embutido, sem JavaScript e sem requisições externas, com uma política de segurança de conteúdo restritiva). Tem cabeçalho e coluna de nomes fixos ao rolar, percentual colorido por faixa (≥ 70% verde, ≥ 50% amarelo, abaixo vermelho), tema claro/escuro automático e layout próprio para impressão (Ctrl+P, ou "Salvar como PDF"). Todo texto vindo da API é escapado.
- **TXT:** cabeçalho com a turma, o critério de nota e os avisos, seguido de uma tabela com colunas alinhadas (nomes à esquerda, números à direita), própria para ler no Bloco de Notas ou colar em e-mails. Notas ausentes aparecem como `-`.
- **CSV:** colunas `Aluno`, `E-mail`, uma por atividade (`Título (/máximo)`), `Total`, `Total possível` e `Percentual`. Textos que começam com `=`, `+` ou `@` recebem um `'` na frente para evitar injeção de fórmula no Excel. Os arquivos contêm dados de alunos: o `.gitignore` ignora `*.csv`, `*.xlsx`, `*.txt` e `*.html`; evite gravá-los em pastas sincronizadas ou compartilhadas.

## ✅ Testes

```bash
dotnet test
```

Os testes (xUnit) não chamam a API real e cobrem:

- **Cálculo:** nota ausente vs. zero, draft vs. assigned, atividades sem `maxPoints`, aluno removido, percentual, ordenação, pesos.
- **Provas:** separação do geral e entre si, palavra-chave (maiúsculas/acentos), escopos `--exams-only`/`--no-exams`, estatísticas.
- **Saídas:** CSV (incluindo escape de fórmulas), Excel, TXT e HTML (incluindo escape de texto).
- **CLI e arquivos:** parser de argumentos, validação de opções, arquivo de configuração, caminhos de exportação (`Geral/`, `Provas/`, nomes repetidos).
- **Fluxo completo** (`ReportRunner`, com um gateway falso): arquivos gerados, cancelamento na primeira falha, Ctrl+C e avisos de retry.

## 🗂️ Estrutura

```
ClassroomGradeReport/
├── Program.cs     ponto de entrada enxuto (códigos de saída)
├── Cli/           App (fluxo de entrada), ReportRunner (carrega, calcula, imprime e exporta),
│                  CliOptions (--help), AppConfig (JSON), Export (formatos e caminhos), CourseSelector
├── Auth/          OAuth Desktop (escopos readonly), caminhos, link impresso no console
├── Api/           cliente fino sobre Google.Apis.Classroom.v1: paginação, `fields`,
│                  retry/backoff (ApiRetry, RetryNotifier), erros em pt-BR
├── Domain/        records: Course, Student, Assignment, Submission
├── Reporting/     cálculo do relatório (lógica pura, sem I/O): GradeReportBuilder, provas (ExamSplit),
│                  estatísticas, IGradingPolicy (pesos)
├── Rendering/     saídas: console (Spectre.Console), CSV, Excel, TXT e HTML
└── Errors/        exceções com mensagens para o usuário
ClassroomGradeReport.Tests/   testes xUnit (cálculo, provas, saídas, CLI/config e fluxo completo)
```

Pacotes principais: `Google.Apis.Classroom.v1`, `Google.Apis.Auth`, `Spectre.Console`, `ClosedXML` e `Microsoft.Extensions.DependencyInjection`.

## 📄 Licença

Distribuído sob a licença [MIT](LICENSE).
