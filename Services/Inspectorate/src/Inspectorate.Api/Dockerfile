FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "Services/Inspectorate/src/Inspectorate.Api/Inspectorate.Api.csproj"
RUN dotnet publish "Services/Inspectorate/src/Inspectorate.Api/Inspectorate.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Inspectorate.Api.dll"]
