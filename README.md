# AdminDashBoard

AdminDashBoard is a local-first full-stack crypto analytics dashboard.

## Stack

- Frontend: Next.js, TypeScript, Tailwind CSS, TanStack Query, Recharts
- Backend: .NET 10, ASP.NET Core Web API
- Database: PostgreSQL
- Market data: CoinGecko API
- AI analysis: OpenAI API through the backend only

## Backend

```bash
dotnet build AdminDashBoard.slnx
dotnet test AdminDashBoard.slnx --no-build
dotnet run --project backend/src/AdminDashBoard.Api/AdminDashBoard.Api.csproj
```

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend URL:

```text
http://localhost:6001
```

Backend URL:

```text
http://localhost:6000
```

## Secrets

Real secrets are stored outside the repository with .NET user-secrets.

```bash
dotnet user-secrets set "ConnectionStrings:Postgres" "..." --project backend/src/AdminDashBoard.Api/AdminDashBoard.Api.csproj
dotnet user-secrets set "OpenAI:ApiKey" "..." --project backend/src/AdminDashBoard.Api/AdminDashBoard.Api.csproj
```

