"""Database module for RAG application."""
from sqlalchemy import create_engine, Column, String, Text, JSON, Float, Boolean, Integer
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from pgvector.sqlalchemy import Vector
import os
from dotenv import load_dotenv

load_dotenv()

Base = declarative_base()

class Document(Base):
    """Document model for storing file metadata."""
    __tablename__ = "documents"
    
    id = Column(String, primary_key=True, index=True)
    filename = Column(String, nullable=False)
    file_type = Column(String, nullable=False)
    file_size = Column(Float, nullable=False)
    creation_date = Column(String)
    last_modified_date = Column(String)
    content_path = Column(String)
    is_indexed = Column(Boolean, default=False)

class Embedding(Base):
    """Embedding model for storing chunk embeddings."""
    __tablename__ = "embeddings"
    
    id = Column(String, primary_key=True, index=True)
    text = Column(Text, nullable=False)
    meta_data = Column(JSON, nullable=False)
    embedding = Column(Vector(768))  # nomic-embed-text-v2-moe-GGUF produces 768-dim embeddings
    document_id = Column(String)
    chunk_index = Column(Integer)

# Create database engine
DATABASE_URL = os.getenv("DATABASE_URL")
if not DATABASE_URL:
    raise ValueError("DATABASE_URL environment variable is required")
engine = create_engine(DATABASE_URL, echo=False)

# Create session
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    """Get database session."""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

def init_db():
    """Initialize database tables."""
    from sqlalchemy import text
    
    DATABASE_URL = os.getenv("DATABASE_URL")
    if not DATABASE_URL:
        raise ValueError("DATABASE_URL environment variable is required")
    
    engine = create_engine(DATABASE_URL, echo=False)
    
    # Create pgvector extension
    with engine.connect() as conn:
        conn.execute(text("CREATE EXTENSION IF NOT EXISTS vector"))
        conn.commit()
    
    # Create tables
    Base.metadata.create_all(bind=engine)