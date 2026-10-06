# 🚀 AI Event-Driven Orchestration Platform

Plataforma que procesa tickets de soporte con IA en tiempo real.

## Arquitectura

```
Cliente → .NET API → RabbitMQ → Python Worker (IA) → RabbitMQ → .NET Consumer → PostgreSQL
```

## Stack

- **.NET 8** Minimal API + EF Core + RabbitMQ.Client
- **Python 3.11** + Transformers (DistilBERT) + Pika
- **PostgreSQL** persistencia
- **RabbitMQ** mensajería
- **Docker Compose** orquestación local

## Cómo correr

```bash
cp .env.example .env
docker compose up --build
```

Espera ~2 min la primera vez (descarga modelo IA).

## Probar

```bash
curl -X POST http://localhost:5000/api/tickets \
  -H "Content-Type: application/json" \
  -d '{"title":"App crashes","description":"Terrible experience, it always crashes!","customerEmail":"user@test.com"}'

sleep 3
curl http://localhost:5000/api/tickets
```

## Endpoints

- `GET  /health`
- `POST /api/tickets`
- `GET  /api/tickets`
- `GET  /api/tickets/{id}`

## Escalar

```bash
docker compose up --scale worker=4
```