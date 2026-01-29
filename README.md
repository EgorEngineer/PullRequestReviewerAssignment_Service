# PR Reviewer Assignment Service

Микросервис для автоматического назначения ревьюверов на Pull Request'ы.

## Быстрый старт

```bash
git clone https://github.com/EgorEngineer/PullRequestReviewerAssignment_Service.git
cd reviewer-assignment
docker-compose up --build
```

Сервис доступен на `http://localhost:8080`.

## API Endpoints

| Метод | Endpoint | Описание |
|-------|----------|----------|
| POST | `/team/add` | Создать команду с участниками |
| GET | `/team/get?team_name={name}` | Получить команду |
| POST | `/users/setIsActive` | Установить флаг активности |
| GET | `/users/getReview?user_id={id}` | Получить PR'ы ревьювера |
| POST | `/pullRequest/create` | Создать PR |
| POST | `/pullRequest/merge` | Смержить PR |
| POST | `/pullRequest/reassign` | Переназначить ревьювера |
| GET | `/health` | Health check |

## Технологии

- .NET 8.0 / ASP.NET Core
- PostgreSQL 16
- Entity Framework Core 8
- Docker / Docker Compose
- Serilog - структурированное логирование


## Принятые решения

1. **TeamName как PK** - имя команды уникально
2. **Назначение ревьюверов** - до 2 активных из команды автора, исключая автора
3. **Переназначение** - новый ревьювер из команды заменяемого
4. **Идемпотентность merge** - повторный вызов возвращает 200 OK


## SLI

| Метрика | Требование |
|---------|------------|
| Время ответа | p95 < 300ms |
| Успешность | 99.9% |
| RPS | 5 |

## Логирование

Приложение использует **Serilog** для структурированного логирования.

**Логи пишутся в:**
- Console (stdout) - для Docker
- Файлы в `logs/` - ротация каждый день, хранятся 7 дней

**Уровни логов:**
- `Information` - основные события (создание PR, merge и т.д.)
- `Warning` - предупреждения (команда уже существует, пользователь не найден)
- `Error` - ошибки приложения
- `Debug` - детальная информация (только в Development)

**Просмотр логов:**
```bash
# Логи Docker контейнера
docker-compose logs -f api

# Логи в файлах
tail -f logs/reviewer-assignment-*.log
```
