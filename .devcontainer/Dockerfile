FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble@sha256:e1fc6e423f543119c406d24e2e687d67c569f18f04a37a8b0005d80ad0dcee80 AS current-dotnet-sdk

FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble@sha256:0ad11caaa728e35cbc46b0592dd11ca0df8111e565a96471024a795f4d412c7c

COPY --from=current-dotnet-sdk /usr/share/dotnet /usr/share/dotnet

USER root
RUN rm -f /etc/apt/sources.list.d/yarn.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        git \
        curl \
        jq \
        python3-pip \
        python3-venv \
        postgresql-client \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/vscode/.nuget/NuGet

USER vscode
