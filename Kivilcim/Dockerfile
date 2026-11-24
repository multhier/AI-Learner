# Kıvılcım: ASP.NET Core 8 MVC
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Kivilcim.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Yüklenen görsel/videolar ve oturum anahtarları kalıcı klasörlerde tutulur.
RUN mkdir -p /app/wwwroot/uploads /app/keys && chown -R app:app /app/wwwroot/uploads /app/keys
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    DataProtection__KeysPath=/app/keys
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kivilcim.dll"]
