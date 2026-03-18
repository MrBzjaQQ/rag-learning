CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS documents (
    id VARCHAR PRIMARY KEY,
    filename VARCHAR NOT NULL,
    file_type VARCHAR NOT NULL,
    file_size FLOAT NOT NULL,
    creation_date VARCHAR,
    last_modified_date VARCHAR,
    content_path VARCHAR,
    is_indexed BOOLEAN DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS embeddings (
    id VARCHAR PRIMARY KEY,
    text TEXT NOT NULL,
    meta_data JSONB NOT NULL,
    embedding VECTOR(768),
    document_id VARCHAR,
    chunk_index INTEGER
);

CREATE INDEX IF NOT EXISTS embeddings_embedding_idx ON embeddings 
USING ivfflat (embedding vector_cosine_ops) 
WITH (lists = 100);

CREATE INDEX IF NOT EXISTS idx_embeddings_document_id ON embeddings(document_id);