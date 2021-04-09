#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Modernized.Backend.ServiceA/Modernized.Backend.ServiceA.csproj", "Modernized.Backend.ServiceA/"]
RUN dotnet restore "Modernized.Backend.ServiceA/Modernized.Backend.ServiceA.csproj"
COPY . .
WORKDIR "/src/Modernized.Backend.ServiceA"
RUN dotnet build "Modernized.Backend.ServiceA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Modernized.Backend.ServiceA.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Modernized.Backend.ServiceA.dll"]