"""Document indexing API endpoints."""
import logging
from fastapi import APIRouter, HTTPException, BackgroundTasks, Depends
from sqlalchemy.orm import Session
from typing import List, Dict, Any
import uuid

from src.config import settings
from src.database import get_db, Document, Embedding, SessionLocal
from src.models import FileResponse
from src.services.indexer import Indexer

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/indexing", tags=["indexing"])

def get_database_session():
    """Dependency to get database session."""
    db = next(get_db())
    try:
        yield db
    finally:
        db.close()

@router.post("/documents/{file_id}/index")
def index_document(file_id: str):
    """Index a document for semantic search."""
    logger.info(f"Starting indexing for file ID: {file_id}")
    
    # Get database session
    db = SessionLocal()
    try:
        doc = db.query(Document).filter(Document.id == file_id).first()
        
        if not doc:
            raise HTTPException(status_code=404, detail="File not found")
        
        logger.info(f"Reading file: {doc.filename} ({doc.content_path})")
        
        # Read file content with error handling for different encodings
        try:
            with open(doc.content_path, 'r', encoding='utf-8', errors="ignore") as f:
                content = f.read()
        except UnicodeDecodeError:
            # Try with 'latin-1' encoding which can handle any byte sequence
            with open(doc.content_path, 'r', encoding='latin-1', errors="ignore") as f:
                content = f.read()
        except Exception as e:
            raise ValueError(f"Could not read file {doc.content_path}: {str(e)}")
        
        logger.info(f"File read successfully. Content length: {len(content)} characters")
        
        # Clean NUL characters from content (common in binary files like DOCX)
        content = content.replace('\x00', '')
        logger.info(f"Content cleaned (NUL chars removed). New length: {len(content)} characters")
        
        # Create indexer and process document
        indexer = Indexer()
        chunks = indexer.chunk_text(content)
        
        logger.info(f"Text chunked into {len(chunks)} parts")
        
        # Use batch processing for embeddings (much faster)
        if len(chunks) > 1:
            logger.info(f"Generating embeddings in batch for {len(chunks)} chunks...")
            embeddings = indexer.get_embeddings_batch(chunks)
            
            for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
                metadata = build_metadata(doc, chunk, i)
                
                emb = Embedding(
                    id=str(uuid.uuid4()),
                    text=chunk,
                    meta_data=metadata,
                    embedding=embedding,
                    document_id=file_id,
                    chunk_index=i
                )
                db.add(emb)
        else:
            # Single chunk - use original method
            for i, chunk in enumerate(chunks):
                logger.info(f"Processing chunk {i+1}/{len(chunks)} (length: {len(chunk)} chars)")
                
                embedding = indexer.get_embedding(chunk)
                
                metadata = build_metadata(doc, chunk, i)
                
                emb = Embedding(
                    id=str(uuid.uuid4()),
                    text=chunk,
                    meta_data=metadata,
                    embedding=embedding,
                    document_id=file_id,
                    chunk_index=i
                )
                db.add(emb)
        
        logger.info(f"Committing {len(chunks)} embeddings to database...")
        
        # Mark document as indexed
        doc.is_indexed = True
        db.commit()
        
        logger.info(f"Document {file_id} indexing completed successfully. Total chunks: {len(chunks)}")
        
        return {"success": True, "message": f"Document {file_id} indexed successfully", "chunks": len(chunks)}
    
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=500, detail=f"Error indexing document: {str(e)}")
    finally:
        db.close()

@router.post("/all")
def index_all_documents(background_tasks: BackgroundTasks, db: Session = Depends(get_database_session)):
    """Index all unindexed documents."""
    docs = db.query(Document).filter(Document.is_indexed == False).all()
    
    if not docs:
        return {"message": "No unindexed documents found"}
    
    # Process in background
    background_tasks.add_task(process_indexing, db, docs)
    
    return {"message": f"Started indexing {len(docs)} documents"}

def process_indexing(db: Session, docs: List[Document]):
    """Process document indexing in background."""
    logger.info(f"Starting indexing for {len(docs)} documents")
    
    indexer = Indexer()
    
    for idx, doc in enumerate(docs):
        try:
            logger.info(f"[{idx+1}/{len(docs)}] Processing document: {doc.filename} (ID: {doc.id})")
            
            # Read file content with error handling for different encodings
            try:
                with open(doc.content_path, 'r', encoding='utf-8', errors="ignore") as f:
                    content = f.read()
            except UnicodeDecodeError:
                # Try with 'latin-1' encoding which can handle any byte sequence
                with open(doc.content_path, 'r', encoding='latin-1', errors="ignore") as f:
                    content = f.read()
            except Exception as e:
                logger.error(f"Could not read file {doc.content_path}: {str(e)}")
                continue
            
            chunks = indexer.chunk_text(content)
            # Clean NUL characters from content (common in binary files like DOCX)
            content = content.replace('\x00', '')
            chunks = indexer.chunk_text(content)
            logger.info(f"[{idx+1}/{len(docs)}] Chunked into {len(chunks)} parts")
            
            # Use batch processing for embeddings (much faster)
            if len(chunks) > 1:
                logger.info(f"[{idx+1}/{len(docs)}] Generating embeddings in batch for {len(chunks)} chunks...")
                embeddings = indexer.get_embeddings_batch(chunks)
                
                for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
                    metadata = build_metadata(doc, chunk, i)
                    
                    emb = Embedding(
                        id=str(uuid.uuid4()),
                        text=chunk,
                        meta_data=metadata,
                        embedding=embedding,
                        document_id=doc.id,
                        chunk_index=i
                    )
                    db.add(emb)
            else:
                # Single chunk - use original method
                for i, chunk in enumerate(chunks):
                    logger.info(f"[{idx+1}/{len(docs)}] Processing chunk {i+1}/{len(chunks)} (length: {len(chunk)} chars)")
                    
                    embedding = indexer.get_embedding(chunk)
                    metadata = build_metadata(doc, chunk, i)
                    
                    emb = Embedding(
                        id=str(uuid.uuid4()),
                        text=chunk,
                        meta_data=metadata,
                        embedding=embedding,
                        document_id=doc.id,
                        chunk_index=i
                    )
                    db.add(emb)
            
            logger.info(f"[{idx+1}/{len(docs)}] Marking document as indexed")
            doc.is_indexed = True
            db.commit()
            
        except Exception as e:
            logger.error(f"Error indexing document {doc.id}: {str(e)}")
            continue
    
    logger.info("All documents indexing completed")

def build_metadata(doc: Document, text: str, chunk_index: int) -> Dict[str, Any]:
    """Build metadata for embedding."""
    return {
        "file_path": doc.content_path,
        "file_name": doc.filename,
        "file_type": doc.file_type,
        "file_size": doc.file_size,
        "creation_date": doc.creation_date,
        "last_modified_date": doc.last_modified_date,
        "_node_content": {
            "id_": str(uuid.uuid4()),
            "embedding": None,
            "metadata": {},
            "excluded_embed_metadata_keys": [],
            "excluded_llm_metadata_keys": [],
            "relationships": {},
            "text": text,
            "start_char_idx": chunk_index * settings.CHUNK_SIZE,
            "end_char_idx": (chunk_index + 1) * settings.CHUNK_SIZE,
            "text_template": "{text}",
            "metadata_template": "{metadata}",
            "metadata_separator": "\n",
            "class_name": "TextNode"
        },
        "_node_type": "TextNode",
        "document_id": doc.id,
        "doc_id": doc.id,
        "ref_doc_id": doc.id
    }