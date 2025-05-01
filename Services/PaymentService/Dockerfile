FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/PaymentService/PaymentService.csproj", "Services/PaymentService/"]
COPY ["Common.Messaging/Common.Messaging.csproj", "Common.Messaging/"]
RUN dotnet restore "Services/PaymentService/PaymentService.csproj"
COPY . .
WORKDIR "/src/Services/PaymentService"
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"] 