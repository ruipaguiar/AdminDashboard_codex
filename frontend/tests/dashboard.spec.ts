import { expect, test } from "@playwright/test";

test("dashboard opens and navigates through main views", async ({ page }) => {
  await page.route("http://localhost:6000/api/market-data/bitcoin?**", async (route) => {
    await route.fulfill({
      json: {
        coinId: "bitcoin",
        symbol: "btc",
        name: "Bitcoin",
        currency: "eur",
        days: 30,
        current: {
          price: 65000,
          marketCap: 1200000000000,
          marketCapRank: 1,
          totalVolume: 30000000000,
          high24h: 66000,
          low24h: 64000,
          priceChange24h: 500,
          priceChangePercentage24h: 0.75,
          lastUpdated: "2026-04-29T10:00:00Z",
        },
        history: [
          { timestamp: "2026-04-28T10:00:00Z", price: 64000, marketCap: null, totalVolume: null },
          { timestamp: "2026-04-29T10:00:00Z", price: 65000, marketCap: null, totalVolume: null },
        ],
      },
    });
  });
  await page.route("http://localhost:6000/api/market-data/bitcoin/indicators?**", async (route) => {
    await route.fulfill({
      json: {
        coinId: "bitcoin",
        currency: "eur",
        days: 30,
        sma20: [],
        ema20: [],
        rsi14: [],
        summary: {
          latestSma20: null,
          latestEma20: null,
          latestRsi14: null,
          rsiSignal: null,
        },
      },
    });
  });
  await page.route("http://localhost:6000/api/analysis/history?**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, offset: 0, limit: 25 } });
  });
  await page.route("http://localhost:6000/api/analysis/history/bitcoin?**", async (route) => {
    await route.fulfill({ json: [] });
  });
  await page.route("http://localhost:6000/api/market-data/snapshots?**", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, offset: 0, limit: 25 } });
  });
  await page.route("http://localhost:6000/api/system/status", async (route) => {
    await route.fulfill({
      json: {
        status: "ok",
        environment: "Development",
        timestampUtc: "2026-04-29T10:00:00Z",
        database: {
          configured: true,
          health: "Healthy",
          appliedMigrations: ["20260428110204_InitialCreate"],
          automaticMigrationsEnabled: true,
        },
        coinGecko: {
          baseUrl: "https://api.coingecko.com/api/v3/",
          apiKeyConfigured: false,
        },
        ai: {
          provider: "OpenAI",
          configured: false,
          model: "gpt-5.4-nano",
          baseUrl: "https://api.openai.com/",
        },
        cors: {
          allowedOrigins: ["http://localhost:6001"],
        },
      },
    });
  });

  await page.goto("/");

  await expect(page.getByText("AdminDasboard").first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Crypto analytics" })).toBeVisible();

  await page.getByRole("button", { name: "Snapshots" }).click();
  await expect(page.getByRole("heading", { name: "Market snapshots" })).toBeVisible();

  await page.getByRole("button", { name: "Analysis" }).click();
  await expect(page.getByRole("heading", { name: "AI analysis history" })).toBeVisible();

  await page.getByRole("button", { name: "Settings" }).click();
  await expect(page.getByRole("heading", { name: "API" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Database" })).toBeVisible();

  await page.getByRole("button", { name: "Markets" }).click();
  await expect(page.getByRole("heading", { name: "Price chart" })).toBeVisible();
});
