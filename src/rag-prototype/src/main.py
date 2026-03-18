"""Main application entry point."""
from fastapi import FastAPI, Response, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
import os

from src.config import settings
from src.database import init_db
from src.routers import files, indexing, search

# Initialize database
init_db()

# Create FastAPI app
app = FastAPI(
    title=settings.APP_NAME,
    description="RAG Prototype Application",
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
def root():
    """Root endpoint - serve index.html."""
    return Response(content=open("/app/static/index.html").read(), media_type="text/html")

@app.get("/health")
def health_check():
    """Health check endpoint."""
    return {"status": "healthy"}

# Include routers
app.include_router(files.router)
app.include_router(indexing.router)
app.include_router(search.router)

# Serve static files - mount last to avoid conflicts with other routes
# Mount at a specific path that doesn't conflict with API routes
STATIC_DIR = "/app/static"
if os.path.exists(STATIC_DIR):
    # Check if index.html exists, then mount at root but only serve if file exists
    @app.get("/{full_path:path}")
    async def serve_static(full_path: str):
        """Serve static files or index.html for unknown paths."""
        if full_path.startswith("api/"):
            raise HTTPException(status_code=404)
        
        file_path = os.path.join(STATIC_DIR, full_path) if full_path else os.path.join(STATIC_DIR, "index.html")
        
        if os.path.isfile(file_path):
            return Response(content=open(file_path).read(), media_type="text/html" if file_path.endswith(".html") else "application/javascript" if file_path.endswith(".js") else "text/css" if file_path.endswith(".css") else None)
        
        # Serve index.html for SPA routing
        if os.path.exists(os.path.join(STATIC_DIR, "index.html")):
            return Response(content=open(os.path.join(STATIC_DIR, "index.html")).read(), media_type="text/html")
        
        raise HTTPException(status_code=404)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("src.main:app", host="0.0.0.0", port=8000, reload=True)