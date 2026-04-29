# AdminDasboard

AdminDasboard is a local-first full-stack crypto analytics dashboard.

## Stack

- Frontend: Next.js, TypeScript, Tailwind CSS, TanStack Query, Recharts
- Backend: .NET 10, ASP.NET Core Web API
- Database: PostgreSQL
- Market data: CoinGecko API
- AI analysis: OpenAI API through the backend only

## Backend

```bash
dotnet build AdminDasboard.slnx
dotnet test AdminDasboard.slnx --no-build
dotnet run --project backend/src/AdminDasboard.Api/AdminDasboard.Api.csproj --urls http://localhost:5160
```

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend URL:

```text
http://localhost:3000
```

Backend URL:

```text
http://localhost:5160
```

## Secrets

Real secrets are stored outside the repository with .NET user-secrets.

```bash
dotnet user-secrets set "ConnectionStrings:Postgres" "..." --project backend/src/AdminDasboard.Api/AdminDasboard.Api.csproj
dotnet user-secrets set "OpenAI:ApiKey" "..." --project backend/src/AdminDasboard.Api/AdminDasboard.Api.csproj
```

OpenAI API billing is separate from a ChatGPT subscription. The dashboard uses the configured
OpenAI API key only on the backend; no AI secret is exposed to the frontend.



Tarefas:
mudas as portas da apps locais para range de 6000
Api => 6000 e site para 6001