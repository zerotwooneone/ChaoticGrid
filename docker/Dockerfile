# syntax=docker/dockerfile:1

# ---- Build Angular ----
FROM node:20-alpine AS client-build
WORKDIR /src

COPY src/chaotic-grid-client/package*.json ./src/chaotic-grid-client/
RUN cd src/chaotic-grid-client && npm ci

COPY src/chaotic-grid-client ./src/chaotic-grid-client
RUN cd src/chaotic-grid-client && npm run build

# ---- Build .NET ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /src

COPY ChaoticGrid.sln ./
COPY src/ChaoticGrid.Server/ ./src/ChaoticGrid.Server/

RUN dotnet restore ./src/ChaoticGrid.Server/ChaoticGrid.Server.csproj
RUN dotnet publish ./src/ChaoticGrid.Server/ChaoticGrid.Server.csproj -c Release -o /app/publish

# Copy Angular dist into wwwroot
COPY --from=client-build /src/src/chaotic-grid-client/dist/chaotic-grid-client /app/publish/wwwroot

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=server-build /app/publish ./

ENTRYPOINT ["dotnet", "ChaoticGrid.Server.dll"]
