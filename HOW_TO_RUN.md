## Requirements
- .NET 9 SDK
- Node.js 18+ with npm or pnpm
- PostgreSQL database (connection string configured in `backend/Api/appsettings.*.json`)

## Backend API
1. `cd backend/Api`
2. Restore dependencies: `dotnet restore`
3. create empty database named waste_incident
3. Apply EF Core migrations (local dev DB): `dotnet ef database update`
4. Run the service: `dotnet run`
5. API defaults to `https://localhost:7049` (see `Properties/launchSettings.json` for ports)

## Frontend (Next.js)
1. `cd frontend`
2. Install packages: `npm install`
3. Copy `.env.example` to `.env` and set `NEXT_PUBLIC_API_BASE` to your API URL
4. Start dev server: `npm run dev`
5. Visit `http://localhost:3000`


## Tests
- Backend: `cd backend/Test && dotnet test`

## Deployment Notes
- Backend deploys via `.github/workflows/deploy-api.yml` to Elastic Beanstalk (`waste-incident-staging`).
- Artifacts are zipped (`api-<sha>.zip`) and uploaded to `elasticbeanstalk-ap-southeast-1-932133996420`.
- Frontend can be hosted on AWS Amplify; ensure `NEXT_PUBLIC_API_BASE` points to an HTTPS API endpoint to avoid mixed-content issues.

