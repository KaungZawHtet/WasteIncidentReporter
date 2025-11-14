# Waste Incident Reporter

Full-stack prototype that helps city operators log waste incidents, detect duplicates, surface AI-generated insights, and monitor trends.

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
| DevOps | AWS Cloud (Amplify,AWS Certification Manager, Elastic BeamStack, EC2, LoadBalancer and so on... ), GitHub Actions → Elastic Beanstalk deploy, NameCheap Domain Service |


## Docs
- [AI_APPROACH.md](AI_APPROACH.md) – describes ML/AI techniques used.
- [ARCHITECTURE.md](ARCHITECTURE.md) – high-level system diagram and flows.
- [HOW_TO_RUN.md](HOW_TO_RUN.md) – quickstart guide.
- [BUG_LOG.md](BUG_LOG.md) – how we can manage bug on notable bugs
