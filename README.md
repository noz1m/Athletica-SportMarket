# Athletica — спортивный онлайн-магазин · .NET 10 + React / TypeScript

Единый проект интернет-витрины: каталог, избранное, статьи, баннеры, FAQ и панель управления.

## Локально на macOS

Требования: .NET SDK 10, PostgreSQL, Node.js 22+, npm.

### Backend

Из корня репозитория:

```bash
cd Backend
dotnet user-secrets init --project WebApi
dotnet user-secrets set 'Jwt:Key' "$(openssl rand -base64 48)" --project WebApi
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Host=localhost;Port=5432;Database=kamilkarateV2;Username=postgres;Password=YOUR_PASSWORD' --project WebApi
dotnet user-secrets set 'BootstrapAdmin:Email' 'admin@example.com' --project WebApi
dotnet user-secrets set 'BootstrapAdmin:Password' 'YOUR_STRONG_ADMIN_PASSWORD' --project WebApi
dotnet restore ClaudeProj.sln
dotnet build ClaudeProj.sln
dotnet run --project WebApi --launch-profile http
```

Создайте БД `kamilkarateV2` и подставьте пароль PostgreSQL. Первый старт применяет существующие миграции и создаёт администратора только если задана конфигурация `BootstrapAdmin`.

API: http://localhost:5218, Swagger: http://localhost:5218/swagger

### Frontend

В другом терминале:

```bash
cd Frontend
npm ci
npm run build
npm run dev
```

UI: http://localhost:5173. Vite проксирует `/api` и `/uploads` на Backend `http://localhost:5218`. В production на nginx использован аналогичный прокси. Не настраивайте секреты JWT или PostgreSQL во Frontend.

## Docker Compose

```bash
cp .env.example .env
# Измените POSTGRES_PASSWORD, JWT_KEY, BOOTSTRAP_ADMIN_PASSWORD и SITE_URL.
# JWT_KEY сгенерируйте командой: openssl rand -base64 48
docker compose up --build
```

По умолчанию Frontend: http://localhost:8080, Backend: http://localhost:5218.

## Проверка

Backend: `dotnet build Backend/ClaudeProj.sln`.
Frontend: `cd Frontend && npm ci && npm run build`.
Сервисные зависимости и СУБД должны быть доступны для интеграционных тестов. Если уже есть действующая БД, сделайте резервную копию перед применением миграций.

## Безопасность

Администратор больше не создаётся с известным паролем. Параметры конфигурации передаются через User Secrets или `.env` (не коммитьте его). JWT-ключ должен быть минимум 32 UTF-8 байта.


## Бренд и данные существующей базы

Публичный бренд теперь **Athletica**. Исходные `Jwt:Issuer`, `Jwt:Audience`, имя PostgreSQL-БД и EF-миграции намеренно **не переименованы**: это внутренние идентификаторы, и их изменение без миграционного плана может сломать рабочую установку.

При запуске сидер автоматически меняет `site_name` с `KamilKarate` на `Athletica`, только если имя осталось заводским. Если вы уже редактировали название через админ-панель, оно сохранится: задайте `Athletica` в настройках сайта вручную. Старые баннеры, статьи, названия и категории из БД тоже не перезаписываются — обновите их через админ-панель.

В текущем API реализован каталог с избранным и переходами на маркетплейсы, **но полноценная корзина, оплата и доставка не добавлены**: для этого потребуется отдельный модуль заказов.

Для сохранения настроек рабочего Mac создайте новую папку проекта, но используйте прежнюю PostgreSQL-базу и свои User Secrets; `.env` и файлы с паролями в ZIP не включаются.
