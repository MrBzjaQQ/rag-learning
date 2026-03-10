"""Pydantic models for RAG API."""
from pydantic import BaseModel
from typing import Optional, List, Dict, Any

class FileMetadata(BaseModel):
    """File metadata model."""
    id: str
    filename: str
    file_type: str
    file_size: float
    creation_date: Optional[str] = None
    last_modified_date: Optional[str] = None
    content_path: Optional[str] = None
    is_indexed: bool = False

class FileUpload(BaseModel):
    """File upload request model."""
    filename: str
    file_type: str
    file_size: float
    content_path: str

class FileResponse(BaseModel):
    """File response model."""
    id: str
    filename: str
    file_type: str
    file_size: float
    creation_date: Optional[str] = None
    last_modified_date: Optional[str] = None
    content_path: Optional[str] = None

class FileDeleteResponse(BaseModel):
    """File delete response model."""
    success: bool
    message: str

class DocumentChunk(BaseModel):
    """Document chunk model."""
    id: str
    text: str
    metadata: Dict[str, Any]
    document_id: str
    chunk_index: int

class SearchRequest(BaseModel):
    """Search request model."""
    query: str
    top_k: Optional[int] = 5
    similarity_threshold: Optional[float] = 0.75

class SearchResponse(BaseModel):
    """Search response model."""
    results: List[Dict[str, Any]]
    total_count: int

class RAGRequest(BaseModel):
    """RAG request model."""
    query: str
    top_k: Optional[int] = 5
    similarity_threshold: Optional[float] = 0.75

class RAGResponse(BaseModel):
    """RAG response model."""
    answer: str
    sources: List[Dict[str, Any]]