# Validação — entrada de voz

Data: 2026-11-10, America/Sao_Paulo.

## Testes locais

- `node --test tests/voice-input.test.mjs`: **12 aprovados**, zero falhas.
  Reconhecedor simulado: início deliberado, pt-BR/prefixo WebKit, ausência de
  suporte/HTTPS, resultados cumulativos e parciais, revisão sem envio automático,
  parada por clique/Esc, erros, cancelamento e callbacks atrasados, Unicode/limite,
  timeout, aba oculta, perda de circuito e cliques repetidos.
- Compilação .NET 10 concluída. Avisos NU1902/NU1903 da dependência transitiva
  preexistente Pkcs permanecem, sem mudança de pacotes nesta feature.
- Regressões .NET: **10 aprovadas**, zero falhas, no filtro de gateway Web,
  renderização e proteção cloud. O teste de host confirmou que `voice-input.mjs`
  é servido como JavaScript. Evidência local ignorada no Git:
  `SofiaInfis.Api.Tests/TestResults/voice-web.trx`.
- Inspeção em navegador com app local de demonstração: entrada no chat,
  microfone habilitado após a inicialização, aviso de processamento on-line,
  compositor em desktop e 390×844, envio por Enter e resposta demonstrativa.
  Controles de texto/anexos recuperados ao terminar. Nenhum microfone real foi
  ativado, nem permissão concedida automaticamente ou áudio de pessoa capturado.
- A primeira inspeção expôs um caminho relativo inválido para importação do asset
  fingerprinted; corrigido com prefixo `./` e repetida a inspeção com sucesso.
  Falha de importação agora preserva envio textual pelo botão com aviso.

## Publicação

Preparada para o mesmo serviço Render Free da entrega 004, preservando as variáveis
de ambiente e a autenticação existentes. A verificação pública usa
`scripts/Test-SofiaCloud.ps1 -TestVoiceAssets`, sem chamadas OpenAI ou captura de
áudio. Commit, deploy e resultados finais serão registrados após confirmação.

## Limites

Os testes simulam eventos da Web Speech API e não medem qualidade fonética nem
comprovam reconhecimento real em todos os navegadores. O serviço de reconhecimento,
o suporte a pt-BR e o prompt de microfone pertencem ao navegador. A SOFIA não passa
a receber áudio na API. A chave e o fluxo RAG permanecem os mesmos.
