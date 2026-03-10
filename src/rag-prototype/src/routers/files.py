"""File management API endpoints."""
from fastapi import APIRouter, HTTPException, UploadFile, File, status, Depends
from sqlalchemy.orm import Session
from typing import List
import uuid
import os
import shutil

from src.config import settings
from src.database import get_db, Document
from src.models import FileResponse, FileDeleteResponse

router = APIRouter(prefix="/api/v1/file", tags=["files"])

def get_database_session():
    """Dependency to get database session."""
    db = next(get_db())
    try:
        yield db
    finally:
        db.close()

UPLOAD_DIR = "uploads"

# Create upload directory if it doesn't exist
os.makedirs(UPLOAD_DIR, exist_ok=True)

@router.post("/", response_model=FileResponse, status_code=status.HTTP_201_CREATED)
async def upload_files(files: List[UploadFile] = File(...), db: Session = Depends(get_database_session)):
    """Upload files to the RAG system."""
    uploaded_files = []
    
    for file in files:
        # Generate unique ID
        file_id = str(uuid.uuid4())
        
        # Save file
        file_path = os.path.join(UPLOAD_DIR, f"{file_id}_{file.filename}")
        with open(file_path, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
        
        # Create document record
        doc = Document(
            id=file_id,
            filename=file.filename,
            file_type=file.content_type or "application/octet-stream",
            file_size=os.path.getsize(file_path),
            content_path=file_path,
            is_indexed=False
        )
        
        db.add(doc)
        uploaded_files.append(FileResponse(
            id=doc.id,
            filename=doc.filename,
            file_type=doc.file_type,
            file_size=doc.file_size,
            creation_date=doc.creation_date,
            last_modified_date=doc.last_modified_date,
            content_path=doc.content_path
        ))
    
    db.commit()
    
    # Return first file if only one uploaded, otherwise return all
    return uploaded_files[0] if len(uploaded_files) == 1 else uploaded_files

@router.get("/{fileId}", response_model=FileResponse)
def get_file(fileId: str, db: Session = Depends(get_database_session)):
    """Get file metadata by ID."""
    doc = db.query(Document).filter(Document.id == fileId).first()
    
    if not doc:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="File not found")
    
    return FileResponse(
        id=doc.id,
        filename=doc.filename,
        file_type=doc.file_type,
        file_size=doc.file_size,
        creation_date=doc.creation_date,
        last_modified_date=doc.last_modified_date,
        content_path=doc.content_path
    )

@router.delete("/{fileId}", response_model=FileDeleteResponse)
def delete_file(fileId: str, db: Session = Depends(get_database_session)):
    """Delete a file by ID (idempotent)."""
    doc = db.query(Document).filter(Document.id == fileId).first()
    
    if doc:
        # Delete physical file
        if os.path.exists(doc.content_path):
            os.remove(doc.content_path)
        
        # Delete database record
        db.delete(doc)
        db.commit()
        
        return FileDeleteResponse(success=True, message=f"File {fileId} deleted successfully")
    else:
        # Idempotent - return success even if file doesn't exist
        return FileDeleteResponse(success=True, message=f"File {fileId} not found (already deleted)")
