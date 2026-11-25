FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/WebKoy.Web/WebKoy.Web.csproj src/WebKoy.Web/
RUN dotnet restore src/WebKoy.Web/WebKoy.Web.csproj
COPY src/ src/
RUN dotnet publish src/WebKoy.Web/WebKoy.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    Platform__StorageRoot=/data/sites
VOLUME /data
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "WebKoy.Web.dll"]
