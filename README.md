# Wellmeet API

Backend API for the Wellmeet application.

## Overview
WellMeet is a full-stack app where users authenticate with JWT, explore activities by category or location, create their own, and join or leave activities created by others. Backend uses full-layered architecture (Controllers, Services, Repositories, Domain) with authorization (Users/Admin), validation, and structured logging for maintainability. 
## Tech Stack
- **Runtime:** .NET 8 (ASP.NET Core)
- **ORM:** Entity Framework Core
- **Databases:**
  - Microsoft SQL Server (local development)
  - PostgreSQL (production – Supabase)
- **Hosting (example):** Render for API hosting
- **Docs:** Swagger / OpenAPI
- **Logging:** Serilog

> Note: The frontend lives in a separate repository.

## Prerequisites
- **.NET 8 SDK**
- **M SQL Server (SSMS)** or **PostgreSQL / Supabase**
- **Database tool** (optional): SSMS, pgAdmin, or any SQL client
- **Visual Studio 2022+** 

## Local Development
1. Configure required settings (either in `Wellmeet/appsettings.json` or via environment variables):
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__SecretKey`
   - `Jwt__Issuer`
   - `Jwt__Audience`
   - (Optional) `Swagger__Enable=true`
   - (Optional) `AdminPassword` for admin seeding
2. Run the API:
   ```bash
   dotnet run --project Wellmeet
   ```
3. Visit Swagger at `/swagger` (enabled in Development and can be enabled elsewhere via config).

## Database Setup

This project went through two database phases during development.

### Local Development (Microsoft SQL Server)
- The DbContext and EF Core migrations were created using a **code-first** approach with Microsoft SQL Server (SSMS).

### Production / Remote Database (PostgreSQL – Supabase)
- The production database is hosted on Supabase (PostgreSQL).
- The db schema was created by executing SQL scripts directly.
- The final PostgreSQL schema is available in: `Wellmeet/supabase_schema.sql`

## Deployment Options

### Option A: Render (recommended)
1. Create a **Web Service** on Render and connect this repository.
2. Build command:
   ```bash
   dotnet publish Wellmeet -c Release -o out
   ```
3. Start command:
   ```bash
   dotnet out/Wellmeet.dll
   ```
4. Set environment variables (example names below; use your real values in Render):
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__SecretKey`
   - `Jwt__Issuer`
   - `Jwt__Audience`
   - (Optional) `Swagger__Enable=true`
   - (Optional) `AdminPassword` for admin seeding


### Option B: Docker
Using the `Dockerfile` in this repo to package the API into a container image, then running that image with the required environment variables.
1. Build the container:
   ```bash
   docker build -t wellmeet-api .
   ```
2. Run the container:
   ```bash
   docker run -p 8080:8080 \
     -e ConnectionStrings__DefaultConnection="..." \
     -e Jwt__SecretKey="..." \
     -e Jwt__Issuer="..." \
     -e Jwt__Audience="..." \
     -e Swagger__Enable=true \
     -e AdminPassword="..." \
     wellmeet-api
   ```

## Configuration
Configuration supports environment variables using double-underscore notation:
```
ConnectionStrings__DefaultConnection
Jwt__SecretKey
Jwt__Issuer
Jwt__Audience
Swagger__Enable
AdminPassword
```
