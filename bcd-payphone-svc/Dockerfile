#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["bcd-payphone-svc/BCD.Payphone.Api.csproj", "bcd-payphone-svc/"]
COPY ["bcd-payphone-lib/BCD.Payphone.Lib.csproj", "bcd-payphone-lib/"]
COPY ["bcd-payphone-logic/BCD.Payphone.Logic.csproj", "bcd-payphone-logic/"]
COPY ["bcd-payphone-data/BCD.Payphone.Data.csproj", "bcd-payphone-data/"]
RUN dotnet restore "bcd-payphone-svc/BCD.Payphone.Api.csproj"
COPY . .
WORKDIR "/src/bcd-payphone-svc"
RUN dotnet build "BCD.Payphone.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BCD.Payphone.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BCD.Payphone.Api.dll"]
