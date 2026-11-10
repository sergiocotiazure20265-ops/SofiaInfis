# Especificações da SOFIA

Atualizado em 2026-11-10. As entregas 001–004 possuem evidências de validação local
e/ou publicação. A 005 acrescenta ditado pt-BR ao chat, sem mudar os contratos da API.
Os relatórios distinguem testes sintéticos, verificações reais e limitações.

| Feature | Escopo | Documentos |
|---|---|---|
|001 — Integração A2A | Backend, contratos, descoberta, delegação, histórico e segurança | [Spec](001-integracao-a2a/spec.md), [plano](001-integracao-a2a/plan.md), [tarefas](001-integracao-a2a/tasks.md), [validação](001-integracao-a2a/validation-results.md) |
|002 — Interface Blazor | Apresentação, nome, chat, documentos, agentes, síntese e rastreamento | [Spec](002-interface-blazor/spec.md), [plano](002-interface-blazor/plan.md), [quickstart](002-interface-blazor/quickstart.md), [validação](../SofiaInfis.Web/VALIDACAO.md) |
|003 — RAG integrado | API/Web/RAG, TXT/XML, bootstrap e containers | [Spec](003-rag-integrado/spec.md), [plano](003-rag-integrado/plan.md), [tarefas](003-rag-integrado/tasks.md), [validação](003-rag-integrado/validation-results.md) |
|004 — Publicação gratuita | Render Free, Neon, autenticação e migração preservada | [Spec](004-publicacao-gratuita/spec.md), [validação](004-publicacao-gratuita/validation-results.md) |
|005 — Entrada de voz | Ditado pt-BR, revisão manual, suporte progressivo e cancelamento | [Spec](005-comando-voz/spec.md), [plano](005-comando-voz/plan.md), [tarefas](005-comando-voz/tasks.md), [validação](005-comando-voz/validation-results.md) |
|006 — Agentes dos alunos | Cadastro aditivo, credenciais separadas e validação dos agentes publicados | [Spec](006-agentes-alunos/spec.md), [plano](006-agentes-alunos/plan.md), [tarefas](006-agentes-alunos/tasks.md), [validação](006-agentes-alunos/validation-results.md) |
|007 — Expansão A2A | Mais seis agentes, descoberta paralela limitada e sucesso parcial | [Spec](007-expansao-catalogo-a2a/spec.md), [plano](007-expansao-catalogo-a2a/plan.md), [tarefas](007-expansao-catalogo-a2a/tasks.md), [validação](007-expansao-catalogo-a2a/validation-results.md) |

O backend 001 não foi reespecificado para incorporar a UI. A 002 documenta seu
consumidor e a síntese sob demanda pelo endpoint legado, mantendo os contratos.
O diagnóstico inicial é [histórico](../RELATORIO_ARQUITETURA_ATUAL_SOFIA.md);
o estado consolidado está na [arquitetura atual](../docs/ARQUITETURA_SOFIA.md).

O registro 002 foi consolidado após a implementação, com evidências já existentes.
Ele não representa execução retroativa de comandos Spec Kit ou uma nova rodada
de testes. Nenhuma branch, checklist de requisitos ou constituição foi alterada.

## Contexto dos comandos Spec Kit

Há mais de uma feature na branch `main`. Para apontar comandos futuros à 002 sem
persistir mudança no seletor do repositório, o helper existente aceita:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY = (Resolve-Path 'specs/002-interface-blazor').Path
$env:SPECIFY_FEATURE_NO_PERSIST = '1'
```

Essas variáveis não foram aplicadas por esta atualização documental. Se o próximo
comando exigir artefatos adicionais (por exemplo, tasks.md), eles devem ser
elaborados para o trabalho solicitado; não inferir tarefas pendentes apenas da
ausência de um arquivo gerado numa documentação retrospectiva.
