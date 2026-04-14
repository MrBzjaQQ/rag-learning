# ADR 001: Переход на C#/.NET 10 с улучшенным поиском

## Статус
Предложено

## Контекст

### Текущее состояние прототипа

**Backend (Python/FastAPI):**
- Точка входа: `src/main.py`
- База данных: PostgreSQL + pgvector (таблицы `documents`, `embeddings`)
- Индекс: IVFFlat с `vector_cosine_ops` (100 lists)
- Модель эмбеддингов: `nomic-ai/nomic-embed-text-v2-moe-GGUF` (768-dim)
- API эндпоинты:
  - `POST /api/v1/search/documents` — векторный поиск
  - `POST /api/v1/search/rag` — RAG с чанками
  - `POST /api/v1/search/rag-answer` — RAG с полными документами
  - `POST /api/v1/file/upload` — загрузка файлов
  - `POST /api/v1/indexing/documents/{file_id}/index` — индексация
- Поиск: исключительно Cosine Similarity через pgvector

**Frontend (Angular 19+):**
- Single-page приложение с Signals
- Компонент `app.ts` + сервис `rag.service.ts`
- Вызов: `POST /api/v1/search/rag-answer`
- Параметры: `query`, `top_k`, `similarity_threshold`

### Проблемы текущего решения

1. **Только векторный поиск** — при росте датасета семантический поиск теряет точность, особенно для специфичных терминов
2. **IVFFlat индекс** — менее эффективен чем HNSW для больших датасетов
3. **Нет переранжирования** — результаты не фильтруются по релевантности
4. **Нет гибридного поиска** — не используется полнотекстовый поиск для точных совпадений
5. **Нет тестов** — нулевое покрытие

## Предлагаемое решение

### 1. Технологический стек

| Компонент | Текущий | Новый |
|-----------|---------|-------|
| Backend | Python/FastAPI | C#/.NET 10 |
| Database | PostgreSQL + pgvector | PostgreSQL + pgvector (сохранить) |
| Frontend | Angular 19+ | Angular 19+ (перенести как есть) |
| Поиск | Cosine Similarity | Hybrid Search (Dense + Sparse) + Reranking |
| Индекс | IVFFlat | HNSW + GIN |

### 2. Архитектура (Clean Architecture)

```
RAG.solution
├── RAG.Domain           # Сущности, интерфейсы репозиториев
│   ├── Entities/
│   │   ├── Document.cs
│   │   └── Embedding.cs
│   └── Interfaces/
│       ├── IDocumentRepository.cs
│       ├── IEmbeddingRepository.cs
│       └── ISearchService.cs
│
├── RAG.Application      # Бизнес-логика, Use Cases
│   ├── Services/
│   │   ├── HybridSearchService.cs    # RRF объединение
│   │   ├── RerankingService.cs       # Cross-Encoder
│   │   └── RAGOrchestrator.cs        # Полный пайплайн
│   ├── DTOs/
│   └── Mappers/
│
├── RAG.Infrastructure   # Реализация (DB, External APIs)
│   ├── Data/
│   │   ├── PgVectorDbContext.cs
│   │   └── Repositories/
│   ├── Embeddings/
│   │   └── NomicEmbeddingClient.cs
│   └── LLM/
│       └── OpenAIChatClient.cs
│
├── RAG.Api              # REST контроллеры
│   ├── Controllers/
│   │   ├── FilesController.cs
│   │   ├── SearchController.cs       # /api/v1/search/*
│   │   └── IndexingController.cs
│   └── Program.cs
│
└── RAG.Tests            # xUnit + Moq + FluentAssertions
    ├── Unit/
    └── Integration/
```

### 3. Улучшение поиска (Core Improvement)

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Hybrid Search Pipeline                          │
└─────────────────────────────────────────────────────────────────────┘

   Запрос пользователя
          │
          ▼
   ┌─────────────────┐
   │  Query Embedding │  (nomic-ai / nomic-embed-text-v2-moe)
   └─────────────────┘
          │
          ▼
   ┌────────────────────┐      ┌─────────────────────┐
   │  Dense Retrieval   │      │  Sparse Retrieval   │
   │  (pgvector/HNSW)   │      │  (BM25 via GIN)     │
   │                    │      │                     │
   │  Cosine Similarity │      │  ts_rank, ts_headline│
   └────────────────────┘      └─────────────────────┘
          │                            │
          ▼                            ▼
   ┌────────────────────┐      ┌─────────────────────┐
   │  Top-K Dense       │      │  Top-K Sparse       │
   │  (K=50)            │      │  (K=50)             │
   └────────────────────┘      └─────────────────────┘
          │                            │
          └──────────┬─────────────────┘
                     ▼
         ┌─────────────────────┐
         │ Reciprocal Rank     │  RRF = Σ 1/(k + rank_i)
         │ Fusion (RRF)        │  k=60 (standard)
         └─────────────────────┘
                     │
                     ▼
         ┌─────────────────────┐
         │  Unified Top-N      │
         │  (N=20)             │
         └─────────────────────┘
                     │
                     ▼
         ┌─────────────────────┐
         │  Cross-Encoder      │  # Переранжирование
         │  Reranking          │  #miniLM или аналог
         └─────────────────────┘
                     │
                     ▼
         ┌─────────────────────┐
         │  Final Top-K        │  # K=5 (параметр пользователя)
         └─────────────────────┘
                     │
                     ▼
         ┌─────────────────────┐
         │  Context + LLM      │
         │  Generation         │
         └─────────────────────┘
```

### 4. API Endpoints (сохранить как в прототипе)

| Method | Path | Описание |
|--------|------|----------|
| POST | `/api/v1/search/documents` | Векторный поиск |
| POST | `/api/v1/search/rag` | RAG с чанками |
| POST | `/api/v1/search/rag-answer` | RAG с полными документами |
| POST | `/api/v1/file/upload` | Загрузка файлов |
| POST | `/api/v1/file/upload-zip` | Загрузка ZIP |
| GET | `/api/v1/file/{fileId}` | Метаданные файла |
| GET | `/api/v1/file/{fileId}/download` | Скачать файл |
| DELETE | `/api/v1/file/{fileId}` | Удалить файл |
| POST | `/api/v1/indexing/documents/{file_id}/index` | Индексировать документ |
| POST | `/api/v1/indexing/all` | Индексировать все |

### 5. Схема БД (расширенная)

```sql
-- Таблицы сохраняем как в прототипе, добавляем индексы:

-- HNSW индекс для векторов (вместо IVFFlat)
CREATE INDEX embeddings_hnsw_idx ON embeddings 
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

-- GIN индекс для полнотекстового поиска (Sparse Retrieval)
CREATE INDEX embeddings_tsvector_idx ON embeddings 
USING GIN (to_tsvector('russian', text));

-- Покрывающий индекс для часто используемых запросов
CREATE INDEX idx_document_id_chunk ON embeddings(document_id, chunk_index);
```

### 6. Тестирование

**Unit Tests:**
- `HybridSearchServiceTests` — тест RRF алгоритма
- `RerankingServiceTests` — тест переранжирования
- `RAGOrchestratorTests` — тест полного пайплайна
- `EmbeddingRepositoryTests` — тест CRUD операций

**Integration Tests:**
- PostgreSQL + pgvector интеграция
- API endpoint тесты (те же что и в прототипе)

**Evaluation:**
- Golden Dataset с 50-100 вопросами
- Метрики: Recall@K, MRR, NDCG

## Последствия

### Положительные
- Значительное улучшение качества поиска (Hybrid + Reranking)
- HNSW индекс обеспечивает лучшую производительность при масштабировании
- Clean Architecture обеспечивает maintainability
- Покрытие тестами гарантирует стабильность

### Отрицательные
- Увеличение latency из-за двухэтапного поиска (компенсируется малым Top-K)
- Необходимость миграции Python → C#
- Дополнительная настройка GIN индексов

## Критерии успеха

1. MRR@5 >= 0.8 на Golden Dataset (vs 0.5 в прототипе)
2. Latency поиска < 500ms для 100k документов
3. Покрытие тестами > 70%
4. API backward compatibility с прототипом