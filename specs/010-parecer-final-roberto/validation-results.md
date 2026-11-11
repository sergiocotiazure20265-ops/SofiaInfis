# Validação — parecer final e cores

2026-11-11. Suíte focada: 100 aprovados, 0 falhas, 0 ignorados. Inclui pipeline real com servidores A2A locais, ordem após encerramento de todas as chamadas, execução única, limiar de um/dois especialistas, cancelamento, reserva de tempo, falhas parciais/totais, revisor indisponível, correlação, limite de pacote sem truncamento, schema publicado, resposta principal/autoria, contingência e renderização das cores.

Build da solução aprovado. Avisos preexistentes NU1902/NU1903 de System.Security.Cryptography.Pkcs 9.0.18 permanecem. A suíte completa dependente de Mongo/Docker não foi executada neste ambiente; testes novos usam repositório em memória e transporte HTTP real local.

O servidor publicado de Roberto requer adaptação ao documento parecer-final-sofia.v1. O cliente não fabrica lote fiscal e mantém a recusa visível. Ver compatibilidade-roberto.md.

## Publicação e teste real

Render: serviço sofia-infis, deploy dep-dbqb9hh42hec73cu695g, revisão e35437cb09c5a6ac85e96609d4521cb599524e21, estado live em 2026-11-11T18:33:37Z.

- Execução a0f41497-2b62-4838-a555-e832ececaf84: dois documentos sintéticos, ECD + NFS-e. Ambas análises parciais; Roberto aguardou a conclusão das duas e foi chamado uma vez na etapa final. Resposta dele: “Solicitação rejeitada antes do cruzamento. Nenhum documento contém JSON lote.v1.” A interface exibiu resposta da SOFIA com aviso explícito de parecer indisponível e autoria de contingência.
- Execução 8acf7d39-ecac-4bf6-928f-d5909b9f70ee: um documento NFS-e. Somente NFS-e acionado, sem etapa final; resposta SOFIA presente.
- Oito cores distintas verificadas via CSS computado no catálogo; ECD verde, NFS-e laranja e Roberto roxo no rastreamento. Desktop 1440 e celular 390 conferidos; largura do documento 390 no viewport 390, sem transbordamento horizontal.
- Primeira conexão do navegador de teste caiu antes de iniciar a análise; recarga e repetição concluíram normalmente. Não foi reproduzida nas duas execuções concluídas.

Evidências locais ignoradas pelo Git em artifacts/final-review: live-multiple.json, live-state.json, preview.png, live-desktop.png, live-mobile.png e live-trace.png. Os arquivos usados contêm somente dados sintéticos de treinamento.

**Resultado:** fluxo e cores publicados. A obtenção do parecer final real continua dependente da extensão no agente remoto do Roberto; a recusa foi confirmada, não apresentada como sucesso.
