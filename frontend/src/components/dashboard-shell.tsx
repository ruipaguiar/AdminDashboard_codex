"use client";

import { FormEvent, useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  Activity,
  BarChart3,
  Bot,
  Coins,
  Database,
  KeyRound,
  LineChart,
  LogOut,
  RefreshCw,
  Search,
  Server,
  Settings,
  TrendingDown,
  TrendingUp,
} from "lucide-react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  Line,
  LineChart as ReLineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  AnalysisResponse,
  apiBaseUrl,
  AssetSearchResult,
  changePassword,
  createAnalysis,
  Currency,
  getAnalysisHistory,
  getIndicators,
  getMarketData,
  getMarketDataSnapshots,
  getSystemStatus,
  HistoryRange,
  IndicatorsResponse,
  listAnalysisHistory,
  MarketDataResponse,
  MarketDataSnapshotListResponse,
  searchAssets,
  SystemStatusResponse,
} from "@/lib/api";
import { cn, formatCurrency, formatNumber } from "@/lib/utils";

const defaultAsset: AssetSearchResult = {
  id: "bitcoin",
  symbol: "btc",
  name: "Bitcoin",
  marketCapRank: 1,
  thumb: null,
};

const ranges: HistoryRange[] = [1, 7, 30, 90, 365];
const currencies: Currency[] = ["eur", "usd"];
const pageSize = 25;
type DashboardView = "markets" | "snapshots" | "analysis" | "settings";
type RiskFilter = "all" | "low" | "medium" | "high";

export function DashboardShell({
  currentUserEmail,
  onLogout,
}: {
  currentUserEmail: string;
  onLogout: () => void;
}) {
  const [view, setView] = useState<DashboardView>("markets");
  const [selectedAsset, setSelectedAsset] = useState<AssetSearchResult>(defaultAsset);
  const [assetSearch, setAssetSearch] = useState("");
  const [assetPickerOpen, setAssetPickerOpen] = useState(false);
  const [currency, setCurrency] = useState<Currency>("eur");
  const [days, setDays] = useState<HistoryRange>(30);
  const [snapshotOffset, setSnapshotOffset] = useState(0);
  const [analysisOffset, setAnalysisOffset] = useState(0);
  const [riskFilter, setRiskFilter] = useState<RiskFilter>("all");
  const coinId = selectedAsset.id;

  const assetQuery = useQuery({
    queryKey: ["asset-search", assetSearch.trim()],
    queryFn: () => searchAssets(assetSearch.trim(), 10),
    enabled: assetSearch.trim().length >= 2,
    staleTime: 60_000,
  });

  const marketQuery = useQuery({
    queryKey: ["market-data", coinId, currency, days],
    queryFn: () => getMarketData(coinId, currency, days),
    enabled: view === "markets",
  });

  const indicatorsQuery = useQuery({
    queryKey: ["indicators", coinId, currency, days],
    queryFn: () => getIndicators(coinId, currency, days),
    enabled: view === "markets",
  });
  const analysisMutation = useMutation({
    mutationFn: () => createAnalysis(coinId, currency, days),
    onSuccess: () => {
      void analysisHistoryQuery.refetch();
    },
  });
  const analysisHistoryQuery = useQuery({
    queryKey: ["analysis-history", coinId, currency, days, view === "analysis" ? 25 : 5],
    queryFn: () => getAnalysisHistory(coinId, currency, days, view === "analysis" ? 25 : 5),
    enabled: view === "markets" || view === "analysis",
  });
  const snapshotsQuery = useQuery({
    queryKey: ["market-data-snapshots", coinId, currency, days, snapshotOffset],
    queryFn: () => getMarketDataSnapshots(coinId, currency, days, pageSize, snapshotOffset),
    enabled: view === "snapshots",
  });
  const fullAnalysisHistoryQuery = useQuery({
    queryKey: ["analysis-history-list", coinId, currency, days, riskFilter, analysisOffset],
    queryFn: () => listAnalysisHistory({
      coinId,
      currency,
      days,
      riskLevel: riskFilter === "all" ? undefined : riskFilter,
      offset: analysisOffset,
      limit: pageSize,
    }),
    enabled: view === "analysis",
  });
  const statusQuery = useQuery({
    queryKey: ["system-status"],
    queryFn: getSystemStatus,
    enabled: view === "settings",
    refetchInterval: view === "settings" ? 30_000 : false,
  });

  const selectAsset = (asset: AssetSearchResult) => {
    setSelectedAsset(asset);
    setAssetSearch("");
    setAssetPickerOpen(false);
    setSnapshotOffset(0);
    setAnalysisOffset(0);
    analysisMutation.reset();
  };

  const isRefreshing =
    view === "snapshots"
      ? snapshotsQuery.isFetching
      : view === "analysis"
        ? fullAnalysisHistoryQuery.isFetching
        : view === "settings"
          ? statusQuery.isFetching
      : marketQuery.isFetching || indicatorsQuery.isFetching;

  return (
    <div className="flex min-h-screen bg-[#0c0f14] text-slate-100">
      <aside className="hidden w-64 shrink-0 border-r border-white/10 bg-[#11161d] px-4 py-5 lg:block">
        <div className="flex h-10 items-center gap-3 px-2">
          <div className="grid size-9 place-items-center rounded-md bg-teal-400 text-slate-950">
            <Coins className="size-5" aria-hidden="true" />
          </div>
          <div>
            <div className="text-sm font-semibold">AdminDashBoard</div>
            <div className="text-xs text-slate-400">Local analytics</div>
          </div>
        </div>

        <nav className="mt-8 space-y-1">
          <SidebarItem icon={BarChart3} label="Markets" active={view === "markets"} onClick={() => setView("markets")} />
          <SidebarItem icon={LineChart} label="Indicators" active={view === "markets"} onClick={() => setView("markets")} />
          <SidebarItem icon={Bot} label="Analysis" active={view === "analysis"} onClick={() => setView("analysis")} />
          <SidebarItem icon={Database} label="Snapshots" active={view === "snapshots"} onClick={() => setView("snapshots")} />
          <SidebarItem icon={Settings} label="Settings" active={view === "settings"} onClick={() => setView("settings")} />
        </nav>

        <div className="mt-8 border-t border-white/10 pt-5">
          <div className="flex items-center gap-2 px-2 text-xs text-slate-400">
            <Server className="size-4" aria-hidden="true" />
            <span>API http://localhost:6002</span>
          </div>
          <button
            type="button"
            onClick={onLogout}
            className="mt-4 flex h-10 w-full items-center gap-3 rounded-md px-3 text-sm text-slate-400 transition hover:bg-white/5 hover:text-slate-200"
          >
            <LogOut className="size-4" aria-hidden="true" />
            <span>Logout</span>
          </button>
        </div>
      </aside>

      <main className="min-w-0 flex-1">
        <header className="border-b border-white/10 bg-[#0f141b]/95 px-4 py-4 backdrop-blur md:px-6">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
            <div>
              <div className="text-xs font-medium uppercase text-teal-300">Market workspace</div>
              <h1 className="mt-1 text-2xl font-semibold text-slate-50 md:text-3xl">
                Crypto analytics
              </h1>
              <div className="mt-1 text-sm text-slate-400">{currentUserEmail}</div>
            </div>

            <div className="flex flex-col gap-3 md:flex-row md:items-center">
              <AssetSearchBox
                selectedAsset={selectedAsset}
                query={assetSearch}
                results={assetQuery.data}
                loading={assetQuery.isFetching}
                error={assetQuery.isError}
                open={assetPickerOpen}
                onQueryChange={(value) => {
                  setAssetSearch(value);
                  setAssetPickerOpen(true);
                }}
                onOpenChange={setAssetPickerOpen}
                onSelect={selectAsset}
              />

              <SegmentedControl
                label="Currency"
                values={currencies}
                value={currency}
                onChange={(value) => {
                  setCurrency(value as Currency);
                  setSnapshotOffset(0);
                  setAnalysisOffset(0);
                }}
              />

              <SegmentedControl
                label="Range"
                values={ranges.map(String)}
                value={String(days)}
                onChange={(value) => {
                  setDays(Number(value) as HistoryRange);
                  setSnapshotOffset(0);
                  setAnalysisOffset(0);
                }}
                formatter={(value) => `${value}D`}
              />

              <button
                type="button"
                onClick={() => {
                  if (view === "snapshots") {
                    void snapshotsQuery.refetch();
                    return;
                  }

                  if (view === "analysis") {
                    void fullAnalysisHistoryQuery.refetch();
                    return;
                  }

                  if (view === "settings") {
                    void statusQuery.refetch();
                    return;
                  }

                  void marketQuery.refetch();
                  void indicatorsQuery.refetch();
                  analysisMutation.reset();
                }}
                className="grid size-10 place-items-center rounded-md border border-white/10 bg-[#151b23] text-slate-200 transition hover:bg-[#1c2631]"
                title="Refresh"
                aria-label="Refresh"
              >
                <RefreshCw className={cn("size-4", isRefreshing && "animate-spin")} />
              </button>
              <button
                type="button"
                onClick={onLogout}
                className="grid size-10 place-items-center rounded-md border border-white/10 bg-[#151b23] text-slate-200 transition hover:bg-[#1c2631] lg:hidden"
                title="Logout"
                aria-label="Logout"
              >
                <LogOut className="size-4" aria-hidden="true" />
              </button>
            </div>
          </div>
        </header>

        <section className="space-y-5 px-4 py-5 md:px-6">
          {view === "snapshots" ? (
            snapshotsQuery.isError ? (
              <ErrorState />
            ) : (
              <SnapshotsPanel
                snapshots={snapshotsQuery.data}
                loading={snapshotsQuery.isLoading}
                currency={currency}
                onPrevious={() => setSnapshotOffset(Math.max(snapshotOffset - pageSize, 0))}
                onNext={() => setSnapshotOffset(snapshotOffset + pageSize)}
              />
            )
          ) : view === "analysis" ? (
            fullAnalysisHistoryQuery.isError ? (
              <ErrorState />
            ) : (
              <AnalysisHistoryPanel
                history={fullAnalysisHistoryQuery.data}
                loading={fullAnalysisHistoryQuery.isLoading}
                currency={currency}
                riskFilter={riskFilter}
                onRiskFilterChange={(value) => {
                  setRiskFilter(value);
                  setAnalysisOffset(0);
                }}
                onPrevious={() => setAnalysisOffset(Math.max(analysisOffset - pageSize, 0))}
                onNext={() => setAnalysisOffset(analysisOffset + pageSize)}
              />
            )
          ) : view === "settings" ? (
            statusQuery.isError ? (
              <ErrorState />
            ) : (
              <SettingsPanel
                status={statusQuery.data}
                loading={statusQuery.isLoading}
                currentUserEmail={currentUserEmail}
                onTestSearch={() => {
                  setAssetSearch("bitcoin");
                  setAssetPickerOpen(true);
                }}
              />
            )
          ) : marketQuery.isError || indicatorsQuery.isError ? (
            <ErrorState />
          ) : (
            <>
              <MetricGrid data={marketQuery.data} loading={marketQuery.isLoading} currency={currency} />
              <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
                <PriceChart
                  data={marketQuery.data}
                  indicators={indicatorsQuery.data}
                  loading={marketQuery.isLoading || indicatorsQuery.isLoading}
                  currency={currency}
                />
                <IndicatorPanel
                  data={indicatorsQuery.data}
                  loading={indicatorsQuery.isLoading}
                  currency={currency}
                />
              </div>
              <AnalysisPanel
                analysis={analysisMutation.data}
                error={analysisMutation.error}
                history={analysisHistoryQuery.data}
                loading={analysisMutation.isPending}
                currency={currency}
                onAnalyze={() => analysisMutation.mutate()}
              />
            </>
          )}
        </section>
      </main>
    </div>
  );
}

function AssetSearchBox({
  selectedAsset,
  query,
  results,
  loading,
  error,
  open,
  onQueryChange,
  onOpenChange,
  onSelect,
}: {
  selectedAsset: AssetSearchResult;
  query: string;
  results?: AssetSearchResult[];
  loading: boolean;
  error: boolean;
  open: boolean;
  onQueryChange: (value: string) => void;
  onOpenChange: (open: boolean) => void;
  onSelect: (asset: AssetSearchResult) => void;
}) {
  const showResults = open && query.trim().length >= 2;

  return (
    <div className="relative w-full md:w-80">
      <label className="sr-only" htmlFor="asset-search">
        Asset
      </label>
      <div className="flex min-h-10 items-center gap-2 rounded-md border border-white/10 bg-[#151b23] px-3 ring-teal-300/40 focus-within:ring-2">
        <Search className="size-4 shrink-0 text-slate-500" aria-hidden="true" />
        <input
          id="asset-search"
          value={query}
          onChange={(event) => onQueryChange(event.target.value)}
          onFocus={() => onOpenChange(true)}
          placeholder={`${selectedAsset.symbol.toUpperCase()} - ${selectedAsset.name}`}
          className="h-10 min-w-0 flex-1 bg-transparent text-sm text-slate-100 outline-none placeholder:text-slate-400"
          autoComplete="off"
        />
        <span className="shrink-0 rounded bg-white/10 px-2 py-1 text-[11px] font-semibold uppercase text-slate-300">
          {selectedAsset.symbol}
        </span>
      </div>

      {showResults ? (
        <div className="absolute left-0 right-0 top-12 z-20 overflow-hidden rounded-md border border-white/10 bg-[#151b23] shadow-2xl shadow-black/30">
          {loading ? (
            <div className="p-3 text-sm text-slate-400">Searching...</div>
          ) : error ? (
            <div className="p-3 text-sm text-amber-200">Asset search unavailable.</div>
          ) : results?.length ? (
            <div className="max-h-80 overflow-y-auto py-1">
              {results.map((asset) => (
                <button
                  key={asset.id}
                  type="button"
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => onSelect(asset)}
                  className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left transition hover:bg-white/10"
                >
                  <span className="min-w-0">
                    <span className="block truncate text-sm font-medium text-slate-100">{asset.name}</span>
                    <span className="block truncate text-xs text-slate-500">{asset.id}</span>
                  </span>
                  <span className="flex shrink-0 items-center gap-2">
                    {asset.marketCapRank ? (
                      <span className="text-xs text-slate-500">#{asset.marketCapRank}</span>
                    ) : null}
                    <span className="rounded bg-white/10 px-2 py-1 text-xs font-semibold uppercase text-slate-300">
                      {asset.symbol}
                    </span>
                  </span>
                </button>
              ))}
            </div>
          ) : (
            <div className="p-3 text-sm text-slate-400">No assets found.</div>
          )}
        </div>
      ) : null}
    </div>
  );
}

function SidebarItem({
  icon: Icon,
  label,
  active = false,
  onClick,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  active?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex h-10 w-full items-center gap-3 rounded-md px-3 text-sm text-slate-400 transition",
        active ? "bg-white/10 text-slate-50" : "hover:bg-white/5 hover:text-slate-200",
      )}
    >
      <Icon className="size-4" aria-hidden="true" />
      <span>{label}</span>
    </button>
  );
}

function SnapshotsPanel({
  snapshots,
  loading,
  currency,
  onPrevious,
  onNext,
}: {
  snapshots?: MarketDataSnapshotListResponse;
  loading: boolean;
  currency: Currency;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const items = snapshots?.items ?? [];

  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d]">
      <div className="flex flex-col gap-2 border-b border-white/10 p-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-semibold text-slate-50">Market snapshots</h2>
          <p className="mt-1 text-sm text-slate-400">
            Cached market responses stored by the backend for the selected filters.
          </p>
        </div>
        <div className="text-xs uppercase text-slate-500">
          {currency} {snapshots ? `- ${snapshots.totalCount} records` : ""}
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full table-fixed text-left text-sm">
          <thead className="border-b border-white/10 text-xs uppercase text-slate-500">
            <tr>
              <th className="w-48 px-4 py-3 font-medium">Asset</th>
              <th className="w-28 px-4 py-3 font-medium">Range</th>
              <th className="w-36 px-4 py-3 font-medium">Price</th>
              <th className="w-28 px-4 py-3 font-medium">24h</th>
              <th className="w-36 px-4 py-3 font-medium">Market cap</th>
              <th className="w-36 px-4 py-3 font-medium">Volume</th>
              <th className="w-32 px-4 py-3 font-medium">Points</th>
              <th className="w-44 px-4 py-3 font-medium">Retrieved</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {loading ? (
              Array.from({ length: 6 }).map((_, index) => (
                <tr key={index}>
                  <td className="px-4 py-3" colSpan={8}>
                    <Skeleton className="h-6 w-full" />
                  </td>
                </tr>
              ))
            ) : items.length ? (
              items.map((snapshot) => (
                <tr key={snapshot.id} className="hover:bg-white/[0.03]">
                  <td className="px-4 py-3">
                    <div className="truncate font-medium text-slate-100">{snapshot.name}</div>
                    <div className="truncate text-xs text-slate-500">
                      {snapshot.symbol.toUpperCase()} - {snapshot.coinId}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-slate-300">{snapshot.days}D</td>
                  <td className="px-4 py-3 text-slate-100">{formatCurrency(snapshot.price, snapshot.currency)}</td>
                  <td className={cn(
                    "px-4 py-3",
                    (snapshot.priceChangePercentage24h ?? 0) >= 0 ? "text-emerald-300" : "text-rose-300",
                  )}>
                    {snapshot.priceChangePercentage24h == null
                      ? "n/a"
                      : `${snapshot.priceChangePercentage24h.toFixed(2)}%`}
                  </td>
                  <td className="px-4 py-3 text-slate-300">{formatNumber(snapshot.marketCap)}</td>
                  <td className="px-4 py-3 text-slate-300">{formatNumber(snapshot.totalVolume)}</td>
                  <td className="px-4 py-3 text-slate-300">{snapshot.historyPoints}</td>
                  <td className="px-4 py-3 text-slate-400">
                    {new Date(snapshot.retrievedAtUtc).toLocaleString("pt-PT")}
                  </td>
                </tr>
              ))
            ) : (
              <tr>
                <td className="px-4 py-10 text-center text-slate-500" colSpan={8}>
                  No snapshots found for the selected filters. Open Markets for this asset to cache market data.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      <PaginationFooter
        loading={loading}
        offset={snapshots?.offset ?? 0}
        limit={snapshots?.limit ?? pageSize}
        totalCount={snapshots?.totalCount ?? 0}
        onPrevious={onPrevious}
        onNext={onNext}
      />
    </div>
  );
}

function AnalysisHistoryPanel({
  history,
  loading,
  currency,
  riskFilter,
  onRiskFilterChange,
  onPrevious,
  onNext,
}: {
  history?: {
    items: Array<{
      id: string;
      coinId: string;
      currency: Currency;
      days: HistoryRange;
      createdAtUtc: string;
      riskLevel: "low" | "medium" | "high";
      analysis: AnalysisResponse;
    }>;
    totalCount: number;
    offset: number;
    limit: number;
  };
  loading: boolean;
  currency: Currency;
  riskFilter: RiskFilter;
  onRiskFilterChange: (value: RiskFilter) => void;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const items = history?.items ?? [];

  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d]">
      <div className="flex flex-col gap-2 border-b border-white/10 p-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-semibold text-slate-50">AI analysis history</h2>
          <p className="mt-1 text-sm text-slate-400">
            Saved analysis responses for the selected asset, currency and range.
          </p>
        </div>
        <div className="flex flex-col gap-2 md:items-end">
          <div className="text-xs uppercase text-slate-500">
            {currency} {history ? `- ${history.totalCount} records` : ""}
          </div>
          <SegmentedControl
            label="Risk"
            values={["all", "low", "medium", "high"]}
            value={riskFilter}
            onChange={(value) => onRiskFilterChange(value as RiskFilter)}
          />
        </div>
      </div>

      <div className="divide-y divide-white/10">
        {loading ? (
          Array.from({ length: 4 }).map((_, index) => (
            <div key={index} className="p-4">
              <Skeleton className="h-28 w-full" />
            </div>
          ))
        ) : items.length ? (
          items.map((item) => (
            <article key={item.id} className="p-4">
              <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="rounded bg-white/10 px-2 py-1 text-xs font-semibold uppercase text-slate-300">
                      {item.coinId}
                    </span>
                    <span className="rounded bg-white/10 px-2 py-1 text-xs font-semibold uppercase text-slate-300">
                      {item.days}D
                    </span>
                    <RiskBadge risk={item.riskLevel} />
                  </div>
                  <h3 className="mt-3 text-base font-semibold text-slate-50">{item.analysis.summary}</h3>
                  <div className="mt-1 text-xs text-slate-500">
                    {new Date(item.createdAtUtc).toLocaleString("pt-PT")}
                  </div>
                </div>
              </div>

              <div className="mt-4 grid gap-3 xl:grid-cols-[minmax(0,1fr)_340px]">
                <div className="space-y-3">
                  <AnalysisBlock title="Trend" value={item.analysis.trend} />
                  <AnalysisBlock title="RSI" value={item.analysis.rsiComment} />
                  <AnalysisBlock title="Entry" value={item.analysis.possibleEntryZone} />
                  <AnalysisBlock title="Stop loss" value={item.analysis.stopLoss} />
                </div>
                <div className="space-y-3">
                  <LevelList title="Support" levels={item.analysis.supportLevels} currency={currency} />
                  <LevelList title="Resistance" levels={item.analysis.resistanceLevels} currency={currency} />
                  <LevelList title="Targets" levels={item.analysis.takeProfitTargets} currency={currency} />
                </div>
              </div>

              <div className="mt-3 rounded-md border border-white/10 bg-[#151b23] p-3 text-xs text-slate-400">
                {item.analysis.disclaimer}
              </div>
            </article>
          ))
        ) : (
          <div className="px-4 py-10 text-center text-slate-500">
            No AI analyses found for the selected filters. Generate one from Markets to populate this view.
          </div>
        )}
      </div>
      <PaginationFooter
        loading={loading}
        offset={history?.offset ?? 0}
        limit={history?.limit ?? pageSize}
        totalCount={history?.totalCount ?? 0}
        onPrevious={onPrevious}
        onNext={onNext}
      />
    </div>
  );
}

function PaginationFooter({
  loading,
  offset,
  limit,
  totalCount,
  onPrevious,
  onNext,
}: {
  loading: boolean;
  offset: number;
  limit: number;
  totalCount: number;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const currentEnd = Math.min(offset + limit, totalCount);

  return (
    <div className="flex flex-col gap-3 border-t border-white/10 p-4 text-sm text-slate-400 md:flex-row md:items-center md:justify-between">
      <span>
        {totalCount === 0 ? "0 records" : `${offset + 1}-${currentEnd} of ${totalCount}`}
      </span>
      <div className="flex gap-2">
        <button
          type="button"
          onClick={onPrevious}
          disabled={loading || offset === 0}
          className="h-9 rounded-md border border-white/10 px-3 text-slate-200 transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Previous
        </button>
        <button
          type="button"
          onClick={onNext}
          disabled={loading || offset + limit >= totalCount}
          className="h-9 rounded-md border border-white/10 px-3 text-slate-200 transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Next
        </button>
      </div>
    </div>
  );
}

function RiskBadge({ risk }: { risk: "low" | "medium" | "high" }) {
  return (
    <span
      className={cn(
        "rounded px-2 py-1 text-xs font-semibold uppercase",
        risk === "low" && "bg-emerald-400/15 text-emerald-200",
        risk === "medium" && "bg-amber-400/15 text-amber-200",
        risk === "high" && "bg-rose-400/15 text-rose-200",
      )}
    >
      {risk}
    </span>
  );
}

function SettingsPanel({
  status,
  loading,
  currentUserEmail,
  onTestSearch,
}: {
  status?: SystemStatusResponse;
  loading: boolean;
  currentUserEmail: string;
  onTestSearch: () => void;
}) {
  if (loading) {
    return (
      <div className="grid gap-4 xl:grid-cols-2">
        {Array.from({ length: 4 }).map((_, index) => (
          <Skeleton key={index} className="h-40 w-full" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-4 xl:grid-cols-2">
        <StatusCard
          title="API"
          status={status?.status === "ok" ? "ok" : "warning"}
          rows={[
            ["Environment", status?.environment ?? "n/a"],
            ["Frontend API URL", apiBaseUrl],
            ["Last check", status ? new Date(status.timestampUtc).toLocaleString("pt-PT") : "n/a"],
          ]}
        />
        <StatusCard
          title="Database"
          status={status?.database.health === "Healthy" ? "ok" : "warning"}
          rows={[
            ["Configured", status?.database.configured ? "yes" : "no"],
            ["Health", status?.database.health ?? "n/a"],
            ["Auto migrations", status?.database.automaticMigrationsEnabled ? "enabled" : "disabled"],
            ["Applied migrations", String(status?.database.appliedMigrations.length ?? 0)],
          ]}
        />
        <StatusCard
          title="CoinGecko"
          status={status?.coinGecko.baseUrl ? "ok" : "warning"}
          rows={[
            ["Base URL", status?.coinGecko.baseUrl ?? "n/a"],
            ["API key", status?.coinGecko.apiKeyConfigured ? "configured" : "not configured"],
            ["Timeout", "10-15s"],
          ]}
          actionLabel="Test search"
          onAction={onTestSearch}
        />
        <StatusCard
          title="AI"
          status={status?.ai.configured ? "ok" : "warning"}
          rows={[
            ["Provider", status?.ai.provider ?? "n/a"],
            ["Configured", status?.ai.configured ? "yes" : "no"],
            ["Model", status?.ai.model ?? "n/a"],
            ["Base URL", status?.ai.baseUrl ?? "n/a"],
          ]}
        />
      </div>

      <div className="rounded-lg border border-white/10 bg-[#11161d] p-4">
        <h2 className="text-base font-semibold text-slate-50">Allowed origins</h2>
        <div className="mt-3 flex flex-wrap gap-2">
          {(status?.cors.allowedOrigins ?? []).map((origin) => (
            <span key={origin} className="rounded bg-white/10 px-2 py-1 text-xs text-slate-300">
              {origin}
            </span>
          ))}
        </div>
      </div>

      <PasswordCard currentUserEmail={currentUserEmail} />
    </div>
  );
}

function PasswordCard({ currentUserEmail }: { currentUserEmail: string }) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);
  const passwordMutation = useMutation({
    mutationFn: () => changePassword(currentPassword, newPassword),
    onSuccess: () => {
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setLocalError(null);
    },
  });

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (newPassword !== confirmPassword) {
      setLocalError("The new passwords do not match.");
      return;
    }

    if (newPassword.length < 8) {
      setLocalError("Use at least 8 characters.");
      return;
    }

    setLocalError(null);
    passwordMutation.mutate();
  };

  return (
    <form onSubmit={onSubmit} className="rounded-lg border border-white/10 bg-[#11161d] p-4">
      <div className="flex items-start gap-3">
        <div className="grid size-9 shrink-0 place-items-center rounded-md bg-white/10 text-teal-200">
          <KeyRound className="size-4" aria-hidden="true" />
        </div>
        <div>
          <h2 className="text-base font-semibold text-slate-50">Password</h2>
          <p className="mt-1 text-sm text-slate-400">{currentUserEmail}</p>
        </div>
      </div>

      <div className="mt-4 grid gap-4 md:grid-cols-3">
        <PasswordInput
          label="Current password"
          value={currentPassword}
          autoComplete="current-password"
          onChange={setCurrentPassword}
        />
        <PasswordInput
          label="New password"
          value={newPassword}
          autoComplete="new-password"
          onChange={setNewPassword}
        />
        <PasswordInput
          label="Confirm password"
          value={confirmPassword}
          autoComplete="new-password"
          onChange={setConfirmPassword}
        />
      </div>

      {localError || passwordMutation.isError ? (
        <div className="mt-4 rounded-md border border-rose-400/30 bg-rose-950/20 p-3 text-sm text-rose-100">
          {localError ?? passwordMutation.error?.message ?? "Could not update password."}
        </div>
      ) : null}

      {passwordMutation.isSuccess ? (
        <div className="mt-4 rounded-md border border-emerald-400/30 bg-emerald-950/20 p-3 text-sm text-emerald-100">
          Password updated.
        </div>
      ) : null}

      <button
        type="submit"
        disabled={passwordMutation.isPending}
        className="mt-4 h-10 rounded-md bg-teal-400 px-4 text-sm font-semibold text-slate-950 transition hover:bg-teal-300 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {passwordMutation.isPending ? "Updating..." : "Update password"}
      </button>
    </form>
  );
}

function PasswordInput({
  label,
  value,
  autoComplete,
  onChange,
}: {
  label: string;
  value: string;
  autoComplete: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="block">
      <span className="text-sm font-medium text-slate-300">{label}</span>
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        type="password"
        autoComplete={autoComplete}
        className="mt-2 h-10 w-full rounded-md border border-white/10 bg-[#151b23] px-3 text-sm outline-none ring-teal-300/40 focus:ring-2"
      />
    </label>
  );
}

function StatusCard({
  title,
  status,
  rows,
  actionLabel,
  onAction,
}: {
  title: string;
  status: "ok" | "warning";
  rows: Array<[string, string]>;
  actionLabel?: string;
  onAction?: () => void;
}) {
  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d] p-4">
      <div className="flex items-start justify-between gap-3">
        <h2 className="text-base font-semibold text-slate-50">{title}</h2>
        <span
          className={cn(
            "rounded px-2 py-1 text-xs font-semibold uppercase",
            status === "ok" ? "bg-emerald-400/15 text-emerald-200" : "bg-amber-400/15 text-amber-200",
          )}
        >
          {status}
        </span>
      </div>
      <dl className="mt-4 space-y-3">
        {rows.map(([label, value]) => (
          <div key={label} className="grid gap-1 md:grid-cols-[160px_minmax(0,1fr)]">
            <dt className="text-xs uppercase text-slate-500">{label}</dt>
            <dd className="break-words text-sm text-slate-200">{value}</dd>
          </div>
        ))}
      </dl>
      {actionLabel && onAction ? (
        <button
          type="button"
          onClick={onAction}
          className="mt-4 h-9 rounded-md border border-white/10 px-3 text-sm text-slate-200 transition hover:bg-white/10"
        >
          {actionLabel}
        </button>
      ) : null}
    </div>
  );
}

function SegmentedControl({
  label,
  values,
  value,
  onChange,
  formatter,
}: {
  label: string;
  values: string[];
  value: string;
  onChange: (value: string) => void;
  formatter?: (value: string) => string;
}) {
  return (
    <div className="flex h-10 items-center rounded-md border border-white/10 bg-[#151b23] p-1" aria-label={label}>
      {values.map((item) => (
        <button
          key={item}
          type="button"
          onClick={() => onChange(item)}
          className={cn(
            "h-8 min-w-10 rounded px-3 text-xs font-medium uppercase transition",
            value === item ? "bg-teal-400 text-slate-950" : "text-slate-400 hover:text-slate-100",
          )}
        >
          {formatter ? formatter(item) : item}
        </button>
      ))}
    </div>
  );
}

function MetricGrid({
  data,
  loading,
  currency,
}: {
  data?: MarketDataResponse;
  loading: boolean;
  currency: Currency;
}) {
  const change = data?.current.priceChangePercentage24h ?? null;
  const isPositive = change != null && change >= 0;

  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <Metric
        label="Price"
        value={formatCurrency(data?.current.price, currency)}
        loading={loading}
        icon={Activity}
      />
      <Metric
        label="24h"
        value={change == null ? "n/a" : `${change.toFixed(2)}%`}
        loading={loading}
        icon={isPositive ? TrendingUp : TrendingDown}
        tone={isPositive ? "positive" : "negative"}
      />
      <Metric
        label="Market cap"
        value={formatNumber(data?.current.marketCap)}
        loading={loading}
        icon={Database}
      />
      <Metric
        label="Volume"
        value={formatNumber(data?.current.totalVolume)}
        loading={loading}
        icon={BarChart3}
      />
    </div>
  );
}

function Metric({
  label,
  value,
  loading,
  icon: Icon,
  tone = "neutral",
}: {
  label: string;
  value: string;
  loading: boolean;
  icon: React.ComponentType<{ className?: string }>;
  tone?: "neutral" | "positive" | "negative";
}) {
  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d] p-4">
      <div className="flex items-center justify-between">
        <span className="text-sm text-slate-400">{label}</span>
        <Icon
          className={cn(
            "size-4",
            tone === "positive" && "text-emerald-300",
            tone === "negative" && "text-rose-300",
            tone === "neutral" && "text-slate-500",
          )}
          aria-hidden="true"
        />
      </div>
      <div className="mt-3 h-8 text-2xl font-semibold text-slate-50">
        {loading ? <Skeleton className="h-8 w-32" /> : value}
      </div>
    </div>
  );
}

function PriceChart({
  data,
  indicators,
  loading,
  currency,
}: {
  data?: MarketDataResponse;
  indicators?: IndicatorsResponse;
  loading: boolean;
  currency: Currency;
}) {
  const chartData = useMemo(() => {
    const emaByTime = new Map(indicators?.ema20.map((point) => [point.timestamp, point.value]) ?? []);

    return (
      data?.history.map((point) => ({
        time: new Date(point.timestamp).toLocaleDateString("pt-PT", {
          day: "2-digit",
          month: "2-digit",
          hour: data.days <= 1 ? "2-digit" : undefined,
        }),
        price: point.price,
        ema20: emaByTime.get(point.timestamp) ?? null,
      })) ?? []
    );
  }, [data, indicators]);

  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d] p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-slate-50">Price chart</h2>
          <p className="mt-1 text-sm text-slate-400">
            {data ? `${data.name} - ${data.symbol.toUpperCase()}` : "Loading market"}
          </p>
        </div>
        <div className="text-xs uppercase text-slate-500">{currency}</div>
      </div>

      <div className="mt-4 h-[420px] min-h-[320px]">
        {loading ? (
          <Skeleton className="h-full w-full" />
        ) : chartData.length === 0 ? (
          <EmptyState />
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData} margin={{ left: 0, right: 12, top: 12, bottom: 0 }}>
              <defs>
                <linearGradient id="priceFill" x1="0" x2="0" y1="0" y2="1">
                  <stop offset="5%" stopColor="#2dd4bf" stopOpacity={0.28} />
                  <stop offset="95%" stopColor="#2dd4bf" stopOpacity={0.02} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="#22303d" vertical={false} />
              <XAxis dataKey="time" tickLine={false} axisLine={false} tick={{ fill: "#94a3b8", fontSize: 12 }} />
              <YAxis
                tickLine={false}
                axisLine={false}
                tick={{ fill: "#94a3b8", fontSize: 12 }}
                width={80}
                tickFormatter={(value) => formatNumber(Number(value))}
              />
              <Tooltip
                contentStyle={{
                  background: "#111827",
                  border: "1px solid rgba(255,255,255,.12)",
                  borderRadius: 8,
                  color: "#e5e7eb",
                }}
                formatter={(value, name) => [
                  formatCurrency(Number(value), currency),
                  name === "ema20" ? "EMA20" : "Price",
                ]}
              />
              <Area type="monotone" dataKey="price" stroke="#2dd4bf" fill="url(#priceFill)" strokeWidth={2} />
              <Line type="monotone" dataKey="ema20" stroke="#f59e0b" dot={false} strokeWidth={2} />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  );
}

function IndicatorPanel({
  data,
  loading,
  currency,
}: {
  data?: IndicatorsResponse;
  loading: boolean;
  currency: Currency;
}) {
  const rsiData = data?.rsi14.slice(-80).map((point) => ({
    time: new Date(point.timestamp).toLocaleTimeString("pt-PT", {
      hour: "2-digit",
      minute: "2-digit",
    }),
    rsi: point.value,
  }));

  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d] p-4">
      <h2 className="text-base font-semibold text-slate-50">Indicators</h2>

      <div className="mt-4 space-y-3">
        <IndicatorMetric label="SMA20" value={formatCurrency(data?.summary.latestSma20, currency)} loading={loading} />
        <IndicatorMetric label="EMA20" value={formatCurrency(data?.summary.latestEma20, currency)} loading={loading} />
        <IndicatorMetric
          label="RSI14"
          value={data?.summary.latestRsi14 == null ? "n/a" : data.summary.latestRsi14.toFixed(2)}
          badge={data?.summary.rsiSignal ?? undefined}
          loading={loading}
        />
      </div>

      <div className="mt-5 h-56">
        {loading ? (
          <Skeleton className="h-full w-full" />
        ) : !rsiData?.length ? (
          <EmptyState />
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <ReLineChart data={rsiData} margin={{ left: -20, right: 8, top: 8, bottom: 0 }}>
              <CartesianGrid stroke="#22303d" vertical={false} />
              <XAxis dataKey="time" tickLine={false} axisLine={false} tick={{ fill: "#94a3b8", fontSize: 11 }} />
              <YAxis domain={[0, 100]} tickLine={false} axisLine={false} tick={{ fill: "#94a3b8", fontSize: 11 }} />
              <Tooltip
                contentStyle={{
                  background: "#111827",
                  border: "1px solid rgba(255,255,255,.12)",
                  borderRadius: 8,
                  color: "#e5e7eb",
                }}
              />
              <Line type="monotone" dataKey="rsi" stroke="#a78bfa" dot={false} strokeWidth={2} />
            </ReLineChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  );
}

function IndicatorMetric({
  label,
  value,
  badge,
  loading,
}: {
  label: string;
  value: string;
  badge?: string;
  loading: boolean;
}) {
  return (
    <div className="flex h-16 items-center justify-between rounded-md border border-white/10 bg-[#151b23] px-3">
      <div>
        <div className="text-xs uppercase text-slate-500">{label}</div>
        <div className="mt-1 text-lg font-semibold text-slate-50">{loading ? <Skeleton className="h-6 w-24" /> : value}</div>
      </div>
      {badge ? (
        <span className="rounded bg-white/10 px-2 py-1 text-xs uppercase text-slate-300">{badge}</span>
      ) : null}
    </div>
  );
}

function AnalysisPanel({
  analysis,
  error,
  history,
  loading,
  currency,
  onAnalyze,
}: {
  analysis?: AnalysisResponse;
  error: Error | null;
  history?: Array<{
    id: string;
    createdAtUtc: string;
    riskLevel: "low" | "medium" | "high";
    analysis: AnalysisResponse;
  }>;
  loading: boolean;
  currency: Currency;
  onAnalyze: () => void;
}) {
  return (
    <div className="rounded-lg border border-white/10 bg-[#11161d] p-4">
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-semibold text-slate-50">AI analysis</h2>
          <p className="mt-1 text-sm text-slate-400">Structured technical analysis generated by the backend.</p>
        </div>
        <button
          type="button"
          onClick={onAnalyze}
          disabled={loading}
          className="inline-flex h-10 items-center justify-center gap-2 rounded-md bg-teal-400 px-4 text-sm font-semibold text-slate-950 transition hover:bg-teal-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Bot className="size-4" aria-hidden="true" />
          {loading ? "Analyzing" : "Analyze"}
        </button>
      </div>

      {error ? (
        <div className="mt-4 rounded-md border border-amber-300/30 bg-amber-950/20 p-3 text-sm text-amber-100">
          {error.message}
        </div>
      ) : null}

      {analysis ? (
        <div className="mt-5 grid gap-4 xl:grid-cols-[minmax(0,1fr)_340px]">
          <div className="space-y-4">
            <AnalysisBlock title="Summary" value={analysis.summary} />
            <AnalysisBlock title="Trend" value={analysis.trend} />
            <AnalysisBlock title="RSI" value={analysis.rsiComment} />
            <AnalysisBlock title="Possible entry zone" value={analysis.possibleEntryZone} />
            <AnalysisBlock title="Stop loss" value={analysis.stopLoss} />
          </div>
          <div className="space-y-3">
            <LevelList title="Support" levels={analysis.supportLevels} currency={currency} />
            <LevelList title="Resistance" levels={analysis.resistanceLevels} currency={currency} />
            <LevelList title="Targets" levels={analysis.takeProfitTargets} currency={currency} />
            <div className="rounded-md border border-white/10 bg-[#151b23] p-3">
              <div className="text-xs uppercase text-slate-500">Risk</div>
              <div className="mt-2 text-lg font-semibold uppercase text-slate-50">{analysis.riskLevel}</div>
            </div>
          </div>
          <div className="rounded-md border border-white/10 bg-[#151b23] p-3 text-xs text-slate-400 xl:col-span-2">
            {analysis.disclaimer}
          </div>
        </div>
      ) : !error ? (
        <AnalysisHistoryPreview history={history} />
      ) : null}
    </div>
  );
}

function AnalysisHistoryPreview({
  history,
}: {
  history?: Array<{
    id: string;
    createdAtUtc: string;
    riskLevel: "low" | "medium" | "high";
    analysis: AnalysisResponse;
  }>;
}) {
  if (!history?.length) {
    return (
      <div className="mt-4 rounded-md border border-white/10 bg-[#151b23] p-4 text-sm text-slate-400">
        AI analysis is generated only when requested.
      </div>
    );
  }

  return (
    <div className="mt-4 rounded-md border border-white/10 bg-[#151b23] p-4">
      <div className="text-xs uppercase text-slate-500">Recent analyses</div>
      <div className="mt-3 space-y-3">
        {history.map((item) => (
          <div key={item.id} className="border-t border-white/10 pt-3 first:border-t-0 first:pt-0">
            <div className="flex items-center justify-between gap-3">
              <div className="text-sm font-medium text-slate-200">{item.analysis.summary}</div>
              <span className="rounded bg-white/10 px-2 py-1 text-xs uppercase text-slate-300">
                {item.riskLevel}
              </span>
            </div>
            <div className="mt-1 text-xs text-slate-500">
              {new Date(item.createdAtUtc).toLocaleString("pt-PT")}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function AnalysisBlock({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-md border border-white/10 bg-[#151b23] p-3">
      <div className="text-xs uppercase text-slate-500">{title}</div>
      <div className="mt-2 text-sm leading-6 text-slate-200">{value}</div>
    </div>
  );
}

function LevelList({
  title,
  levels,
  currency,
}: {
  title: string;
  levels: number[];
  currency: Currency;
}) {
  return (
    <div className="rounded-md border border-white/10 bg-[#151b23] p-3">
      <div className="text-xs uppercase text-slate-500">{title}</div>
      <div className="mt-2 flex flex-wrap gap-2">
        {levels.length === 0 ? (
          <span className="text-sm text-slate-500">n/a</span>
        ) : (
          levels.map((level) => (
            <span key={`${title}-${level}`} className="rounded bg-white/10 px-2 py-1 text-xs text-slate-200">
              {formatCurrency(level, currency)}
            </span>
          ))
        )}
      </div>
    </div>
  );
}

function ErrorState() {
  return (
    <div className="rounded-lg border border-rose-400/30 bg-rose-950/20 p-5 text-rose-100">
      <div className="font-semibold">Backend unavailable</div>
      <div className="mt-1 text-sm text-rose-200/80">Check that the API is running on http://localhost:6002.</div>
    </div>
  );
}

function EmptyState() {
  return <div className="grid h-full place-items-center text-sm text-slate-500">No data</div>;
}

function Skeleton({ className }: { className?: string }) {
  return <div className={cn("animate-pulse rounded-md bg-white/10", className)} />;
}
