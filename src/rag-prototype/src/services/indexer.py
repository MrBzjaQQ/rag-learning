"""Document indexing service."""
from typing import List, Dict, Any
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity

from src.config import settings

class Indexer:
    """Indexer service for document processing and embedding generation."""
    
    def __init__(self):
        self.vectorizer = TfidfVectorizer(
            stop_words='english',
            use_idf=True,
            norm="l2",
            ngram_range=(1, 2),
            analyzer='word',
            sublinear_tf=True
        )
    
    def chunk_text(self, text: str) -> List[str]:
        """Split text into chunks."""
        chunks = []
        words = text.split()
        
        # Use smaller chunk size for embedding API (max 512 tokens)
        # Using chunk size that less than 512, otherwise it has errors
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
        vectorizer = TfidfVectorizer(
            stop_words='english',
            use_idf=True,
            norm="l2",
            ngram_range=(1, 2),
            analyzer='word',
            sublinear_tf=True
        )
        tfidf = vectorizer.fit_transform([text1, text2])
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
    
    def _generate_tfidf_embedding(self, text: str) -> List[float]:
        """Generate fallback TF-IDF based embedding."""
        try:
            tfidf = self.vectorizer.fit_transform([text])
            return tfidf.toarray()[0].tolist()
        except Exception:
            # Return zero vector as last resort
            return [0.0] * 768
    
    def find_similar(self, query: str, embeddings: List[Any], threshold: float = 0.75, top_k: int = 5) -> List[Dict[str, Any]]:
        """Find similar documents based on cosine similarity."""
        results = []
        
        for emb in embeddings:
            # Calculate similarity
            similarity = self.calculate_cosine_similarity(query, emb.text)
            
            if similarity >= threshold:
                results.append({
                    "text": emb.text,
                    "metadata": emb.meta_data,
                    "score": similarity,
                    "document_id": emb.document_id,
                    "chunk_index": emb.chunk_index
                })
        
        # Sort by score and return top_k
        results.sort(key=lambda x: x["score"], reverse=True)
        return results[:top_k]