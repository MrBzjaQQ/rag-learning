"""Configuration module for RAG application."""
import os
from dotenv import load_dotenv

load_dotenv()

class Settings:
    """Application settings."""
    
    # Database
    DATABASE_URL: str = os.getenv("DATABASE_URL", "postgresql://user:password@localhost:5432/rag_db")
    
    # OpenAI API
    OPENAI_API_KEY: str = os.getenv("OPENAI_API_KEY", "")
    OPENAI_BASE_URL: str = os.getenv("OPENAI_BASE_URL", "http://host.docker.internal:8033/v1")

    # OpenAI API Embeddings
    OPENAI_API_KEY_EMBEDDINGS: str = os.getenv("OPENAI_API_KEY_EMBEDDINGS", "")
    OPENAI_BASE_URL_EMBEDDINGS: str = os.getenv("OPENAI_BASE_URL_EMBEDDINGS", "http://host.docker.internal:8034/v1")
    
    # Llama.cpp
    LLAMA_CPP_MODEL_PATH: str = os.getenv("LLAMA_CPP_MODEL_PATH", "")
    
    # Application
    APP_NAME: str = os.getenv("APP_NAME", "RAG Prototype")
    DEBUG: bool = os.getenv("DEBUG", "False").lower() == "true"
    
    # Search settings
    SIMILARITY_THRESHOLD: float = 0.75
    TOP_K_RESULTS: int = 5
    
    # Chunk settings
    CHUNK_SIZE: int = 500
    CHUNK_OVERLAP: int = 100

settings = Settings()