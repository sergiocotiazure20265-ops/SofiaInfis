# Validação da integração RAG

Execução local autorizada em 2026-11-09, America/Sao_Paulo.

## Implementação e testes

- Compilação .NET 10 concluída; imagens Release da API e Web construídas.
- Suíte completa: **88 aprovados, zero falhas, zero ignorados**, em 43 segundos:
  `dotnet test SofiaInfis.slnx --no-build --no-restore --logger "trx;LogFileName=rag-final.trx"`.
- Evidência local: `SofiaInfis.Api.Tests/TestResults/rag-final.trx` (ignorado no Git).
- Casos novos: XML fiscal com namespaces/atributos; XML malformado/DTD/entidades
  externas; ingestão TXT/XML → PgVector → busca → chat sem agentes; rejeição de
  ingestão truncada; criação de tabela ausente e reinício preservando conhecimentos.
- OpenAI nos testes automatizados é sintética; PostgreSQL e Mongo são containers
  descartáveis nas portas 54329 e 27029. Teste Postman existente também aprovado.
- A concorrência entre classes foi limitada com `xunit.runner.json`: execuções
  anteriores sobrecarregaram Mongo durante builds, causando 503/timeouts. Os testes
  de concorrência continuam executando chamadas simultâneas internamente.
- Um teste A2A comparava um contador enquanto a primeira chamada autorizada ainda
  estava pendente. O teste agora espera essa execução terminar antes de verificar
  que a chamada proibida não invoca o agente. Não houve mudança de regras A2A.

## Stack real e OpenAI

- Sete serviços iniciados: API, Web, PostgreSQL/PgVector, MongoDB, SQL Server,
  pgAdmin e Mongo Express. Bancos e ferramentas com health checks saudáveis.
- API `http://localhost:5151/health`: HTTP 200. Web `http://localhost:5275`: HTTP 200.
- A base anterior contém **35 trechos**, preservados após startup, ingestões de
  validação e reinício dos aplicativos.
- Após autorização específica do usuário para enviar os trechos recuperados à
  OpenAI, `scripts/Test-Sofia.ps1` executou busca semântica real e pergunta com TXT
  e XML juntos. Resultado: 3 referências RAG, 2 arquivos processados, código de
  controle e valor do XML confirmados, 1.914 tokens de chat informados pelo provedor.
  Execução: `5f54fe8d-2b6a-4c9d-94cd-1c36534c6da2`.
- Ingestão real dos dois documentos sintéticos: um chunk por documento; ambos
  recuperados pela busca. Somente esses dois registros, identificados por uma
  fonte UUID exclusiva da validação, foram removidos; contagem final: 35.
- Verificação pelo navegador: apresentação, identificação, chat conectado,
  formulário de conhecimentos e resposta real sobre serviços da INFIS com fontes.
- Imagens não contêm `appsettings.json` local nem arquivos de configuração de
  exemplo. A credencial é fornecida em ambiente durante startup, sem ser exibida.
- A2A está desativado na stack de uso; nenhum agente real de aluno foi acessado.

## Limites observados

- Suíte local não comprova interoperabilidade com agentes reais; módulo opcional.
- PDF textual suportado; OCR e memória conversacional permanecem fora desta entrega.
- Limites de extração preservados; ingestão recusa truncamento. A ingestão não
  adiciona documentos do chat automaticamente à base.
- NuGet emite avisos NU1902/NU1903 para dependência transitiva preexistente
  `System.Security.Cryptography.Pkcs 9.0.18`; não foi feita atualização incidental
  da árvore de pacotes nesta implementação.
- PostgreSQL existente informa divergência de versão de collation (2.41/2.36).
  Busca vetorial e operações de leitura/escrita passaram; não foram refeitos índices
  textuais nem alterado o metadado do banco do usuário nesta entrega.
- Postman CLI 1.86.0 disponível, sem workspace inicializado/login; a simulação
  local existente executa sem publicação nem chamada a agentes reais.
