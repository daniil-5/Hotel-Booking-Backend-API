# BookingSystem — система бронирования отелей

Учебный проект курса «Базы данных» (БГУИР, кафедра информатики): REST API на
ASP.NET Core 8 + PostgreSQL 15 с минималистичным веб-интерфейсом, кэшированием
Redis, журналированием MongoDB и серверной логикой в СУБД (триггеры и хранимые
процедуры).

## Быстрый старт (Docker)

```bash
docker compose up -d          # postgres (с инициализацией БД) + redis + mongo + api
# UI:      http://localhost:5044
# Swagger: http://localhost:5044/swagger
```

При первом старте контейнер PostgreSQL выполняет `db/01_schema.sql` (схема,
триггеры, процедуры) и `db/02_seed.sql` (демо-данные).

## Быстрый старт (локально, без контейнера для API)

```bash
docker compose up -d postgres redis mongo   # только инфраструктура
cd BookingSystem.API
dotnet run --project BookingSystem.API --no-launch-profile --urls http://localhost:5044
```

## Демо-учётные записи

| Роль          | Логин                 | Пароль       |
|---------------|-----------------------|--------------|
| Администратор | admin@booking.local   | `Admin123!`  |
| Менеджер      | manager1@booking.local| `Manager123!`|
| Гость         | user1@booking.local   | `User123!`   |

## Демо-данные

40 отелей в 15 городах, 160 типов номеров (Стандарт/Бизнес/Люкс/Семейный),
календарь сезонных цен на 60 дней, ~160 бронирований со всеми статусами,
10 пользователей, журнал аудита.

## Структура

```
db/01_schema.sql        схема: таблицы, ограничения, EXCLUDE по датам,
                        триггеры, хранимые процедуры, представления
db/02_seed.sql          генерация демо-данных (generate_series)
docker-compose.yml      postgres + redis + mongo + api (healthchecks)
BookingSystem.API/      решение .NET (Clean Architecture, 4 проекта + тесты)
  BookingSystem.API/wwwroot/   минималистичный SPA (vanilla JS, без сборки)
.github/workflows/ci.yml  CI: сборка → юнит → интеграционные (PostgreSQL) → docker
```

## Тестирование (пирамида)

| Уровень        | Проект                              | Число | Команда |
|----------------|-------------------------------------|-------|---------|
| Юнит           | `BookingSystem.Tests`               | 17    | `dotnet test BookingSystem.Tests` |
| Интеграционные | `BookingSystem.IntegrationTests`    | 15    | `dotnet test BookingSystem.IntegrationTests` |
| E2E            | браузерный сценарий UI              | —     | каталог → поиск → отель → бронь → отмена → админ-панель |

Интеграционные тесты поднимают отдельную базу `booking_tests` (соединение
задаётся переменной окружения `BOOKING_TEST_CONNECTION`, по умолчанию
`localhost:5433`), применяют схему и проверяют ограничения целостности,
триггеры, процедуры и функции; каждый тест изолирован транзакцией.

## Ключевые решения

- **Запрет двойного бронирования на уровне СУБД**: исключающее ограничение
  `no_overlap_active_bookings` (btree_gist, пересечение диапазонов дат) —
  активные брони (статусы 1–3) одного типа номера не пересекаются; конфликт
  отдаётся как HTTP 409.
- **Сезонное ценообразование**: таблица `room_pricings` (уникальность «тип
  номера + дата»); функция `calc_booking_price` подставляет базовую цену на
  даты без специальной.
- **Аудит**: триггер `audit_booking_change` пишет события в
  `user_action_audit`; приложение дублирует значимые события в MongoDB.
- **Кэширование**: Redis + кэширующие декораторы сервисов, инвалидация по
  префиксам (включая `booking_details:` и `user_booking_history:`).
- **Частичные индексы** `WHERE is_deleted = FALSE` для поиска и авторизации.

## Схема базы

8 таблиц: `role`, `users`, `hotels`, `room_types`, `room_pricings`,
`hotel_photos`, `bookings`, `user_action_audit`; enum-тип `user_action_type`;
представления `v_hotel_statistics`, `v_active_bookings`. Статусы брони:
1 — ожидает, 2 — активно, 3 — подтверждено, 4 — отменено, 5 — завершено.
Роли: 1 — гость, 2 — менеджер, 3 — администратор.

## CI

`.github/workflows/ci.yml`: сборка и юнит-тесты → интеграционные тесты на
service-контейнере PostgreSQL 15 → сборка docker-образа и валидация compose.
