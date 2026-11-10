# Implementation Plan: 002 — Interface Blazor da SOFIA

**Date**: 2026-11-05. **Status**: arquitetura implementada, documentada retrospectivamente.
**Spec**: [spec.md](spec.md). **Operação**: [quickstart.md](quickstart.md).

## Decisão e impacto

A solicitação do usuário autorizou um novo projeto Blazor na solução. Foi adicionado
`SofiaInfis.Web`, sem modificar serviços, controllers, contratos ou dependências
produtivas existentes da API. O projeto de testes referencia a Web e a solução
lista os três projetos. Não houve nova migração de banco ou mudança de protocolo.

## Contexto técnico

- C# / .NET 10, ASP.NET Core Blazor Web App com Interactive Server.
- HttpClient e System.Text.Json; CSS responsivo e JS para arraste, foco e teclado.
- Referência ao projeto API para reutilizar DTOs canônicos, extrator PDF/TXT e
  tipos de consulta. O host API é outro processo, consumido por HTTP.
- Não foi criada uma biblioteca compartilhada nem reestruturada a solução nesta
  entrega. Esse acoplamento é uma escolha explícita de reaproveitamento; eventual
  extração de contratos/parsers exige evolução própria.
- Arte local, fonte instalada com fallback de sistema, sem CDN ou analytics.
- Testes existentes xUnit/Mvc.Testing/HtmlRenderer, sem novos pacotes de frontend.

## Responsabilidades

| Componente | Responsabilidade |
|---|---|
| Home | Apresentação, modal de nome e entrada no chat |
| ChatPanel | Compositor, anexos, conversa transitória, agentes e ações |
| RunTrace | Metadados, etapas e consumo em painel recolhido |
| MarkdownText | Renderização codificada com Markdown básico |
| Brand / UiIcon | Identidade e elementos visuais |
| ISofiaChat | Fronteira entre UI e comunicação |
| LiveSofiaChat | HTTP, acompanhamento, correlação, síntese e cancelamento |
| DemoSofiaChat | Respostas explicitamente simuladas em Development |
| DocumentPreparation | Limites e reutilização do extrator existente |
| chat.js | Eventos de arquivos, Enter, foco e scroll |

## Fluxos implementados

1. Nome é guardado no componente e contextualiza a solicitação especializada.
2. Sem anexo: multipart `pergunta` para `/api/v1/sofia/perguntar`.
3. Com anexos: validar/extrair → consultar agentes → POST orquestração → GET de
   execução paginado → GET de resultados por IDs construídos localmente.
4. Não seguir links remotos arbitrários retornados pela API; preservar correlação.
5. Síntese: serializar pergunta original, análises e limitações em TXT limitado,
   enviar ao endpoint legado de perguntas e guardar o ID da síntese separadamente.
6. Interrupção da espera: cancelamento vinculado ao circuito; solicitar cancelar
   à API quando houver ID documental e informar ausência de confirmação.

## Segurança e estado

O navegador conversa com o servidor Blazor. `Sofia:ServiceToken` só é usado pelo
servidor em operações A2A. Credenciais de serviço são configuradas por ambiente ou
User Secrets; não são DTO de negócio nem credencial digitada pelo usuário.
O cliente aceita HTTP apenas em loopback, exige HTTPS fora dele e não segue redirects.
Essa comunicação backend-backend não requer abrir a política CORS da API.

O ClientId configurado representa a integração Web perante a API; não representa
automaticamente cada pessoa por seu nome. O circuito restringe a conversa exibida
e não há rota pública de consulta de execução da UI. Isso não substitui autenticação
institucional nem oferece isolamento multiusuário autenticado por pessoa.

Nome, pergunta, arquivos e respostas da conversa são transitórios. A persistência
de análises A2A e sua retenção são responsabilidade da 001. Recarregar a UI não apaga
registros Mongo. Não há retomada de chat persistente ou memória de LLM.

## Validação e conformidade

9 testes novos: 6 casos em SofiaWebGatewayTests,2 em SofiaWebRenderingTests e1 em
SofiaWebA2AIntegrationTests. Última suíte completa:79 aprovados. As capturas em
[galeria](../../docs/INTERFACE_SOFIA.md) usam demonstração, enquanto o teste integrado
atravessa API de teste, dois agentes reais de fixture, Mongo/PgVector e OpenAI sintética.

Constituição: composição aditiva net10.0; DI e responsabilidades separadas; contratos
001 preservados; usuário/pergunta/textos e IDs mantidos; erros/timeout e rastreabilidade;
RAG/extração reutilizados; testes proporcionais; referência às skills do projeto.
Limites e condições não comprovadas permanecem explícitos na spec e na validação.
Nenhuma mudança da constituição foi necessária para registrar a interface.
