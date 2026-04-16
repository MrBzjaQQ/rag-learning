import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface RAGRequest {
  query: string;
  top_k?: number;
  similarity_threshold?: number;
}

export interface RAGResponse {
  answer: string;
  sources: Array<{
    text: string,
    score: number,
    document_id: string,
    chunk_index: number
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class RAGService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1';

  ragAnswer(request: RAGRequest): Observable<RAGResponse> {
    return this.http.post<RAGResponse>(`${this.apiUrl}/search/rag-answer`, request);
  }

  getFile(fileId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/file/${fileId}/download`, { responseType: 'blob' });
  }
}
