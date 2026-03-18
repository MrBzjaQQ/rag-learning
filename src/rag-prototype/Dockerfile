FROM node:20-alpine AS frontend-builder

WORKDIR /app

RUN npm install -g @angular/cli@21

COPY rag-ui/package*.json ./

RUN npm install && npm cache clean --force

COPY rag-ui/ ./

RUN ng build rag-ui --configuration production


FROM python:3.11-slim as backend-base

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    g++ \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src ./src


FROM python:3.11-slim

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=backend-base /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=backend-base /usr/local/bin/uvicorn /usr/local/bin/uvicorn
COPY --from=backend-base /app/src ./src
COPY --from=frontend-builder /app/dist/rag-ui/browser ./static

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8000

CMD ["uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "8000"]