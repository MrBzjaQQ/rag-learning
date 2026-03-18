import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { marked } from 'marked';

import { RAGService } from './services/rag.service';

interface FileSource {
  id: string;
  filename: string;
  file_type: string;
  file_size: number;
  creation_date?: string;
  last_modified_date?: string;
  content_path?: string;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  template: `
    <div class="container">
      <header class="header">
        <h1 class="title">RAG Search</h1>
      </header>

      <main class="main">
        @if (!result()) {
          <form class="query-form" (ngSubmit)="onSubmit()">
            <div class="input-group">
              <input
                type="text"
                [(ngModel)]="query"
                name="query"
                placeholder="Введите ваш запрос..."
                class="query-input"
                required
                autocomplete="off"
                [disabled]="loading()"
              />
              <button type="submit" class="submit-button" [disabled]="loading()">
                @if (loading()) {
                  <span class="spinner"></span> Отправка...
                } @else {
                  Отправить
                }
              </button>
            </div>
          </form>
        } @else {
          <div class="result-container">
            <div class="question-section">
              <span class="label">Вопрос:</span>
              <p class="question-text">{{ result()?.query }}</p>
            </div>

            <div class="answer-section">
              <div class="answer-header">
                <span class="label">Ответ:</span>
                <button class="copy-button" (click)="copyAnswer()" type="button">
                  {{ copied() ? 'Скопировано' : 'Копировать' }}
                </button>
              </div>
              @if (loading()) {
                <div class="loading-container">
                  <span class="spinner"></span>
                  <span class="loading-text">Генерация ответа...</span>
                </div>
              } @else {
                <div [innerHTML]="renderedAnswer()" class="answer-content"></div>
              }
            </div>

            @if (result()?.sources && result()?.sources.length > 0) {
              <div class="sources-section">
                <span class="label">Источники:</span>
                <ul class="sources-list">
                  @for (source of result()?.sources; track source.id) {
                    <li class="source-item">
                      <a
                        [href]="'/api/v1/file/' + source.id"
                        (click)="onFileClick($event, source.id, source.filename)"
                        class="source-link"
                      >
                        {{ source.filename }}
                      </a>
                    </li>
                  }
                </ul>
              </div>
            }

            <button class="new-query-button" (click)="reset()" type="button">
              Новый запрос
            </button>
          </div>
        }
      </main>
    </div>
  `,
  styles: [`
    .container {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      background-color: #f5f5f5;
    }

    .header {
      background-color: #2c3e50;
      color: white;
      padding: 20px 40px;
      margin: 0;
    }

    .title {
      margin: 0;
      font-size: 24px;
      font-weight: 500;
    }

    .main {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 40px 20px;
    }

    .query-form {
      width: 100%;
      max-width: 800px;
    }

    .input-group {
      display: flex;
      gap: 12px;
    }

    .query-input {
      flex: 1;
      padding: 14px 16px;
      font-size: 16px;
      border: 1px solid #ddd;
      border-radius: 4px;
      outline: none;
      transition: border-color 0.2s;
    }

    .query-input:focus {
      border-color: #3498db;
    }

    .submit-button {
      padding: 14px 24px;
      font-size: 16px;
      background-color: #3498db;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      transition: background-color 0.2s;
    }

    .submit-button:hover:not(:disabled) {
      background-color: #2980b9;
    }

    .submit-button:disabled {
      background-color: #95a5a6;
      cursor: not-allowed;
    }

    .spinner {
      display: inline-block;
      width: 16px;
      height: 16px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .loading-container {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 32px;
      background-color: white;
      border-radius: 4px;
      border: 1px solid #ddd;
    }

    .loading-text {
      color: #2c3e50;
      font-size: 16px;
    }

    .result-container {
      width: 100%;
      max-width: 800px;
    }

    .question-section,
    .answer-section,
    .sources-section {
      margin-bottom: 32px;
    }

    .label {
      display: block;
      font-weight: 600;
      color: #2c3e50;
      margin-bottom: 12px;
      font-size: 14px;
    }

    .question-text {
      padding: 16px;
      background-color: white;
      border-radius: 4px;
      border: 1px solid #ddd;
      margin: 0;
      font-size: 16px;
      line-height: 1.6;
    }

    .answer-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }

    .copy-button {
      padding: 8px 16px;
      font-size: 14px;
      background-color: #ecf0f1;
      color: #2c3e50;
      border: 1px solid #bdc3c7;
      border-radius: 4px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .copy-button:hover {
      background-color: #dfe6e9;
    }

    .answer-content {
      padding: 16px;
      background-color: white;
      border-radius: 4px;
      border: 1px solid #ddd;
      font-size: 16px;
      line-height: 1.6;
      overflow-x: auto;
    }

    .answer-content :deep(h1),
    .answer-content :deep(h2),
    .answer-content :deep(h3) {
      margin-top: 0;
      margin-bottom: 16px;
      color: #2c3e50;
    }

    .answer-content :deep(p) {
      margin: 0 0 16px 0;
      line-height: 1.6;
    }

    .answer-content :deep(ul),
    .answer-content :deep(ol) {
      margin: 0 0 16px 0;
      padding-left: 32px;
    }

    .answer-content :deep(li) {
      margin-bottom: 8px;
      line-height: 1.6;
    }

    .answer-content :deep(code) {
      background-color: #ecf0f1;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', Courier, monospace;
      font-size: 14px;
    }

    .answer-content :deep(pre) {
      background-color: #2c3e50;
      padding: 16px;
      border-radius: 4px;
      overflow-x: auto;
      margin: 0 0 16px 0;
    }

    .answer-content :deep(pre code) {
      background-color: transparent;
      padding: 0;
      color: #ecf0f1;
    }

    .answer-content :deep(blockquote) {
      border-left: 4px solid #3498db;
      padding-left: 16px;
      margin: 0 0 16px 0;
      color: #7f8c8d;
    }

    .answer-content :deep(a) {
      color: #3498db;
      text-decoration: none;
    }

    .answer-content :deep(a:hover) {
      text-decoration: underline;
    }

    .sources-list {
      list-style: none;
      padding-left: 0;
      margin: 0;
    }

    .source-item {
      margin-bottom: 8px;
    }

    .source-link {
      color: #3498db;
      text-decoration: none;
      font-size: 16px;
      transition: color 0.2s;
    }

    .source-link:hover {
      color: #2980b9;
      text-decoration: underline;
    }

    .new-query-button {
      padding: 14px 24px;
      font-size: 16px;
      background-color: #ecf0f1;
      color: #2c3e50;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      transition: background-color 0.2s;
    }

    .new-query-button:hover {
      background-color: #dfe6e9;
    }
  `]
})
export class App {
  private readonly ragService = inject(RAGService);

  protected query = signal('');
   protected result = signal<null | { query: string; answer: string; sources: FileSource[] }>(null);
   protected renderedAnswer = signal('');
   protected copied = signal(false);
   protected loading = signal(false);

  async onSubmit() {
     const queryValue = this.query();
     if (!queryValue.trim()) return;

     this.loading.set(true);

     try {
       const response = await this.ragService.ragAnswer({
         query: queryValue,
         top_k: 8,
         similarity_threshold: 0.05
       }).toPromise();

       this.result.set({
         query: queryValue,
         answer: response.answer,
         sources: response.sources
       });

       this.renderedAnswer.set(marked.parse(response.answer) as string);
     } catch (error) {
       console.error('Error fetching RAG answer:', error);
     } finally {
       this.loading.set(false);
     }
   }

  copyAnswer() {
    const answerText = this.result()?.answer || '';
    navigator.clipboard.writeText(answerText).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  onFileClick(event: MouseEvent, fileId: string, filename: string) {
    event.preventDefault();
    this.ragService.getFile(fileId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Error downloading file:', error);
      }
    });
  }

 reset() {
     this.query.set('');
     this.result.set(null);
     this.renderedAnswer.set('');
     this.loading.set(false);
   }
}