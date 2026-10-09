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
- 📁 Exporta **CSV** pronto para o Excel pt-BR (`;`, vírgula decimal, UTF-8 com BOM), inclusive um arquivo por turma.
- 🔒 **Somente leitura**: nenhum escopo de escrita; nada sai da máquina além das chamadas ao Google.
- 🔁 Paginação completa, retry com backoff para 429/5xx e mensagens de erro claras em português.
- 🧩 Cálculo puro e testável, com ponto de extensão para pesos/categorias (`IGradingPolicy`).

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
- A API **não expõe a nota geral** calculada pelo Classroom; a soma é feita por esta aplicação. Pesos/categorias podem ser adicionados implementando `IGradingPolicy` (`Reporting/IGradingPolicy.cs`).

## 🔐 Por que preciso de credenciais?

A Google Classroom API **não é pública**: as turmas, alunos e notas pertencem à sua conta, e o Google só entrega esses dados a um aplicativo **identificado** e **autorizado por você**. Isso exige duas coisas diferentes:

| O quê | Para quê | Onde fica |
|---|---|---|
| **`credentials.json`** (cliente OAuth Desktop) | **Identifica o aplicativo** perante o Google (qual projeto, quais escopos, quota). Sozinho **não dá acesso a nenhum dado**. | Você baixa do Google Cloud e salva em `%APPDATA%\ClassroomGradeReport\credentials.json` (ou passa `--credentials <caminho>`) |
| **Token** (gerado na autorização) | Prova que **você** permitiu que o app leia suas turmas (somente leitura). É renovado automaticamente. | Criado na primeira execução em `%APPDATA%\ClassroomGradeReport	oken\` |

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

**Nunca commite** `credentials.json` nem o diretório do token (o `.gitignore` também cobre `client_secret*.json`, `credenciais*.json`, `token/` e `*.csv`).

## 🛠️ Solução de problemas

| Sintoma | Causa / solução |
|---|---|
| `Arquivo credentials.json não encontrado` | Salve o JSON em `%APPDATA%\ClassroomGradeReport\credentials.json` ou use `--credentials <caminho>` / `CLASSROOM_CREDENTIALS_PATH`. |
| `Erro 403: access_denied` / "app não concluiu a verificação" | App em *Testing*: adicione o e-mail em **Público-alvo > Usuários de teste** (ou publique o app). |
| `Nenhuma turma encontrada em que você seja professor(a)` | Você autorizou com uma conta sem turmas (ou só com turmas arquivadas: use `--include-inactive`). Rode `--logout` e autorize com a conta certa. |
| API não habilitada (403) | Ative a **Google Classroom API** no projeto. |
| Autorização expirou após ~7 dias | App em *Testing*; publique como "In production" ou refaça a autorização (`--logout`). |
| 429 / cota | O app tenta de novo com backoff; se persistir, aguarde e execute novamente. |

## 🚀 Como rodar

```bash
dotnet run --project ClassroomGradeReport -- --help
```

| Opção | Descrição |
|---|---|
| `--course <id\|nome>` | Turma (ID ou parte do nome); pula o menu |
| `--csv <caminho>` | Exporta CSV (`;`, vírgula decimal, UTF-8 com BOM). Com `--all-courses`, é uma pasta |
| `--all-courses` | Relatório de todas as turmas, sem menu; com `--csv <pasta>` gera um CSV por turma (`Nome-ID.csv`). Não combina com `--course` |
| `--include-drafts` | Usa `draftGrade` quando não houver `assignedGrade` |
| `--missing-as-zero` | Atividade sem nota vale 0 |
| `--include-inactive` | Inclui turmas não ativas (arquivadas etc.); por padrão só as ACTIVE |
| `--credentials <caminho>` | Caminho do `credentials.json` |
| `--logout` | Apaga o token salvo; a próxima execução pede autorização de novo (útil para trocar de conta) |
| `-h`, `--help` | Ajuda |

Exemplos:

```bash
dotnet run --project ClassroomGradeReport
dotnet run --project ClassroomGradeReport -- --course "Matemática" --csv notas.csv
dotnet run --project ClassroomGradeReport -- --all-courses --csv relatorios
# credencial em outro local (ex.: ao lado do projeto) + todas as turmas + um CSV por turma na pasta "relatorios"
dotnet run --project ClassroomGradeReport -- --credentials ClassroomGradeReport\credenciais.json --all-courses --csv relatorios
dotnet run --project ClassroomGradeReport -- --all-courses --include-inactive
dotnet run --project ClassroomGradeReport -- --logout
dotnet run --project ClassroomGradeReport -- --course 123456789 --include-drafts --missing-as-zero
```

Erros de API 429/5xx são repetidos com backoff exponencial.

**CSV:** colunas `Aluno`, `E-mail`, uma por atividade (`Título (/máximo)`), `Total`, `Total possível` e `Percentual`. Textos que começam com `=`, `+` ou `@` recebem um `'` na frente para evitar injeção de fórmula no Excel. Os arquivos contêm dados de alunos: o `.gitignore` ignora `*.csv`; evite gravá-los em pastas sincronizadas ou compartilhadas.

## ✅ Testes

```bash
dotnet test
```

Os testes cobrem o cálculo (nota ausente vs. zero, draft vs. assigned, atividades sem `maxPoints`, aluno removido, percentual, ordenação), o CSV e o parser de argumentos. Não chamam a API real.

## 🗂️ Estrutura

```
ClassroomGradeReport/        Program.cs, Cli/, Auth/, Api/, Domain/, Reporting/ (lógica pura), Rendering/, Errors/
ClassroomGradeReport.Tests/  xUnit
```

## 📄 Licença

Distribuído sob a licença [MIT](LICENSE).
