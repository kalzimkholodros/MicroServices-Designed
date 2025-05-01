FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ApiGateways/Aggregator/Aggregator.csproj", "ApiGateways/Aggregator/"]
RUN dotnet restore "ApiGateways/Aggregator/Aggregator.csproj"
COPY . .
WORKDIR "/src/ApiGateways/Aggregator"
RUN dotnet build "Aggregator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Aggregator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aggregator.dll"] 