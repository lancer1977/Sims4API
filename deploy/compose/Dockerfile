FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Sims4.SignalR/PolyhydraGames.Sims4.Bridge.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5230
EXPOSE 5230
ENTRYPOINT ["dotnet", "PolyhydraGames.Sims4.Bridge.dll"]
