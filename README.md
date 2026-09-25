## Запуск

1. Запуcк PostgreSQL:

docker compose up -d postgres

2. Запуск API:

```bash
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=appdb;Username=appuser;Password=appsecret' \
ASPNETCORE_URLS='http://localhost:5077' \
dotnet run --project src/TestJob.Api/TestJob.Api.csproj --urls http://localhost:5077
```

3. Swagger UI:

```text
http://localhost:5077/api/swagger
```

## Docker Compose

Для запуска стека (API + PostgreSQL + pgAdmin):

```bash
cd /workspaces/codespaces-blank
docker compose up --build -d
```

Адреса:

- API: http://localhost:8090/api/process
- Swagger: http://localhost:8090/api/swagger
- pgAdmin: http://localhost:8080
- PostgreSQL: localhost:5432

## Данные для входа в pgAdmin

```text
Email: admin@admin.com
Password: admin
```
