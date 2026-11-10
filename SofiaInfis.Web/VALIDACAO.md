# Validação do frontend Blazor — 2026-11-05

Escopo: projeto novo `SofiaInfis.Web` net10.0, apresentação e chat responsivos,
identificação por nome, PDF/TXT, cliente da API existente, análises individuais,
síntese separada e rastreamento. A solicitação do usuário autoriza este novo
frontend; contratos e serviços produtivos existentes foram preservados.

Esta evidência acompanha a [spec 002](../specs/002-interface-blazor/spec.md), o
[plano](../specs/002-interface-blazor/plan.md) e a
[arquitetura atual](../docs/ARQUITETURA_SOFIA.md).

## Evidências automatizadas

- Restore pelo cache local, sem alterar versões produtivas existentes.
- Build da solução:0 erros; os avisos NU1902/NU1903 de dependência já existentes
  também aparecem no novo projeto por sua referência à API (6 avisos na última execução).
- 9 testes novos para o frontend: 6 casos de gateway, 2 de host/renderização segura
  e 1 integração com API, dois agentes Kestrel e Mongo/PgVector reais de teste.
- Suíte completa: **79 aprovados,0 falhas,0 ignorados**, duração reportada16 s.
- Comando: `dotnet test SofiaInfis.slnx --no-build --no-restore --logger trx --results-directory TestResults -v minimal`.
- Relatório local ignorado pelo Git:
  `TestResults/sergi_SERGIO_2026-11-05_15_15_13_net10.0.trx`.
- Cenários: multipart legado sem anexos; extração integral; correlação de duas
  contribuições; falha parcial; recusa de ID divergente; bloqueio de links remotos
  fornecidos na resposta; recusa de truncamento; erros sanitizados; síntese sem
  substituir a execução ou as análises; codificação de HTML não confiável.
- OpenAI sintética em todos os testes. Nenhuma chave/token real ou agente de aluno
  foi necessário. Alertas de dependências preexistentes não foram remediados;
  o restore final com NuGetAudit=false não equivale a uma nova auditoria.

## Conferência visual e interação

Navegador local com perfil de demonstração explícito. Conferidos:

- Landing desktop 1440 px, arte da personagem, navegação e chamada para o chat.
- Modal de nome, teclado e entrada na conversa.
- Envio por Enter, painel de rastreamento expandido com ID/etapa e menu móvel
  verificados na versão final; indicação DEMO permanece visível no cabeçalho.
- Anexo de TXT sintético pelo seletor e exibição do arquivo antes do envio.
- Envio, três contribuições demonstrativas, expansão de análise individual,
  geração de síntese demonstrativa e painel de rastreamento.
- Layout móvel 390×844: documento390 px, sem overflow horizontal; compositor
 362,4 px. Resposta e detalhes legíveis, compositor fixo, menu recolhível.
- Drag/drop implementado por eventos nativos e InputFile; a conferência manual
  usou o seletor. O gesto de arraste completo não foi automatizado no navegador.
- Artefato visual: `artifacts/frontend/sofia-desktop.png` (local, ignorado pelo Git).

As capturas posteriores das seis telas estão na
[galeria documental](../docs/INTERFACE_SOFIA.md), com cópias PNG em `docs/images/sofia`.
Incluem identificação, chat, análise individual, síntese, rastreamento e celular.
Essas capturas são evidência visual; não acrescentam novos testes automatizados.

As respostas apresentadas na inspeção visual são **demonstrativas**. A integração
automatizada separada atravessou a API real de teste e dois especialistas HTTP.
Interoperabilidade de agentes reais, disponibilidade de OpenAI, autenticação
institucional do frontend e deploy externo não foram validados nesta etapa.

Após os últimos ajustes visuais e de acessibilidade, os 9 testes do frontend foram
executados novamente e passaram. O host de demonstração ficou disponível na porta
5275; os dois bancos descartáveis de teste foram encerrados.

O rastreamento é de execução e etapas observadas pelo cliente; não promete spans
distribuídos que a API atual não expõe. A conversa é transitória e não adiciona
memória de LLM. A geração real de síntese usa o endpoint existente de perguntas e
depende da base RAG/OpenAI configurada. Consulte README.md para executar e conectar.

Atualização documental de 2026-11-05: spec 002, arquitetura consolidada e vínculos
com a 001 adicionados após a implementação. A suíte não foi reexecutada nessa
atualização;79 e9 permanecem os resultados históricos descritos acima.
