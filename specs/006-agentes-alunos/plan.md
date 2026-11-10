# Plano

1. Inspecionar constituição, skills e implementação de descoberta, registry, transporte, credenciais, autorização, workers, Mongo e gateway.
2. Manter o módulo existente e adicionar catálogo público distribuído com API e Web. Fazer merge por IdAgente; configuração explícita precede defaults dos alunos. Ativação por flag evita contaminar testes e implantações anteriores.
3. Verificar DNS A/AAAA e HTTPS em cada destino; manter pinning exato por URL/IP e rejeição de redirects. Novos IPs de Vercel precisam nova revisão; nunca liberar faixa inteira.
4. Configurar segredos no Render por merge, preservando variáveis existentes. Provisionar Mongo Atlas somente após acesso autorizado disponível, com usuário limitado ao banco e rede de saída autorizada.
5. Token de entrada independente no servidor Blazor; somente hash no cliente da API e quatro permissões limitadas. Não usar Basic como autenticação A2A, nem token compartilhado como credencial de entrada.
6. Teste externo opt-in atravessa o gateway real e API em TestServer com Mongo isolado, endpoints Vercel reais e arquivo sintético. Validador existente verifica contrato/correlação; cliente existente verifica Task/Artifact. Testes normais continuam com agentes e OpenAI simulados.
7. Publicar somente após validação e registrar separadamente os checks do Render/interface. Caso Mongo inacessível, deixar flags globais desligadas e documentar variáveis exatas pendentes.

Pacotes existentes: .NET 10, Microsoft.Agents.AI.A2A preview 1.27.0-preview.261030.1; transporte da aplicação já implementa o perfil JSON-RPC 0.3.0 diretamente. Nenhuma API de SDK nova presumida. Sem novo CORS: frontend Blazor chama API pelo servidor.
