# 005 — Entrada de voz no chat

**Criada em**: 2026-11-10. **Autorização**: implementar comando de voz, atualizar
as specs, enviar ao GitHub e publicar a nova versão. Branch de entrega: `main`.
**Escopo**: ditado em português do Brasil, com texto revisável e envio manual.
Complementa [002](../002-interface-blazor/spec.md), preservando RAG, arquivos,
autenticação e contratos HTTP das entregas 001–004.

## Histórias e aceite

### US1 — Ditar a pergunta (P1)

1. O chat mostra um botão de microfone identificado como “Ditar pergunta”.
2. O microfone só é solicitado após clique deliberado; o navegador controla a
   permissão. Não há captura no carregamento da página ou entrada no chat.
3. O ditado usa `pt-BR`. Texto final é acrescentado à pergunta já digitada,
   sem duplicação de resultados cumulativos. Hipóteses parciais aparecem separadas.
4. Estados de permissão, escuta e finalização são anunciados por região de status.
5. O usuário encerra pelo mesmo botão, por Esc ou pela conclusão natural da fala.
6. O texto permanece editável. A transcrição nunca envia mensagem automaticamente;
   o envio manual continua usando a API, RAG e anexos existentes.

### US2 — Recuperar-se de limitações (P1)

1. Falta de suporte ou contexto inseguro desabilita apenas o microfone e explica
   a alternativa. Digitação, envio e arquivos permanecem funcionais.
2. Permissão negada, ausência de microfone/fala, rede, idioma e falha de início
   produzem avisos claros, sem apagar o texto existente e permitindo nova tentativa.
3. Perguntas continuam limitadas a 2.000 unidades UTF-16 no compositor. Ao atingir
   o limite, o ditado é encerrado com aviso; não corta pares de surrogates Unicode.
4. Cada captura dura no máximo 60 segundos, sem reinício automático.

### US3 — Encerrar a captura e preservar o chat (P1)

1. Nova conversa, saída do chat, descarte do componente ou perda da conexão com
   Blazor encerram a captura. Eventos atrasados não alteram a conversa nova.
2. Ao ocultar a aba, a captura termina e o texto final já reconhecido é preservado.
3. Durante escuta/finalização não se envia pergunta nem se inicia síntese ou nova
   leitura de anexos. Durante envio/leitura, o botão de voz fica indisponível.
4. Botão possui nome acessível, estado pressionado, foco visível e alvo adequado
   ao toque. A interface continua responsiva e respeita a preferência de movimento.

## Privacidade e limites

- O navegador pode usar um serviço on-line para reconhecimento; a UI informa isso
  antes da captura. A disponibilidade depende do navegador, idioma e serviço.
- A SOFIA não grava áudio, não mantém arquivos de áudio nem recebe áudio na API.
  Apenas texto é transitório no circuito Blazor; a API recebe a pergunta após Enviar.
- Não é adicionada conta/provedor pago de transcrição, nem alterada a chave OpenAI.
- Não inclui leitura falada das respostas, captura em segundo plano ou comandos
  falados de enviar/apagar mensagens. O comando do microfone é iniciar/parar ditado.
- Microfone real e qualidade fonética não são comprovados por mocks. A validação
  deve separar testes automatizados de qualquer teste real de áudio/permissão.

## Impacto e reversão

Mudanças aditivas no compositor, módulo JS isolado e estilos. Rotas, DTOs, limites
de documentos e base Neon permanecem iguais. O recurso falha para digitação se
não houver suporte. Reversão: restaurar o deploy anterior e o compositor anterior;
não há migração de dados ou mudança em contas/permissões cloud.
