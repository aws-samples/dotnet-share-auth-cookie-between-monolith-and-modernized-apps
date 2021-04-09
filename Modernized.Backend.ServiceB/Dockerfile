#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Modernized.Backend.ServiceB/Modernized.Backend.ServiceB.csproj", "Modernized.Backend.ServiceB/"]
RUN dotnet restore "Modernized.Backend.ServiceB/Modernized.Backend.ServiceB.csproj"
COPY . .
WORKDIR "/src/Modernized.Backend.ServiceB"
RUN dotnet build "Modernized.Backend.ServiceB.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Modernized.Backend.ServiceB.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Modernized.Backend.ServiceB.dll"]