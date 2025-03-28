# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copia os arquivos de projeto
COPY ["AppFinancas.Web/AppFinancas.Web.csproj", "AppFinancas.Web/"]
COPY ["AppFinancas.Shared/AppFinancas.Shared.csproj", "AppFinancas.Shared/"]

# Restaura as dependências
RUN dotnet restore "AppFinancas.Web/AppFinancas.Web.csproj" \
    --runtime linux-musl-x64

# Copia o código-fonte
COPY . .

# Publica com otimizações mais agressivas
RUN dotnet publish "AppFinancas.Web/AppFinancas.Web.csproj" \
    --configuration Release \
    --runtime linux-musl-x64 \
    --self-contained true \
    --output /app/publish \
    /p:PublishReadyToRun=true \
    /p:InvariantGlobalization=true \
    /p:DebugType=None \
    /p:DebugSymbols=false \
    /p:EnableCompressionInSingleFile=true \
    /p:StripSymbols=true

# Etapa final com Alpine mais enxuto
FROM alpine:3.19 AS final
WORKDIR /app

# Instala apenas o mínimo de pacotes necessários
RUN apk --no-cache add libstdc++ icu-libs ca-certificates && \
    rm -rf /var/cache/apk/*

# Copia os arquivos publicados
COPY --from=build /app/publish .

# Cria pasta para as chaves de proteção de dados
RUN mkdir -p /root/.aspnet/DataProtection-Keys && \
    chmod 700 /root/.aspnet/DataProtection-Keys

# Remove todos os arquivos desnecessários
RUN find . -name "*.pdb" -type f -delete && \
    find . -name "*.xml" -type f -not -path "*/wwwroot/*" -delete && \
    find . -name "*.config" -type f -not -path "*/web.config" -delete && \
    find ./wwwroot -name "*.map" -type f -delete

# Configura a aplicação
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production
ENV COMPlus_TC_QuickJitForLoops=1
ENV COMPlus_ReadyToRun=1

EXPOSE 8080

# Define o comando de inicialização
ENTRYPOINT ["./AppFinancas.Web"]