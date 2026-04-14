# Декомпозиция задач: Переход на C#/.NET 10 RAG

## Этап 1: Инфраструктура решения

### TASK-1: Настройка .NET 10 решения
- [ ] Создать Solution `RAG.sln`
- [ ] Создать проекты:
  - `RAG.Domain` — Class Library (сущности, интерфейсы)
  - `RAG.Application` — Class Library (бизнес-логика)
  - `RAG.Infrastructure` — Class Library (DB, External APIs)
  - `RAG.Api` — ASP.NET Core Web API
  - `RAG.Tests` — xUnit тестовый проект
- [ ] Настроить Clean Architecture зависимости
- [ ] Настроить CI/CD (опционально)

---

## Этап 2: Domain слой

### TASK-2: Определение сущностей
- [ ] Создать `Document.cs` (id, filename, fileType, fileSize, contentPath, isIndexed, даты)
- [ ] Создать `Embedding.cs` (id, text, metadata, vector[768], documentId, chunkIndex)
- [ ] Создать enum `SearchMethod` (Vector, BM25, Hybrid)
- [ ] Настроить Npgsql и pgvector пакеты

---

## Этап 3: Infrastructure слой

### TASK-3: PostgreSQL + pgvector контекст
- [ ] Создать `RagDbContext` : DbContext
- [ ] Настроить pgvector extension при миграции
- [ ] Добавить HNSW индекс для векторов
- [ ] Добавить GIN индекс для полнотекстового поиска (TSVector)
- [ ] Настроить connection string в appsettings.json

### TASK-4: Repository реализации
- [ ] `DocumentRepository` : IDocumentRepository
- [ ] `EmbeddingRepository` : IEmbeddingRepository
- [ ] Реализовать CRUD операции
- [ ] Добавить методы для векторного поиска (cosine_distance)

### TASK-5: Клиенты для внешних сервисов
- [ ] `NomicEmbeddingClient` — генерация эмбеддингов (HTTP к nomic-ai)
- [ ] `OpenAIChatClient` — LLM генерация (HTTP к OpenAI-подобному API)
- [ ] Вынести base URLs и API keys в конфигурацию

---

## Этап 4: Application слой (Search Logic)

### TASK-6: Dense Retrieval (Vector Search)
- [ ] Реализовать метод `SearchByVector(query, topK)`
- [ ] Использовать pgvector cosine_distance
- [ ] Вернуть Top-K результатов с score

### TASK-7: Sparse Retrieval (BM25 / Full-Text)
- [ ] Реализовать метод `SearchByKeywords(query, topK)`
- [ ] Использовать PostgreSQL ts_rank, ts_headline
- [ ] Поддержку русского языка (to_tsvector('russian', ...))

### TASK-8: Hybrid Search (RRF)
- [ ] Реализовать `HybridSearchService`
- [ ] Объединить результаты Dense + Sparse через Reciprocal Rank Fusion
- [ ] Формула: RRF(d) = Σ 1/(k + rank(d)), k=60
- [ ] Параметризируемое количество результатов на каждый поиск

### TASK-9: Reranking (Cross-Encoder)
- [ ] Реализовать `RerankingService`
- [ ] Интеграция с Cross-Encoder моделью (HTTP или локальная)
- [ ] Переранжирование Top-N кандидатов
- [ ] Возврат финального Top-K

### TASK-10: RAG Orchestrator
- [ ] Создать `RAGOrchestrator` — полный пайплайн
- [ ] Query -> Hybrid Search -> Rerank -> Context -> LLM
- [ ] Поддержка режима с чанками и с полными документами
- [ ] Сохранение backward compatibility с прототипом

---

## Этап 5: API слой

### TASK-11: Контроллеры (сохранить API прототипа)
- [ ] `FilesController` — /api/v1/file/*
  - POST upload, upload-zip
  - GET /{fileId}, /{fileId}/download
  - DELETE /{fileId}
- [ ] `SearchController` — /api/v1/search/*
  - POST /documents (vector search)
  - POST /rag (RAG with chunks)
  - POST /rag-answer (RAG with full docs)
- [ ] `IndexingController` — /api/v1/indexing/*
  - POST /documents/{file_id}/index
  - POST /all

### TASK-12: Middleware и конфигурация
- [ ] Настроить CORS
- [ ] Настроить Swagger/OpenAPI
- [ ] Health check endpoint
- [ ] Логирование (Serilog)

---

## Этап 6: Тестирование

### TASK-13: Unit тесты
- [ ] `HybridSearchServiceTests` — тестирование RRF
  - Тест объединения результатов
  - Тест сортировки по rank
- [ ] `RerankingServiceTests`
  - Тест переранжирования списка
- [ ] `RAGOrchestratorTests`
  - Тест полного пайплайна (mock всех зависимостей)
- [ ] `EmbeddingRepositoryTests`
  - Тест сохранения и поиска векторов

### TASK-14: Integration тесты
- [ ] Настроить TestContainers для PostgreSQL
- [ ] `SearchIntegrationTests` — реальные запросы к БД
- [ ] `ApiIntegrationTests` — HTTP тесты контроллеров

### TASK-15: Evaluation (Golden Dataset)
- [ ] Создать JSON с 50-100 тестовыми вопросами
- [ ] Добавить expected relevant documents для каждого
- [ ] Написать скрипт оценки: MRR@K, Recall@K
- [ ] Запустить на прототипе и на новом решении, сравнить

---

## Этап 7: Frontend Integration

### TASK-16: Перенос Angular UI
- [ ] Скопировать `rag-ui` как есть из прототипа
- [ ] Обновить base URL в environment
- [ ] Проверить работу с новым .NET API
- [ ] Проверить все сценарии: upload, search, download

---

## Зависимости между задачами

```
TASK-1 ──┬──► TASK-2 ──► TASK-3 ──► TASK-4 ──► TASK-5
         │                              │
         │                              ▼
         │                         TASK-6 ──► TASK-7 ──► TASK-8 ──► TASK-9 ──► TASK-10
         │                                                                         │
         └──────────► TASK-11 ◄────────────────────────────────────────────────────┘
                                                                         │
                                                         TASK-12 ──► TASK-13 ◄──► TASK-14
                                                                              │
                                                                              ▼
                                                                         TASK-15
                                                                              │
                                                         TASK-16 ◄───────────┘
```

## Приоритеты

| Priority | Tasks | Обоснование |
|----------|-------|-------------|
| High | TASK-1, TASK-2, TASK-3, TASK-6 | Базовый фундамент |
| High | TASK-8, TASK-9, TASK-10 | Core improvement (улучшение поиска) |
| High | TASK-11 | API compatibility |
| Medium | TASK-4, TASK-5 | Infrastructure |
| Medium | TASK-12 | Production readiness |
| Medium | TASK-13, TASK-14 | Тестирование |
| Low | TASK-15 | Evaluation |
| Low | TASK-16 | Frontend (UI уже готов) |