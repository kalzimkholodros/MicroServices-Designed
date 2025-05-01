FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/ProductService/ProductService.csproj", "Services/ProductService/"]
COPY ["Common.Messaging/Common.Messaging.csproj", "Common.Messaging/"]
RUN dotnet restore "Services/ProductService/ProductService.csproj"
COPY . .
WORKDIR "/src/Services/ProductService"
RUN dotnet build "ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"] 