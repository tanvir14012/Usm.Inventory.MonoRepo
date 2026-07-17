FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "Gateway/ApiGateway/ApiGateway.csproj"
RUN dotnet publish "Gateway/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# ca-certificates: allows update-ca-certificates to install the USM Root CA
# icu-libs: required for full .NET globalization support on Alpine
RUN apk add --no-cache ca-certificates icu-libs

COPY scripts/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "ApiGateway.dll"]
