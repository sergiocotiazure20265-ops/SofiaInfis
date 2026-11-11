# Interface SOFIA — Blazor / .NET 10

Projeto adicional à API existente. Landing page com as artes fornecidas, identificação
por nome, chat, anexo/arraste de PDF, TXT e XML, análise por especialista, síntese final
e rastreamento expansível. Layout adaptado para desktop, tablet e celular.

Documentação da entrega: [spec 002](../specs/002-interface-blazor/spec.md),
[plano](../specs/002-interface-blazor/plan.md),
[quickstart](../specs/002-interface-blazor/quickstart.md),
[arquitetura atual](../docs/ARQUITETURA_SOFIA.md) e
[prints das telas](../docs/INTERFACE_SOFIA.md).

Execução completa em containers: `./scripts/Start-Sofia.ps1`, na raiz da solução.

## Executar

Na raiz da solução:

```powershell
dotnet run --project SofiaInfis.Web --launch-profile demo
```

Abra [SOFIA local](http://localhost:5275/). O perfil **demo** usa dados explicitamente simulados,
somente em Development, sem API, MongoDB, agentes ou OpenAI. Não há fallback
automático para respostas fictícias quando a conexão real falha.

Para conectar à API real em outra janela de terminal:

```powershell
$env:Sofia__ApiBaseUrl = 'http://localhost:5151/'
$env:Sofia__ServiceToken = '<TOKEN-DE-SERVICO-CONFIGURADO-NA-API>'
dotnet run --project SofiaInfis.Web --launch-profile http
```

Também é possível usar User Secrets (`Sofia:ApiBaseUrl`, `Sofia:ServiceToken`).
Não grave o token em appsettings compartilhado. O arquivo `appsettings.example.json`
é apenas referência; as opções têm defaults no código. Para outras capacidades,
configure `Sofia__Capabilities__0`, `__1` etc., conforme o catálogo autorizado.

A API deve estar em execução. Para o fluxo opcional por especialistas, configure
`Sofia__UseSpecialistAgents=true` e habilite o módulo A2A,
Mongo e agentes conforme o README da solução. O ClientId de serviço deve possuir
`a2a:agents:read`, `a2a:execute`, `a2a:read`, `a2a:cancel` e as capacidades solicitadas.
Use HTTPS fora do loopback local; redirecionamentos HTTP não são seguidos.

## Fluxos e decisões

- Blazor Interactive Server: a credencial fica no servidor; o navegador não chama
  a API diretamente. A conexão servidor-servidor não exige ampliar CORS.
- O nome é identificação de contexto, **não autenticação**. Cada circuito possui
  sua conversa transitória, sem URLs públicas de consulta por ID nem armazenamento
  de nome/documentos no navegador. A interface publicada é pública, sem login;
  a chave de acesso da API permanece no servidor. Este projeto não cria contas ou SSO.
- Com especialistas habilitados: seleciona por pergunta/conteúdo; cada agente recebe
  somente anexos compatíveis ou contexto textual da pergunta. Sem correspondência,
  usa `/api/v1/sofia/perguntar`, com RAG/OpenAI.
- Com documentos e `UseSpecialistAgents=false` (padrão): envia PDF/TXT/XML diretamente
  a `/api/v1/sofia/perguntar`, com extração e RAG/OpenAI na API.
- `/conhecimentos`: registra texto ou PDF/TXT/XML na base persistente.
- Com `UseSpecialistAgents=true`: extrai texto quando há anexos e envia a
  `/api/v1/orquestracoes`, acompanha a execução e consulta resultados por chamada.
  O frontend nunca chama os agentes diretamente nem muda o wire A2A.
- Também é possível anexar arquivos sem escrever: ao clicar em Enviar, a interface
  usa a pergunta “Analise os documentos anexados e apresente os principais achados
  e limitações.”, visível na conversa. Enter envia; Shift+Enter insere nova linha.
- Limites:5 arquivos,5 MiB cada,20 MiB no total; pergunta5–2.000 caracteres;
  nome até 200. No fluxo especializado, textos que seriam truncados são **rejeitados** antes
  da delegação, com orientação para dividir o documento. PDF de imagem exige OCR
  externo; este projeto não adiciona OCR.
- “Gerar síntese final” envia as análises e limitações em um TXT ao endpoint
  original de perguntas. Isso utiliza RAG/OpenAI e pode consumir tokens reais.
  O ID da síntese e seus tokens ficam separados do ID da orquestração. As análises
  individuais permanecem intactas. Limite20.000 caracteres na entrada da síntese;
  acima disso, a UI preserva e apresenta apenas as contribuições individuais.
- Cancelamento solicita interrupção à API e distingue confirmação de encerramento
  apenas da espera. Cada envio tem orçamento de 150 s; síntese até120 s.
- Rastreamento mostra etapas observadas pelo cliente, IDs, estados, durações e
  consumo disponível. Não é um visualizador completo de spans OpenTelemetry.
  A API atual não expõe taskId/contextId remotos nessa consulta; não são inventados.
- Markdown é renderizado como texto codificado, com ênfase/títulos/listas básicos;
  HTML e scripts recebidos não são executados.
- A conversa não é memória de LLM: mensagens anteriores não são automaticamente
  reenviadas ao modelo. Recarregar ou “Nova conversa” limpa a sessão local.
  Limite20 mensagens por conversa para limitar retenção em memória.

## Arquitetura e compatibilidade

`Components/Pages/Home` → `ChatPanel`/`RunTrace` → `ISofiaChat` → `LiveSofiaChat`
→ API HTTP existente. `DocumentPreparation` aplica os limites e usa o extrator
existente. `DemoSofiaChat` é uma implementação separada, selecionada apenas pelo
perfil de demonstração em Development. JS restrito a arraste, teclado, foco e scroll.

A referência ao projeto API reutiliza os DTOs canônicos e o extrator, evitando
duplicar contratos e parsers nesta entrega. O host da API não é iniciado dentro do
frontend. Nenhuma dependência produtiva existente ou contrato HTTP foi alterado.
Em uma evolução futura, esses tipos podem ser extraídos para uma biblioteca
compartilhada, mediante planejamento, sem ser necessário nesta interface.

As imagens são cópias das artes fornecidas, sem alterações no arquivo original.
Fontes usam recursos locais; não há CDN, analytics ou envio de documentos a terceiros
pelo frontend além dos serviços de análise explicitamente configurados na solução.

## Validação

```powershell
dotnet build SofiaInfis.slnx
dotnet test SofiaInfis.Api.Tests --filter 'FullyQualifiedName~SofiaWeb'
```

Os testes `SofiaWebGatewayTests` usam HTTP sintético para contratos, erros,
correlação e síntese. `SofiaWebRenderingTests` verifica o host e codificação de HTML.
`SofiaWebA2AIntegrationTests` atravessa o cliente do frontend, API real de teste,
dois agentes Kestrel, Mongo/PgVector isolados e OpenAI sintética.
Prepare os bancos conforme `../SofiaInfis.Api.Tests/Fixtures/README.md` para esse
teste de integração e para a suíte completa. Nenhum teste exige agentes de alunos
ou uma chave real de OpenAI.

## Entrada de voz no chat

Clique no microfone **Ditar pergunta**, permita o uso pelo navegador e fale em
português. Clique novamente ou pressione Esc para encerrar. Revise o texto e
clique em Enviar; nenhuma transcrição envia a pergunta automaticamente.

O texto falado é acrescentado ao texto já digitado. Cada ditado dura até um minuto
e respeita o limite de 2.000 caracteres do compositor, com aviso ao atingir o limite.
A captura termina ao sair da aba, trocar de conversa ou perder a conexão do chat.
Permissão negada, falha de rede ou ausência de suporte não impedem digitação/anexos.

Requer HTTPS (ou localhost) e um navegador com reconhecimento de fala disponível.
O navegador pode usar um serviço on-line para reconhecer o áudio. A SOFIA não grava
nem envia áudio à API; apenas o texto segue para o fluxo existente após o envio.
Não há novo serviço pago de transcrição. A qualidade e o suporte dependem do navegador.

Especificação: [005 — Entrada de voz](../specs/005-comando-voz/spec.md).
Teste do adaptador, na raiz: `node --test tests/voice-input.test.mjs`.
