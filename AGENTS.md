# AGENTS.md

## Project

AdminDashBoard is a local-first full-stack crypto analytics dashboard.

Stack:
- Frontend: Next.js, TypeScript, App Router, Tailwind CSS, shadcn/ui
- Backend: .NET 10, ASP.NET Core Web API
- Database: PostgreSQL
- Market data: CoinGecko API
- AI analysis: Claude API via backend only

AdminDashBoard allows the user to select a crypto asset, view market data and historical charts, calculate technical indicators, and request an AI-assisted analysis.

## Working rules

- Work in small, testable phases.
- Do not generate the whole project in one pass.
- After each phase, explain:
  - files changed
  - commands to run
  - how to test
  - expected result
  - next recommended step
- Prefer clean, maintainable code over excessive architecture.
- Avoid overengineering.
- Do not add authentication yet.
- Do not implement real trading.
- Do not connect to exchanges.
- Do not create buy/sell automation.

## Security rules

- Never hardcode API keys.
- Never expose Anthropic/Claude API keys to the frontend.
- Frontend must call only the local backend.
- Backend is responsible for CoinGecko and Claude calls.
- Use environment variables, user-secrets, .env files, or appsettings.Development.json placeholders.
- Never commit real secrets.
- Validate all API inputs.

## Backend rules

Use:
- ASP.NET Core .NET 10
- Swagger/OpenAPI in development
- ProblemDetails for errors
- CORS restricted to the local frontend
- HttpClientFactory for external APIs
- Options Pattern for configuration
- EF Core for PostgreSQL when database integration begins
- Structured logging

Suggested backend structure:

/backend/src/AdminDashBoard.Api
/backend/src/AdminDashBoard.Application
/backend/src/AdminDashBoard.Domain
/backend/src/AdminDashBoard.Infrastructure
/backend/tests/AdminDashBoard.Tests

API constraints:
- Allowed currencies initially: eur, usd
- Allowed history ranges: 1, 7, 30, 90, 365
- Validate coin IDs and query parameters
- Return clear DTOs
- Do not leak provider-specific errors directly to the frontend

## Frontend rules

Use:
- Next.js App Router
- TypeScript
- Tailwind CSS
- shadcn/ui
- TanStack Query
- Recharts or lightweight-charts
- Zod where useful

Design:
- Modern SaaS/admin dashboard style
- Dark mode preferred
- Responsive layout
- Clean cards
- Large readable chart
- Sidebar and header
- Loading, error, and empty states

## AI analysis rules

Claude must receive only structured data from the backend:
- coin
- current market data
- historical prices
- calculated indicators
- summarized chart context

Claude must not be asked to invent prices or future guarantees.

Every AI response must include this disclaimer:

"Isto é uma análise automatizada com base em dados históricos e indicadores técnicos. Não constitui aconselhamento financeiro."

Preferred AI response shape:

{
  "summary": "...",
  "trend": "...",
  "rsiComment": "...",
  "supportLevels": [],
  "resistanceLevels": [],
  "possibleEntryZone": "...",
  "stopLoss": "...",
  "takeProfitTargets": [],
  "riskLevel": "low | medium | high",
  "disclaimer": "..."
}

## Commands

Frontend:
```bash
cd frontend
npm install
npm run dev
```

Backend:
```bash

dotnet build AdminDashBoard.slnx
dotnet test AdminDashBoard.slnx --no-build
```
