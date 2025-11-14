# Waste Incident Reporter – Architecture Overview

This is overview of The entire project.

## High-Level Diagram
```
Browser (Next.js App on Amplify)
    │  HTTPS (REST)
    ▼
Elastic Beanstalk in Load Balancer Mode (HTTPS)
    │  ASP.NET Core API
    ▼
Backend (EC2 instances managed by EB)
    │
    ├─ PostgreSQL for incident data + vectors
    ├─ ML.NET services in-memory (TF-IDF embeddings, classification)

```

## Frontend
- Framework: Next.js + React Server Components.
- Hosting: AWS Amplify; custom domains set via Amplify Domain Management.

## Backend
- ASP.NET Core 9 Web API (`backend/Api`).
- Controllers:
  - `IncidentsController` – CRUD, CSV import, similarity lookups.
  - `InsightsController` – trends, anomalies, admin summaries.
- Services:
  - `IncidentService` – EF Core data access, CSV Helper ingest, vector generation via `TextEmbeddingService`.
  - `SimilarityService` – cosine similarity over vectors.
  - `TrendService` / `AnomalyService` – statistical intelligence (rolling means, z-scores).
  - `WasteClassificationService` – ML.NET multiclass classifier for waste types.
  - `TextEmbeddingService` – TF-IDF + n-gram features built from seed corpus + live data.
- Observability: Serilog console logs, `/health` & `/` endpoints for ELB.

## Data & AI Flow
1. **Model Initiation** – Train the two models on the bootstrap period.
2. **Similarity Search** – When users request “Similar Incidents,” vectors are compared via cosine similarity; thresholds drive UI messaging (e.g., 0.7 = high similarity).
3. **Classification** – `WasteClassificationService` predicts a waste category using ML.NET’s OneVersusAll + L-BFGS logistic regression.
4. **Insights** – Trend service aggregates counts per day and computes spike z-scores; anomaly service detects outliers via rolling window standard deviations; admin summary converts raw stats into English text.

## Persistence
- Database: PostgreSQL ( managed RDS in AWS).
- Schema: `Incidents` table includes `Id`, `Description`, `Timestamp`, `Location`, `Category`, `Status`, `TextVector` (double precision array).
- Seeding: CSV samples (`backend/db/waste_classification_samples.csv` and `backend/db/text_embedding_corpus.csv` ) for ~1000 entries.

## Deployment Pipeline
This is a brief of CI/CD Flow. 

- GitHub Actions workflow `.github/workflows/deploy-api.yml`:
  1. Restore + test backend.
  2. `dotnet publish` (linux-x64).
  3. Zip output, upload to S3 bucket `elasticbeanstalk-ap-southeast-1-932133996420`.
  4. Create EB application version (`waste-incident-be-new`) and deploy to env `Waste-incident-be-new-env`.
- Frontend: Amplify automatically rebuilds on repo pushes per `amplify.yml`.

## Environments & Configuration
- Environment vars handled via `appsettings.{Environment}.json` (backend) and `.env` (frontend).
- Health checks for EB target `/` or `/health`.
