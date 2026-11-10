# Pesquisa — 001 Integração A2A

Data: 2026-11-05. Pesquisa de planejamento; nenhuma integração executada.
Referências: [spec.md](spec.md) e [constituição](../../.specify/memory/constitution.md).

## R1. SDK e perfil realmente disponíveis

**Decisão:** manter dependências atuais e implementar futuramente adaptador HTTP
A2A 0.3.0/JSON-RPC, isolado por IA2AAgentClient, com IHttpClientFactory e
System.Text.Json. Não serializar modelos v1 como v0.3.

**Evidências locais:** obj/project.assets.json restaura
Microsoft.Agents.AI.A2A/1.27.0-preview.261030.1 com A2A/1.0.0-preview3.
O cache NuGet a2a/1.0.0-preview3/README.md:18–25 declara suporte v1;
lib/net10.0/A2A.xml:711–721 documenta conversão de card v0.3 para v1.
Conversão de card não comprova compatibilidade das mensagens.
A2A.V0_3 não está restaurado.

**Racional:** atender perfil constitucional sem upgrade/downgrade ou APIs presumidas.
**Alternativas:** adicionar SDK legado/compatibilidade exige nova dependência;
migrar turma para v1 exige emenda aprovada. Não houve teste wire; não se afirma
que todo SDK v1 seja incapaz de interoperar.
Fontes: [versão e dependências](https://www.nuget.org/packages/Microsoft.Agents.AI.A2A/1.27.0-preview.261030.1),
[guia Microsoft de migração](https://learn.microsoft.com/en-us/agent-framework/migration-guide/agent-to-agent-sdk-v1).

## R2. Contrato wire e identidade

**Decisão:** exigir protocolVersion 0.3.0 e preferredTransport JSONRPC explícitos.
Descobrir em /.well-known/agent-card.json e chamar o url aprovado do card.
ID canônico do agente fica no catálogo SOFIA, vinculado a card/nome/destino esperado;
não existe idAgente nativo no card.

**Racional:** version é versão do agente, não do protocolo. Exemplos conceituais
das skills omitem discriminadores kind de Message/Task e campos do card.
Novas fixtures complementam o envelope sem alterar o domínio 1.0.
O texto oficial exige preferredTransport, mas o schema permite omissão com default;
SOFIA usa perfil explícito para remover ambiguidade.
**Alternativa rejeitada:** inferir versão, destino ou identidade sem validação.
Fontes: [especificação 0.3](https://a2a-protocol.org/v0.3.0/specification/),
[schema oficial fixado](https://raw.githubusercontent.com/a2aproject/A2A/v0.3.0/specification/json/a2a.json).

## R3. Acompanhamento de tarefas

**Decisão:** message/send inicia, tasks/get acompanha por params.id e tasks/cancel
tenta cancelar no mesmo endpoint RPC. Extrair análise do Artifact de Task completed,
não da mensagem de progresso. Message direta não é resultado final nesta entrega.
Polling de 500 ms, sem reenvio da análise, limitado ao orçamento da chamada.

**Racional:** erro de negócio em tarefa concluída difere de tarefa protocolar falha.
Cancelar remotamente é tentativa, não garantia.
**Alternativas:** streaming e push ficam fora; retries de análise desabilitados.
Fonte: [schema oficial dos envelopes](https://raw.githubusercontent.com/a2aproject/A2A/v0.3.0/specification/json/a2a.json).

## R4. Aceitação assíncrona aditiva

**Decisão:** novos controllers para agentes e orquestrações; POST retorna 202,
com consulta/cancelamento separados. Aceitação exige validação, reserva de fila,
registro durável e enfileiramento antes de 202. Nome/pergunta/textos só em memória.
Cinco execuções ativas, dez pendentes, fila limitada; excesso retorna 429.
Falha de fila após gravação registra falha e devolve 503, sem disparar análise.

**Racional:** desconectar após 202 não cancela trabalho aceito. Reinício perde o
payload transitório e registra interrupção, sem retomada automática.
**Alternativas:** HTTP aberto por 120 s ou fila durável de documentos ampliariam
fragilidade ou persistência sensível. Instância única, sem processos sobrepostos.
Fonte: [workers .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers).

## R5. Persistência MongoDB

**Decisão:** MongoDB.Driver 3.13.0 e banco de histórico existente; duas coleções,
a2a_execucoes e a2a_chamadas, uma chamada/resultado por documento.

**Racional:** limite de 1 MiB é por envelope, não do conjunto; muitos agentes podem
ultrapassar 16 MiB BSON se todas as análises forem embutidas na execução.
**Alternativas:** documento único exigiria novo teto agregado; PostgreSQL exigiria
esquema novo misturado ao RAG. Não adicionar banco nem replica set.
Fonte: [limites MongoDB](https://www.mongodb.com/docs/manual/reference/limits/).

## R6. Atomicidade e recuperação

**Decisão:** atualização condicional por estado/versão/ownerRunId, sem upsert
de resultado. Gate por execução serializa gravação e fechamento na instância.
Registrar chamada antes do envio; confirmar salvo somente após acknowledgment.
Planejar w:1 e j:true, com timeout próprio de operação.

Finalizar depois das gravações locais; no reinício sem sobreposição, reconciliar
chamadas abertas como interrompidas e preservar terminais. Se gravação da chamada
concluir e resumo falhar, reconstruir resumo das chamadas persistidas.
Timeout com resultado incerto exige releitura por ID, não presunção de rollback.
Nunca reconstruir payload nem reenviar análise em recuperação.

**Racional:** atomicidade Mongo é por documento, não entre coleções.
**Alternativas:** transações exigiriam infraestrutura adicional; coordenação local
não serve para múltiplas réplicas. Scale-out/rolling deployment exige lease/fencing.
Fontes: [atomicidade](https://www.mongodb.com/docs/manual/core/write-operations-atomicity/),
[write concern](https://www.mongodb.com/docs/manual/reference/write-concern/).

## R7. Retenção e privacidade

**Decisão:** não gravar nome/pergunta/texto bruto; guardar IDs e resultados
protegidos. expiresAt = encerramento +30 dias também nas chamadas. TTL single-field,
leitura sempre com expiresAt > now e limpeza monitorada para exclusão <=24 h.
Registros abertos têm expiração provisória conservadora e reconciliação.

**Racional:** TTL é assíncrono, não garantia de exclusão imediata.
Indisponibilidade do banco deve gerar alerta e regularização; não cumprimento fictício.
**Alternativa rejeitada:** depender só de TTL para impedir consulta vencida.
Fonte: [TTL MongoDB 8.0](https://www.mongodb.com/docs/v8.0/core/index-ttl/).

## R8. Autenticação sem mudar os legados

**Decisão:** esquema SofiaA2AService, token opaco de serviço via Bearer,
hash SHA-256 configurado por ambiente, comparação constante.
Identidade clientId estável e permissões/capacidades; rotação mantém proprietário.
Novos endpoints com políticas explícitas, sem fallback global e sem Authorize
nos controllers antigos. 401 para credencial ausente/inválida, 403 para falta
de permissão e 404 para execução de outro proprietário/desconhecida/expirada.
Credencial remota separada por destino; nunca repassar token do chamador.

**Racional:** não há issuer/SSO/JWT existente. Token opaco não é JWT, embora use Bearer.
Tokens >=256 bits aleatórios, fornecidos fora do Git.
**Alternativa:** OIDC/JWT requer emissor e nova integração, não presumidos.
Fontes: [autenticação ASP.NET 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0),
[esquemas por política](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/authorize-with-a-specific-scheme?view=aspnetcore-10.0),
[FixedTimeEquals](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals?view=net-10.0).

## R9. HTTP/CORS e destinos

**Decisão:** clientes nomeados separados para discovery/RPC; redirects automáticos
desabilitados, validação de URL, porta, DNS e conexão efetiva contra destinos
permitidos. Credencial só vai ao destino explicitamente autorizado.
Docker local usa serviços internos aprovados; HTTPS fora da rede isolada,
sem desabilitar validação de certificado.

CORS só para frontend existente/solicitado: SofiaBrowser por endpoint novo,
origens explícitas, GET/POST/OPTIONS, Content-Type/Authorization, sem cookies.
Ausência de origens não bloqueia backend-backend nem startup legado.

**Racional:** allowlist textual de URL sozinha não evita SSRF por resolução DNS.
**Alternativa rejeitada:** abrir origens ou tratar CORS como autenticação.
Fontes: [IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
e [skill HTTP](../../.agents/skills/sofia-cors-rest-a2a/SKILL.md).

## R10. Limites e observabilidade

**Decisão:** 5 s descoberta, 60 s chamada incluindo polling, 120 s execução desde
aceitação incluindo fila/vagas, fechamento separado <=5 s; até três chamadas
simultâneas por execução. TimeProvider injetável para prazos/testes.
Limitar stream inclusive sem Content-Length e após descompressão.

ActivitySource/Meter SofiaInfis.A2A e exportação console sanitizada via pacotes
já existentes. Métricas por agente/operação/status; IDs só em logs/spans.
Tokens e custo nulos quando não fornecidos. Não inventar campo de consumo nativo
A2A; extensão depende de acordo documentado e fixture, fora do mínimo.

**Racional:** preservar privacidade e limitar cardinalidade.
**Alternativa rejeitada:** capturar corpos/segredos ou inferir tokens remotos.
Fonte: [métricas .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation).

## R11. Testes e dependências de desenvolvimento

**Decisão:** projeto SofiaInfis.Api.Tests net10 com Unit/Contract/Integration/Regression.
Versões presentes no cache: xunit 2.9.3, runner 3.1.4, Test SDK 17.14.1,
Mvc.Testing 10.0.9, JsonSchema.Net 7.3.4. Incluir/validar no restore na implementação.
Fixtures HTTP determinísticas e Mongo real de teste para CAS/reinício/TTL.
Regressão substitui clientes externos para não cobrar tokens.

**Racional:** stub de interface sozinho não prova wire/rede.
**Alternativas:** depender só de agentes reais não é reproduzível.
Nenhum projeto ou teste criado/executado nesta fase.

## R12. Contexto da feature e branch

**Decisão:** registrar BRANCH 001-integracao-a2a retornado pelo setup como contexto
de feature; Git real continua main. common.ps1 usa nome da pasta como fallback.
Não criar/trocar branch implicitamente.

## Verificação dos artefatos de desenho

Foram validados nove exemplos positivos: três payloads de domínio, um corpo de
criação local e cinco exemplos wire/card contra o schema oficial A2A fixado em
v0.3.0. Dezoito mutações negativas (campos obrigatórios ausentes e declaração de
texto incompleto) foram rejeitadas pelos schemas. A validação utilizou JsonSchema.Net
7.3.4 já presente no cache, via PowerShell/.NET, sem instalar dependências.
O schema oficial foi baixado somente para arquivo temporário.

Isso valida os exemplos de contrato, não o comportamento de uma implementação.
Build, HTTP entre serviços, persistência, prazos e testes de aplicação não foram
executados. JSON Schema não verifica correlação contextual nem evidências factuais.

**Conclusão:** escolhas resolvidas; validação de compilação, wire, persistência e
interoperabilidade pertence à implementação. Nenhuma dependência foi alterada.
