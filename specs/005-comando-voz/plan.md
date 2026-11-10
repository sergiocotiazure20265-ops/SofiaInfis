# Plano técnico

- Adaptador JS isolado `voice-input.mjs`, com Web Speech API padrão ou prefixada,
  detecção de contexto seguro, `pt-BR`, resultados parciais e finais.
- Início síncrono no clique nativo para preservar a ativação do usuário; .NET
  acompanha disponibilidade/estado/transcrição por JS interop.
- `ChatPanel` controla bloqueios, avisos, texto revisável e cancelamento ao navegar.
  `bindComposer` preserva teclado/arraste e descarta o adaptador ao encerrar.
- Importação de `chat.js` pelo catálogo fingerprinted `Assets`, evitando módulo
  antigo em cache após a publicação. Módulo de voz servido como JavaScript.
- Timer de um minuto; cancelamento em ocultação, dispose e falha de interop.
  Callbacks cancelados são ignorados; resultados cumulativos não são concatenados
  novamente a cada evento. Texto e hipóteses respeitam o limite do compositor.
- Testes Node com reconhecedor simulado para eventos e erros, sem capturar áudio.
  Testes .NET de entrega de assets e regressão Web. Inspeção visual sem ativar
  microfone real, salvo autorização específica para isso.
- Commit/push no repositório autorizado; deploy manual no serviço Render existente
  e confirmação do estado live, endpoints HTTPS e assets publicados.

Referências primárias:
[SpeechRecognition](https://developer.mozilla.org/en-US/docs/Web/API/SpeechRecognition),
[erros](https://developer.mozilla.org/en-US/docs/Web/API/SpeechRecognition/error_event),
[módulos Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/location-of-javascript?view=aspnetcore-10.0).
