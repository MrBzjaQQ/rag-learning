"""Document indexing service with batch processing for improved performance."""
from typing import List, Dict, Any, Optional
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from sqlalchemy.orm import Session
from pgvector.sqlalchemy import Vector

from src.config import settings
from src.database import Embedding, get_db


class Indexer:
    """Indexer service for document processing and embedding generation.
    
    Optimizations:
    - Batch embedding generation (reduces API calls)
    - Pre-computed TF-IDF vectorizer
    - Reusable vectorizer for similarity calculations
    """
    
    def __init__(self):
        # Initialize vectorizer once for all operations (kept for potential text-based fallback)
        self.vectorizer = TfidfVectorizer(
            stop_words='english',
            use_idf=True,
            norm="l2",
            ngram_range=(1, 2),
            analyzer='word',
            sublinear_tf=True
        )
        self.vectorizer_fitted = False
    
    def fit_vectorizer(self, texts: List[str]) -> None:
        """Fit the vectorizer on a batch of texts for efficient similarity calculations."""
        if not self.vectorizer_fitted:
            self.vectorizer.fit(texts)
            self.vectorizer_fitted = True
    
    def chunk_text(self, text: str) -> List[str]:
        """Split text into chunks."""
        chunks = []
        words = text.split()
        
        # Use smaller chunk size for embedding API (max 512 tokens)
        max_chunk_chars = settings.CHUNK_SIZE
        
        current_chunk = []
        current_length = 0
        
        for word in words:
            word_len = len(word) + 1  # +1 for space
            if current_length + word_len > max_chunk_chars and current_chunk:
                chunks.append(' '.join(current_chunk))
                current_chunk = [word]
                current_length = word_len
            else:
                current_chunk.append(word)
                current_length += word_len
        
        if current_chunk:
            chunks.append(' '.join(current_chunk))
        
        return chunks
    
    def calculate_cosine_similarity(self, text1: str, text2: str) -> float:
        """Calculate cosine similarity between two texts."""
        tfidf = self.vectorizer.transform([text1, text2])
        similarity = cosine_similarity(tfidf[0:1], tfidf[1:2])
        return float(similarity[0][0])
    
    def get_embedding(self, text: str) -> List[float]:
        """Generate embedding for a text chunk using OpenAI API."""
        try:
            from openai import OpenAI
            
            client = OpenAI(
                api_key=settings.OPENAI_API_KEY_EMBEDDINGS,
                base_url=settings.OPENAI_BASE_URL_EMBEDDINGS
            )
            
            response = client.embeddings.create(
                input=text,
                model="nomic-ai/nomic-embed-text-v2-moe-GGUF"
            )
            
            return response.data[0].embedding
        
        except Exception as e:
            print(f"Error generating embedding: {str(e)}")
            # Fallback to TF-IDF based embedding
            return self._generate_tfidf_embedding(text)
    
    def query_to_embedding(self, query: str) -> List[float]:
        """Convert query text to embedding vector."""
        return self.get_embedding(query)
    
    def get_embeddings_batch(self, texts: List[str]) -> List[List[float]]:
        """Generate embeddings for multiple texts in batch (more efficient)."""
        try:
            from openai import OpenAI
            
            client = OpenAI(
                api_key=settings.OPENAI_API_KEY_EMBEDDINGS,
                base_url=settings.OPENAI_BASE_URL_EMBEDDINGS
            )
            
            # Process in batches to avoid timeout
            all_embeddings = []
            batch_size = 10
            
            for i in range(0, len(texts), batch_size):
                batch = texts[i:i + batch_size]
                
                response = client.embeddings.create(
                    input=batch,
                    model="nomic-ai/nomic-embed-text-v2-moe-GGUF"
                )
                
                all_embeddings.extend([data.embedding for data in response.data])
            
            return all_embeddings
        
        except Exception as e:
            print(f"Error generating batch embeddings: {str(e)}")
            # Fallback to TF-IDF based embeddings
            return [self._generate_tfidf_embedding(text) for text in texts]
    
    def _generate_tfidf_embedding(self, text: str) -> List[float]:
        """Generate fallback TF-IDF based embedding with exactly 768 dimensions."""
        try:
            if not self.vectorizer_fitted:
                self.fit_vectorizer([text])
            tfidf = self.vectorizer.transform([text])
            embedding = tfidf.toarray()[0].tolist()
            
            # Pad or truncate to exactly 768 dimensions
            if len(embedding) < 768:
                embedding = embedding + [0.0] * (768 - len(embedding))
            elif len(embedding) > 768:
                embedding = embedding[:768]
            
            return embedding
        except Exception:
            # Return zero vector as last resort
            return [0.0] * 768
    
    def find_similar(self, query: str, embeddings: Optional[List[Any]] = None, threshold: float = 0.75, top_k: int = 5) -> List[Dict[str, Any]]:
        """Find similar documents based on cosine similarity using database vector search."""
        db = next(get_db())
        try:
            query_embedding = self.query_to_embedding(query)
            
            results = db.query(Embedding).order_by(
                Embedding.embedding.cosine_distance(query_embedding)
            ).limit(top_k).all()
            
            search_results = []
            for emb in results:
                emb_array = np.array(emb.embedding)
                query_array = np.array(query_embedding)
                
                cosine_sim = float(np.dot(emb_array, query_array) / (np.linalg.norm(emb_array) * np.linalg.norm(query_array)))
                
                search_results.append({
                    "text": emb.text,
                    "metadata": emb.meta_data,
                    "score": cosine_sim,
                    "document_id": emb.document_id,
                    "chunk_index": emb.chunk_index
                })
            
            return search_results
        
        finally:
            db.close()
