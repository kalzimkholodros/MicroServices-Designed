FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/BasketService/BasketService.csproj", "Services/BasketService/"]
COPY ["Common.Messaging/Common.Messaging.csproj", "Common.Messaging/"]
RUN dotnet restore "Services/BasketService/BasketService.csproj"
COPY . .
WORKDIR "/src/Services/BasketService"
RUN dotnet build "BasketService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BasketService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BasketService.dll"] 