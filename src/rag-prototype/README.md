# RAG Prototype

A Retrieval-Augmented Generation (RAG) application prototype built with Python, FastAPI, PostgreSQL with pgvector extension, and Angular 21 frontend.

## Features

- **Document Storage**: Store and manage documents (.txt, .pdf, .doc, .docx)
- **Document Indexing**: Manual document indexing for semantic search
- **Semantic Search**: Find relevant documents using cosine similarity
- **RAG Context Expansion**: Expand LLM context with relevant document chunks
- **Modern Web UI**: Minimalist Angular 21 interface with Markdown rendering

## Features

- **Document Storage**: Store and manage documents (.txt, .pdf, .doc, .docx)
- **Document Indexing**: Manual document indexing for semantic search
- **Semantic Search**: Find relevant documents using cosine similarity
- **RAG Context Expansion**: Expand LLM context with relevant document chunks

## Architecture

```
src/rag-prototype/
├── rag-ui/                  # Angular 21 frontend application
│   ├── src/
│   │   ├── app/
│   │   │   ├── app.ts       # Main application component
│   │   │   └── services/
│   │   │       └── rag.service.ts
│   │   └── main.ts          # Application entry point
│   ├── angular.json         # Angular configuration
│   └── package.json         # NPM dependencies
├── src/
│   ├── __init__.py
│   ├── main.py              # FastAPI application entry point
│   ├── config.py            # Application configuration
│   ├── database.py          # Database models and connection
│   ├── models.py            # Pydantic models for API
│   ├── routers/
│   │   ├── __init__.py
│   │   ├── files.py         # File upload/download/delete endpoints
│   │   ├── indexing.py      # Document indexing endpoints
│   │   └── search.py        # Search and RAG endpoints
│   └── services/
│       ├── __init__.py
│       └── indexer.py       # Indexing service with cosine similarity
├── backend-static/          # Built Angular frontend (generated)
├── requirements.txt         # Python dependencies
└── README.md
```

## API Endpoints

### File Management (`/api/v1/files`)
- `POST /` - Upload a file
- `GET /{file_id}` - Get file metadata
- `DELETE /{file_id}` - Delete a file (idempotent)

### Indexing (`/api/v1/indexing`)
- `POST /documents/{file_id}/index` - Index a specific document
- `POST /all` - Index all unindexed documents

### Search (`/api/v1/search`)
- `POST /documents` - Search for similar documents
- `POST /rag` - Perform RAG query with context expansion
- `POST /rag-answer` - Perform RAG query with full document context

### File Management (`/api/v1/file`)
- `POST /` - Upload a file
- `GET /{fileId}` - Get file metadata
- `GET /{fileId}/download` - Download a file
- `DELETE /{fileId}` - Delete a file (idempotent)

## Setup

### Prerequisites

- Python 3.9+
- Node.js 20+ (for Angular frontend)
- PostgreSQL with pgvector extension
- OpenAI API key or local LLM (llama.cpp)

### Local Development

1. Clone the repository:
```bash
cd src/rag-prototype
```

2. Install Python dependencies:
```bash
pip install -r requirements.txt
```

3. Install frontend dependencies:
```bash
cd rag-ui
npm install
cd ..
```

4. Create `.env` file from `.env.example`:
```bash
cp .env.example .env
```

5. Configure environment variables in `.env`:
```env
DATABASE_URL=postgresql://user:password@localhost:5432/rag_db
OPENAI_API_KEY=your_openai_api_key_here
OPENAI_BASE_URL=http://localhost:8033/v1
LLAMA_CPP_MODEL_PATH=/path/to/model.gguf
APP_NAME="RAG Prototype"
DEBUG=False
```

6. Initialize the database:
```python
python -c "from src.database import init_db; init_db()"
```

7. Build the frontend:
```bash
cd rag-ui
npm run build
cd ..
```

8. Run the backend:
```bash
uvicorn src.main:app --host 0.0.0.0 --port 8000 --reload
```

9. Access the application at http://localhost:8000 and Swagger UI at http://localhost:8000/docs

### Docker Deployment

1. Build and run with Docker Compose:
```bash
docker-compose up -d
```

2. The application will be available at http://localhost:8000

3. The frontend is served from `/static` and accessible at the root URL

## Database Schema

### Documents Table
- `id` (String, PK)
- `filename` (String)
- `file_type` (String)
- `file_size` (Float)
- `creation_date` (String)
- `last_modified_date` (String)
- `content_path` (String)
- `is_indexed` (Boolean)

### Embeddings Table
- `id` (String, PK)
- `text` (Text)
- `metadata` (JSON)
- `embedding` (Vector - 768 dimensions)
- `document_id` (String)
- `chunk_index` (Integer)

## Usage

1. **Upload a document**:
   ```bash
   curl -X POST "http://localhost:8000/api/v1/files/" \
     -H "accept: application/json" \
     -F "file=@document.txt"
   ```

2. **Index the document**:
   ```bash
   curl -X POST "http://localhost:8000/api/v1/indexing/documents/{file_id}/index" \
     -H "accept: application/json"
   ```

3. **Search documents**:
   ```bash
   curl -X POST "http://localhost:8000/api/v1/search/documents" \
     -H "accept: application/json" \
     -H "Content-Type: application/json" \
     -d '{"query": "your search query", "top_k": 5, "similarity_threshold": 0.75}'
   ```

4. **Perform RAG query**:
   ```bash
   curl -X POST "http://localhost:8000/api/v1/search/rag" \
     -H "accept: application/json" \
     -H "Content-Type: application/json" \
     -d '{"query": "your question", "top_k": 5, "similarity_threshold": 0.75}'
   ```

## Configuration

- `SIMILARITY_THRESHOLD`: Minimum cosine similarity for search results (default: 0.75)
- `TOP_K_RESULTS`: Number of top results to return (default: 5)
- `CHUNK_SIZE`: Size of text chunks for indexing (default: 1000)
- `CHUNK_OVERLAP`: Overlap between chunks (default: 200)

## License

MIT