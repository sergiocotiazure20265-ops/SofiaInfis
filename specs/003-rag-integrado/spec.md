# SOFIA integrada com RAG e documentos

Autorização: solicitação do responsável em 2026-11-09 para implementar o necessário
ao funcionamento integrado, executar os containers e aceitar TXT/XML, sem acessar
outros agentes nesta etapa.

## Cenários e contrato

- Frontend real envia perguntas com ou sem PDF/TXT/XML ao endpoint multipart
  existente `POST /api/v1/sofia/perguntar`; API consulta PostgreSQL/PgVector e OpenAI.
- Especialistas são opcionais por `Sofia:UseSpecialistAgents`, desativado por padrão.
  Contratos e implementação A2A permanecem disponíveis para ativação posterior.
- `/conhecimentos` permite ingestão textual ou por arquivo. Endpoint novo:
  `POST /api/v1/conhecimentos/ingest/arquivo`, multipart `titulo`, `fonte`, `arquivo`.
  Mantém o recibo da ingestão JSON existente. Título: 5–200; fonte: 3–500 caracteres.
- PDF/TXT/XML: até 5 MB por arquivo; perguntas: até cinco anexos e 20 MB totais
  incluindo multipart; extração: 20.000 caracteres, com marcador de truncamento.
  Ingestão de arquivo recusa texto truncado ou com menos de 20 caracteres.
- XML preserva nomes de elementos, namespaces, atributos e valores; parser recusa
  DTD e entidades externas. XML malformado retorna 400 antes de consultar OpenAI.
- Inicialização idempotente cria extensão vector, tabela e índice HNSW quando
  `Rag:InitializeDatabase=true`; não remove nem reescreve conhecimentos existentes.
- `/health` verifica acesso ao esquema RAG, sem chamar OpenAI ou consumir tokens.
- Compose executa API e frontend .NET 10, PostgreSQL e infraestrutura existente.
  Credencial OpenAI permanece em configuração local/ambiente, fora das imagens.

## Impacto, compatibilidade e reversão

Problemas: frontend exigia A2A para anexos; banco não tinha bootstrap; Compose não
executava os aplicativos; XML era rejeitado. Mudanças autorizadas limitam-se a esses
fluxos e à alimentação da base. Alternativa de habilitar agentes foi descartada
conforme escopo solicitado. Endpoints existentes preservados; suporte XML aditivo.
Para reativar fluxo especializado, configurar A2A e `UseSpecialistAgents=true`.
Desabilitar `Rag:InitializeDatabase` impede DDL no início; dados permanecem intactos.
Nenhuma memória de chat ou informação institucional fictícia é adicionada à base.

## Aceite

Compilação, suíte existente, testes novos de XML/ingestão/chat direto, containers
executando e verificação de ingestão → busca → pergunta com serviços reais.
