# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first for caching
COPY Wellmeet/Wellmeet.csproj Wellmeet/
RUN dotnet restore "Wellmeet/Wellmeet.csproj"

# Copy the rest
COPY . .

# Publish
RUN dotnet publish "Wellmeet/Wellmeet.csproj" -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080

ENTRYPOINT ["dotnet", "Wellmeet.dll"]
