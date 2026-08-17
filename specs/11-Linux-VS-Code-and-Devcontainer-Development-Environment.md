# Persona Simulation Playground

## Linux, VS Code and Devcontainer development environment

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Date:** 2026-08-16

## 1. Goal

Development is done on Linux using VS Code. Framework and Playground each have their own dev container and can be built, tested, versioned and published independently.

Essentially all that is needed on the host is:

- Git;
- VS Code;
- Dev Containers Extension;
- Docker Engine or an explicitly tested compatible container runtime;
- Access to the two Git repositories;
- Browser for authentication and local UI testing.

The .NET SDK does not need to be installed on the host. The binding SDK version lives in the respective repository and dev container.

## 2. Repository splitting

```text
~/src/ddd-building-blocks/
  independent Git repository
  own dev container
  own CI and releases
  erzeugt NuGet-Pakete

~/src/persona-simulation-playground/
  standalone Git repository
  Own Devcontainer plus PostgreSQL service
  own CI and deployments
  consumes versioned DDD.BuildingBlocks packages

~/.local/share/nuget/persona-feed/
  lokaler NuGet-Ordnerfeed
  not a Git repository
  exchange point for prerelease packages
```

Both repositories are allowed to be viewed in an optional local VS Code multi-root workspace. However, for Codex change requests, only the repository to be changed is opened. This reduces accidental changes across repositories.

## 3. Host requirements

### 3.1 Recommended base

- current supported Linux distribution;
- Docker Engine with Compose plugin;
- User access to Docker without repeated `sudo`, only on a trusted development machine;
- VS Code Stable;
- SSH or HTTPS Git access;
- sufficient memory for SDK, NuGet and container caches.

### 3.2 Docker Socket security notice

Test containers require access to a container daemon. A host Docker socket mounted in the dev container grants the container very extensive control over the host daemon.

Variants:

| Variant | Advantage | Disadvantage |
|---|---|---|
| Host Docker Socket | simple, fast, good test container compatibility | high permission on the host |
| Docker-in-Docker | more separate test environment | privileged dev container, more resources |
| dedicated development VM | best practical isolation | additional operating expenses |
| rootless container runtime | lower host risk | more compatibility check |

For a personal, trustworthy development computer, the host socket is pragmatic. For agentic execution, a dedicated development VM or an isolated Docker daemon is a safer option. Codex does not receive blanket uncontrolled network or host access.

## 4. VS Code Extensions

### 4.1 Mandatory on the host

| Extension | ID | Purpose |
|---|---|---|
| Dev Containers | `ms-vscode-remote.remote-containers` | Open repository in defined container |
| Codex | `openai.chatgpt` | focused agentic work and review in the editor |

### 4.2 Recommended in both dev containers

| Extension | ID | Purpose |
|---|---|---|
| C# Dev Kit | `ms-dotnettools.csdevkit` | Solution Explorer, Testing, Debugging |
| C# | `ms-dotnettools.csharp` | C# Language Services, used by Dev Kit |
| Codex | `openai.chatgpt` | Agent works with the open repository context; Remote container behavior is verified in setup smoke test |
| EditorConfig | `EditorConfig.EditorConfig` | consistent formatting |
| markdownlint | `DavidAnson.vscode-markdownlint` | Specification and README quality |
| YAML | `redhat.vscode-yaml` | CI, Compose and k3s manifests |
| GitLens | `eamodio.gitlens` | History and review, optional |

### 4.3 Recommended in Playground container only

| Extension | ID | Purpose |
|---|---|---|
| Containers | `ms-azuretools.vscode-containers` | Container and Compose Artifacts |
| Kubernetes | `ms-kubernetes-tools.vscode-kubernetes-tools` | later k3s manifests and diagnosis |
| REST Client | `humao.rest-client` | versioned HTTP smoke requests, optional |

Further UI-specific extensions will only be added after the frontend framework has been selected. Preemptively installing React, Vue, Svelte and Blazor tools at the same time will only create noise.

### 4.4 License notice

C# Dev Kit is free for individuals and certain community, educational and open source scenarios. When used in organizations, Visual Studio Professional or Enterprise rights may be required. Since license terms depend on the actual context of use, this is checked before a commercial organization setup.

## 5. Shared repository files

Both repositories receive:

```text
.devcontainer/
  devcontainer.json
  Dockerfile
.vscode/
  extensions.json
  settings.json
  tasks.json
  launch.json, when an executable host is available
.codex/
  config.toml, only project-specific, non-secret settings
AGENTS.md
.editorconfig
.gitignore
global.json
Directory.Build.props
Directory.Packages.props
NuGet.Config
README.md
docs/
eng/ or scripts/
```

Secrets, tokens and personal paths are not committed.

## 6. Common Devcontainer base

A small, custom Dockerfile based on the official .NET 10 LTS dev container image is used. The image is pinned to a verified multi-architecture digest for the reproducible F0 baseline; The SDK selection is additionally set to the stable SDK `10.0.400` via `global.json`.

Example:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble@sha256:e1fc6e423f543119c406d24e2e687d67c569f18f04a37a8b0005d80ad0dcee80 AS current-dotnet-sdk

FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble@sha256:0ad11caaa728e35cbc46b0592dd11ca0df8111e565a96471024a795f4d412c7c

COPY --from=current-dotnet-sdk /usr/share/dotnet /usr/share/dotnet

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       git curl jq postgresql-client \
    && rm -rf /var/lib/apt/lists/*

USER vscode
```

The framework container only requires `postgresql-client` from the product provider spike. It may still contain a common base image if it does not create an unnecessarily large or difficult-to-maintain toolchain.

## 7. Framework Devcontainer

### 7.1 Goal

- .NET 10 SDK;
- Git and build tools;
- NuGet Cache;
- Access to local package feed;
- Container daemon for test containers;
- no permanently running application infrastructure.

### 7.2 Example `devcontainer.json`

```jsonc
{
  "name": "DDD.BuildingBlocks",
  "build": {
    "dockerfile": "Dockerfile",
    "context": "."
  },
  "remoteUser": "vscode",
  "mounts": [
    "source=ddd-building-blocks-nuget,target=/home/vscode/.nuget/packages,type=volume",
    "source=ddd-building-blocks-codex,target=/home/vscode/.codex,type=volume",
    "source=${localEnv:HOME}/.local/share/nuget/persona-feed,target=/workspaces/local-nuget-feed,type=bind"
  ],
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csdevkit",
        "ms-dotnettools.csharp",
        "openai.chatgpt",
        "EditorConfig.EditorConfig",
        "DavidAnson.vscode-markdownlint",
        "redhat.vscode-yaml",
        "eamodio.gitlens"
      ]
    }
  },
  "postCreateCommand": "dotnet --info && dotnet restore",
  "containerEnv": {
    "NUGET_PACKAGES": "/home/vscode/.nuget/packages"
  },
  "features": {
    "ghcr.io/devcontainers/features/docker-outside-of-docker:1": {}
  }
}
```

Docker access for test containers is configured as an intentional additional feature after deciding between host socket, DinD or development VM. It is not incorporated into the base template without comment.

## 8. Playground dev container with Compose

### 8.1 Services

```text
workspace
postgres
optional fake inference service, only when needed
```

vLLM on the DGX Spark is not built into the dev container. The endpoint remains an external infrastructure and is addressed via local configuration.

### 8.2 Example `compose.yaml`

```yaml
services:
  workspace:
    build:
      context: .
      dockerfile: Dockerfile
    volumes:
      - ..:/workspaces/persona-simulation-playground:cached
      - playground-nuget:/home/vscode/.nuget/packages
      - ${HOME}/.local/share/nuget/persona-feed:/workspaces/local-nuget-feed
    command: sleep infinity
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:18
    environment:
      POSTGRES_DB: persona_playground
      POSTGRES_USER: persona
      POSTGRES_PASSWORD: local development only
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U persona -d persona_playground"]
      interval: 5s
      timeout: 3s
      retries: 20
    volumes:
      - playground-postgres:/var/lib/postgresql/data

volumes:
  playground-nuget:
  playground-postgres:
```

The specific PostgreSQL major version is confirmed at implementation start and then pinned. The password is only permitted for the isolated local development database.

### 8.3 Example `devcontainer.json`

```jsonc
{
  "name": "Persona Simulation Playground",
  "dockerComposeFile": "compose.yaml",
  "service": "workspace",
  "workspaceFolder": "/workspaces/persona-simulation-playground",
  "shutdownAction": "stopCompose",
  "remoteUser": "vscode",
  "forwardPorts": [5000, 5001],
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csdevkit",
        "ms-dotnettools.csharp",
        "openai.chatgpt",
        "EditorConfig.EditorConfig",
        "DavidAnson.vscode-markdownlint",
        "redhat.vscode-yaml",
        "ms-azuretools.vscode-containers",
        "ms-kubernetes-tools.vscode-kubernetes-tools",
        "humao.rest-client",
        "eamodio.gitlens"
      ]
    }
  },
  "postCreateCommand": "dotnet --info && dotnet restore",
  "containerEnv": {
    "NUGET_PACKAGES": "/home/vscode/.nuget/packages",
    "ConnectionStrings__PostgreSql": "Host=postgres;Port=5432;Database=persona_playground;Username=persona;Password=local-development-only"
  }
}
```

## 9. VS Code Settings

Recommended shared settings:

```jsonc
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll": "explicit",
    "source.organizeImports": "explicit"
  },
  "files.trimTrailingWhitespace": true,
  "files.insertFinalNewline": true,
  "dotnet.defaultSolution": "auto",
  "testExplorer.useNativeTesting": true,
  "chatgpt.openOnStartup": false,
  "chatgpt.commentCodeLensEnabled": true
}
```

Only documented, actually supported settings are committed. Personal theme, font and keyboard settings remain user-specific.

## 10. Tasks

Both repositories receive standardized tasks:

```text
restore
build
test-fast
test-integration
test-all
format-check
pack
clean
```

Playground additionally:

```text
db-start
db-stop
db-reset-development
run-api
run-ui later
e2e-stub
e2e-vllm manuell
```

Destructive tasks such as database reset must be clearly marked as development-only and must not use a freely configurable production connection.

## 11. Debugging

### Framework

- Test debugging via C# Dev Kit;
- Example Host only when needed;
- no artificial executable framework application.

###Playground

- API Host via `dotnet` Launch Profile;
- Environment `Development`;
- PostgreSQL service from Compose;
- vLLM endpoint via user secrets or not committed `.env`;
- UI debug configuration only after technology selection.

## 12. Codex configuration

The official Codex extension is installed under the extension ID `openai.chatgpt`. CLI and IDE extension share their agent configuration. Personal defaults are in `~/.codex/config.toml`, trustworthy project-specific additions are in `.codex/config.toml`.

The official documentation confirms VS Code support, but does not currently describe its own mandatory dev container operating mode. That's why the initial setup practically checks whether the extension in the remote workspace correctly uses all the required repository and terminal functions. If the remote installation does not fully work, the extension remains active as a local VS Code UI and Codex CLI is used in a controlled manner within the container. This fallback decision is only made after the smoke test.

Recommended principles:

- `workspace-write` as normal mode;
- Release when network access or scope is exceeded;
- no secrets in `.codex/config.toml`;
- per repository an `AGENTS.md` with architecture rules, build commands and stop rules;
- Codex only opens and modifies one repository per job;
- Git checkpoint or clean commit before and after each stage;
- Network only for restore or expressly authorized research;
- Treat Docker access as a highly privileged capability.

Framework-`AGENTS.md` emphasizes:

- public API and SemVer;
- Provider Contract Suite;
- no playground logic;
- Stop after each modernization stage.

Playground-`AGENTS.md` emphasizes:

- domain dependency direction;
- Framework only as a package;
- Single user and single active session;
- persist-before-publish;
- no UI technical logic;
- no independent scope extension.

## 13. NuGet and Credentials

- global NuGet cache as a named volume per repository;
- local feed as host bind mount;
- central feeds via `NuGet.Config` without plaintext tokens;
- Access data via credential provider, environment or CI secrets;
- no credentials in the image, compose, repository or devcontainer feature;
- Package source mapping recommended for DDD.BuildingBlocks packages.

## 14. Architecture supporting files

### `.editorconfig`

- C# conventions;
- Namespace and Usings rules;
- Nullable convention;
- line endings LF;
- Do not format Markdown excessively strictly.

### `Directory.Build.props`

- `net10.0` not necessarily global if testing tools differ;
- Nullable;
- deterministic builds;
- Warnings as Errors in CI;
- Analyzer level;
- Repository metadata.

### `Directory.Packages.props`

- central versions;
- Test packages can be identified separately;
- Provider packages only in infrastructure projects;
- no transitive Npgsql dependency in domain/core.

## 15. Setup steps

### Framework

1. Clone repository separately.
2. Add dev container files in your own small stage.
3. Build and open containers.
4. Run F0 baseline unchanged.
5. Mount local NuGet feed.
6. Add pack and consumer smoke test.
7. only then begin modernization.

###Playground

1. create new standalone repository.
2. Add Devcontainer plus PostgreSQL-Compose.
3. Create solution skeleton and architecture tests.
4. configure local feed.
5. Pin shared framework release.
6. Run consumer smoke test.
7. only then begin the first domain stage.

## 16. Acceptance of the development environment

- both repositories open independently in the dev container;
- identical documented build commands work in Terminal and CI;
- Framework can package and publish to local feed;
- Playground can restore package from local feed;
- PostgreSQL integration test running in Playground setup;
- VS Code discovers solution and tests;
- Debugging works;
- Codex only sees the open repository;
- no secrets are committed;
- completely rebuilding the container is reproducible.

## 17. Technical references

- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Dev Container Metadata Reference](https://code.visualstudio.com/docs/devcontainers/devcontainerjson-reference)
- [C# in VS Code](https://code.visualstudio.com/docs/languages/dotnet)
- [C# Dev Kit FAQ](https://code.visualstudio.com/docs/csharp/cs-dev-kit-faq)
- [Codex IDE Extension](https://learn.chatgpt.com/docs/codex/ide)
- [Codex Developer Settings](https://learn.chatgpt.com/docs/developer-settings)
- [Codex Config Basics](https://learn.chatgpt.com/docs/config-file/config-basic)
- [Codex Agent Approvals and Security](https://learn.chatgpt.com/docs/agent-approvals-security)
- [PostgreSQL 18 Documentation](https://www.postgresql.org/docs/current/)
