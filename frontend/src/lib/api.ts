export type Currency = "eur" | "usd";
export type HistoryRange = 1 | 7 | 30 | 90 | 365;

export type AssetSearchResult = {
  id: string;
  symbol: string;
  name: string;
  marketCapRank: number | null;
  thumb: string | null;
};

export type CurrentMarketData = {
  price: number | null;
  marketCap: number | null;
  marketCapRank: number | null;
  totalVolume: number | null;
  high24h: number | null;
  low24h: number | null;
  priceChange24h: number | null;
  priceChangePercentage24h: number | null;
  lastUpdated: string | null;
};

export type HistoricalMarketPoint = {
  timestamp: string;
  price: number;
  marketCap: number | null;
  totalVolume: number | null;
};

export type MarketDataResponse = {
  coinId: string;
  symbol: string;
  name: string;
  currency: Currency;
  days: HistoryRange;
  current: CurrentMarketData;
  history: HistoricalMarketPoint[];
};

export type IndicatorPoint = {
  timestamp: string;
  value: number;
};

export type IndicatorsResponse = {
  coinId: string;
  currency: Currency;
  days: HistoryRange;
  sma20: IndicatorPoint[];
  ema20: IndicatorPoint[];
  rsi14: IndicatorPoint[];
  summary: {
    latestSma20: number | null;
    latestEma20: number | null;
    latestRsi14: number | null;
    rsiSignal: "overbought" | "oversold" | "neutral" | null;
  };
};

export type AnalysisResponse = {
  summary: string;
  trend: string;
  rsiComment: string;
  supportLevels: number[];
  resistanceLevels: number[];
  possibleEntryZone: string;
  stopLoss: string;
  takeProfitTargets: number[];
  riskLevel: "low" | "medium" | "high";
  disclaimer: string;
};

export type AnalysisHistoryItem = {
  id: string;
  coinId: string;
  currency: Currency;
  days: HistoryRange;
  riskLevel: "low" | "medium" | "high";
  createdAtUtc: string;
  analysis: AnalysisResponse;
};

export type AnalysisHistoryListResponse = {
  items: AnalysisHistoryItem[];
  totalCount: number;
  offset: number;
  limit: number;
};

export type MarketDataSnapshotListItem = {
  id: string;
  coinId: string;
  symbol: string;
  name: string;
  currency: Currency;
  days: HistoryRange;
  retrievedAtUtc: string;
  price: number | null;
  marketCap: number | null;
  totalVolume: number | null;
  priceChangePercentage24h: number | null;
  historyPoints: number;
};

export type MarketDataSnapshotListResponse = {
  items: MarketDataSnapshotListItem[];
  totalCount: number;
  offset: number;
  limit: number;
};

export type SystemStatusResponse = {
  status: string;
  environment: string;
  timestampUtc: string;
  database: {
    configured: boolean;
    health: string;
    appliedMigrations: string[];
    automaticMigrationsEnabled: boolean;
  };
  coinGecko: {
    baseUrl: string;
    apiKeyConfigured: boolean;
  };
  ai: {
    provider: string;
    configured: boolean;
    model: string;
    baseUrl: string;
  };
  cors: {
    allowedOrigins: string[];
  };
};

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5160";

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function getMarketData(coinId: string, currency: Currency, days: HistoryRange) {
  return getJson<MarketDataResponse>(
    `/api/market-data/${coinId}?currency=${currency}&days=${days}`,
  );
}

export function getIndicators(coinId: string, currency: Currency, days: HistoryRange) {
  return getJson<IndicatorsResponse>(
    `/api/market-data/${coinId}/indicators?currency=${currency}&days=${days}`,
  );
}

export function searchAssets(query: string, limit = 10) {
  return getJson<AssetSearchResult[]>(
    `/api/assets/search?query=${encodeURIComponent(query)}&limit=${limit}`,
  );
}

export async function createAnalysis(
  coinId: string,
  currency: Currency,
  days: HistoryRange,
) {
  const response = await fetch(`${apiBaseUrl}/api/analysis`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ coinId, currency, days }),
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string } | null;
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}`);
  }

  return response.json() as Promise<AnalysisResponse>;
}

export function getAnalysisHistory(
  coinId: string,
  currency: Currency,
  days: HistoryRange,
  limit = 5,
) {
  return getJson<AnalysisHistoryItem[]>(
    `/api/analysis/history/${coinId}?currency=${currency}&days=${days}&limit=${limit}`,
  );
}

export function listAnalysisHistory({
  coinId,
  currency,
  days,
  riskLevel,
  offset = 0,
  limit = 25,
}: {
  coinId?: string;
  currency?: Currency;
  days?: HistoryRange;
  riskLevel?: "low" | "medium" | "high";
  offset?: number;
  limit?: number;
}) {
  const search = new URLSearchParams({
    offset: String(offset),
    limit: String(limit),
  });

  if (coinId) {
    search.set("coinId", coinId);
  }

  if (currency) {
    search.set("currency", currency);
  }

  if (days) {
    search.set("days", String(days));
  }

  if (riskLevel) {
    search.set("riskLevel", riskLevel);
  }

  return getJson<AnalysisHistoryListResponse>(`/api/analysis/history?${search.toString()}`);
}

export function getMarketDataSnapshots(
  coinId?: string,
  currency?: Currency,
  days?: HistoryRange,
  limit = 25,
  offset = 0,
) {
  const search = new URLSearchParams({
    offset: String(offset),
    limit: String(limit),
  });

  if (coinId) {
    search.set("coinId", coinId);
  }

  if (currency) {
    search.set("currency", currency);
  }

  if (days) {
    search.set("days", String(days));
  }

  return getJson<MarketDataSnapshotListResponse>(`/api/market-data/snapshots?${search.toString()}`);
}

export function getSystemStatus() {
  return getJson<SystemStatusResponse>("/api/system/status");
}

export { apiBaseUrl };
