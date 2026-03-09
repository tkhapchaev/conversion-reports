# Conversion Reports

Сервис принимает асинхронные запросы на построение отчетов по конверсии просмотров в оплаты и уменьшает стоимость отчетов за счет батчинга

## Основная идея

-	сообщения приходят через Kafka
-	запросы одного пользователя за рабочий день собираются в один батч
-	одинаковые отчеты внутри батча считаются один раз
-	результат отдается через HTTP и gRPC
-	частый поллинг читается из кэша, а не из базы данных

## Стек

-	.NET 8
-	ASP.NET Core
-	gRPC, HTTP
-	Kafka
-	EF Core, PostgreSQL
-	Docker
-	xUnit, SQLite для модульных тестов

## Запуск

```bash
docker compose up --build --detach
```

API:
-	http://localhost:8080
-	http://localhost:8080/swagger
-	http://localhost:8080/health

Отправка тестового сообщения (со значениями по умолчанию):
```bash
dotnet run --project ./Source/ConversionReports.Publisher
```

Или с аргументами:
```bash
dotnet run --project Source/ConversionReports.Publisher -- "5ecda0f5-b339-42ee-96d6-7aac13f1d828" "user-1" "100" "200" "2026-03-01T00:00:00Z" "2026-03-08T00:00:00Z"
```

Или же через Kafka UI:
```bash
{
	"requestId": "5ecda0f5-b339-42ee-96d6-7aac13f1d828",
	"messageId": "7eac8ede1ad3430d98ce08605c3b2afd",
	"userId": "user-1",
	"periodStartUtc": "2026-03-02T14:22:58.0167792+00:00",
	"periodEndUtc": "2026-03-09T14:22:58.0168246+00:00",
	"productId": 100,
	"designId": 200,
	"requestedAtUtc": "2026-03-09T14:22:58.0168246+00:00"
}
```

Результаты доступны по:
```bash
http://localhost:8080/api/report-requests/{requestId}
```

Запуск тестов:
```bash
dotnet test
```

## Local services

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Kafka UI: `http://localhost:8081`
