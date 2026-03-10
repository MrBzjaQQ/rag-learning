"""Search API endpoints."""
from fastapi import APIRouter, HTTPException, Depends
from sqlalchemy.orm import Session
from typing import List, Dict, Any

from src.config import settings
from src.database import get_db, Embedding
from src.models import SearchRequest, SearchResponse, RAGRequest, RAGResponse
from src.services.indexer import Indexer

router = APIRouter(prefix="/api/v1/search", tags=["search"])

def get_database_session():
    """Dependency to get database session."""
    db = next(get_db())
    try:
        yield db
    finally:
        db.close()

@router.post("/documents", response_model=SearchResponse)
def search_documents(request: SearchRequest, db: Session = Depends(get_database_session)):
    """Search for similar documents."""
    try:
        indexer = Indexer()
        
        # Get all embeddings from database
        embeddings = db.query(Embedding).all()
        
        # Find similar documents
        results = indexer.find_similar(
            query=request.query,
            embeddings=embeddings,
            threshold=request.similarity_threshold or settings.SIMILARITY_THRESHOLD,
            top_k=request.top_k or settings.TOP_K_RESULTS
        )
        
        return SearchResponse(results=results, total_count=len(results))
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error searching documents: {str(e)}")

@router.post("/rag", response_model=RAGResponse)
def rag_query(request: RAGRequest, db: Session = Depends(get_database_session)):
    """Perform RAG query with context expansion."""
    try:
        indexer = Indexer()
        
        # Get relevant documents
        embeddings = db.query(Embedding).all()
        similar_docs = indexer.find_similar(
            query=request.query,
            embeddings=embeddings,
            threshold=request.similarity_threshold or settings.SIMILARITY_THRESHOLD,
            top_k=request.top_k or settings.TOP_K_RESULTS
        )
        
        # Build context from similar documents
        context = "\n\n".join([doc["text"] for doc in similar_docs])
        
        # Generate answer using LLM with context
        answer = generate_answer(request.query, context)
        
        return RAGResponse(answer=answer, sources=similar_docs)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error performing RAG query: {str(e)}")

def generate_answer(query: str, context: str) -> str:
    """Generate answer using LLM with RAG context."""
    try:
        from openai import OpenAI
        
        client = OpenAI(
            api_key=settings.OPENAI_API_KEY,
            base_url=settings.OPENAI_BASE_URL
        )
        
        prompt = f"""Based on the following context, please answer the question.

Context:
{context}

Question:
{query}

Answer:"""
        
        response = client.chat.completions.create(
            model="nomic-ai/nomic-embed-text-v2-moe-GGUF",
            messages=[
                {"role": "system", "content": "You are a helpful assistant that answers questions based on the provided context."},
                {"role": "user", "content": prompt}
            ],
            temperature=0.1,
            max_tokens=500
        )
        
        return response.choices[0].message.content.strip()
    
    except Exception as e:
        return f"Error generating answer: {str(e)}"