# Diagramas de arquitetura

`arquitetura.excalidraw` tem quatro frames:

1. **Contexto e deploy**: usuário, containers do `docker-compose` (web, server, postgres) e o deploy pelo GitHub Actions.
2. **Projetos e dependências**: os projetos de `src/FMS.slnx` e as `ProjectReference` entre eles.
3. **Modelo de dados**: entidades de `FMS.Domain` com as chaves e relacionamentos de `EntityConfigurations.cs`.
4. **Fluxo de login**: o caminho do `Login.razor` até o JWT e a `UserSession`.

Convenção: caixa sólida existe no código; caixa tracejada vermelha está prevista em `docs/ARQUITECTURE.md` ou falta implementar.

## Como abrir e editar

- **Navegador:** em [excalidraw.com](https://excalidraw.com), menu > Abrir, escolha o arquivo e, depois de editar, menu > Salvar no disco por cima dele.
- **VS Code:** instale a extensão *Excalidraw* (`pomdtr.excalidraw-editor`); o arquivo abre direto no editor e salva no próprio `.excalidraw`.

Atualize o diagrama no mesmo PR que mudar a estrutura (novo projeto, entidade ou container).
