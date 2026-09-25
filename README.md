# TestJob API

REST API для обработки HTML-страниц и извлечения данных из JSON-запроса.

## Требования

- .NET 10 SDK
- Docker Desktop / Docker Engine
- PostgreSQL (поднимается через Docker Compose)

## Локальный запуск

1. Запустите PostgreSQL:

```bash
docker compose up -d postgres
```

2. Запустите API локально:

```bash
cd /workspaces/codespaces-blank
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=appdb;Username=appuser;Password=appsecret' \
ASPNETCORE_URLS='http://localhost:5077' \
dotnet run --project src/TestJob.Api/TestJob.Api.csproj --urls http://localhost:5077
```

3. Откройте Swagger UI в браузере:

```text
http://localhost:5077/api/swagger
```

4. Для проверки API можно отправить POST-запрос:

```bash
curl -sS -X POST http://localhost:5077/api/process \
  -H 'Content-Type: application/json' \
  --data @json_payload_1.txt
```

## Docker Compose

Для запуска всей стека (API + PostgreSQL + pgAdmin):

```bash
cd /workspaces/codespaces-blank
docker compose up --build -d
```

После запуска доступны следующие адреса:

- API: http://localhost:8090/api/process
- Swagger: http://localhost:8090/api/swagger
- pgAdmin: http://localhost:8080
- PostgreSQL: localhost:5432

## Данные для входа в pgAdmin

```text
Email: admin@admin.com
Password: admin
```

## Важное замечание

- Адрес `0.0.0.0` используется только для привязки внутри контейнера и не является URL для браузера.
- Для браузера всегда используйте `localhost`.
- В этом окружении Codespaces локальный запуск через `localhost:5077` является наиболее стабильным вариантом проверки.

## Полезные файлы

- `json_payload_1.txt` — пример входного JSON
- `json_payload_2.txt` — второй пример входного JSON
- `json_result_1.txt` — ожидаемый результат для первого запроса
- `json_result_2.txt` — ожидаемый результат для второго запроса
