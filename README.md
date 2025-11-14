# Waste Incident Reporter

Full-stack sample that helps city operators log waste incidents, detect duplicates, surface AI-generated insights, and monitor trends. The backend is an ASP.NET Core 9 API with ML.NET services for embeddings/classification; the frontend is a Next.js dashboard hosted separately (e.g., AWS Amplify).

## Key Features
- **Incident Management** – CRUD operations with CSV import/export.
- **Duplicate Detection** – Cosine similarity over TF‑IDF embeddings to highlight related incidents.
- **Waste Classification** – Offline ML.NET model predicts categories from descriptions.
- **Trend Insights** – Daily counts, anomaly alerts, and admin summaries exposed via `/api/insights/*`.

## Stack
| Layer | Tech |
| --- | --- |
| Backend | ASP.NET Core 9, Entity Framework Core, PostgreSQL, Serilog |
| AI Services | ML.NET (TF‑IDF embeddings, multiclass classification) |
| Frontend | Next.js 14, Tailwind CSS, Recharts |
| Tooling | CSV Helper, GitHub Actions → Elastic Beanstalk deploy |

## Running Locally
See [HOW_TO_RUN.md](HOW_TO_RUN.md) for detailed steps. In short:
1. `cd backend/Api && dotnet restore && dotnet ef database update && dotnet run`
2. `cd frontend && npm install && npm run dev`
3. Browse `http://localhost:3000` and set `NEXT_PUBLIC_API_BASE` to the API URL.


## Docs
- [AI_APPROACH.md](AI_APPROACH.md) – describes ML/AI techniques used.
- [ARCHITECTURE.md](ARCHITECTURE.md) – high-level system diagram and flows.
- [HOW_TO_RUN.md](HOW_TO_RUN.md) – quickstart guide.
