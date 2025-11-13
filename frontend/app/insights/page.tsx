'use client';

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { BarChart, LineChart } from "@/components/insight";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE?.replace(/\/$/, "");
const WATCH_PAGE_SIZE = 5;

type TrendPoint = { day: string; count: number };
type TrendResponse = { data: TrendPoint[]; lastDayZScore?: number; spike?: boolean };
type CategoryRecord = { category: string; count: number };
type AnomalyRecord = { day: string; count: number; zScore: number; isAnomaly: boolean };

const formatDay = (day: string) =>
  new Date(day).toLocaleDateString(undefined, { month: "short", day: "numeric" });


export default function InsightsPage() {
  const pathname = usePathname();
  const [trend, setTrend] = useState<{ label: string; value: number }[]>([]);
  const [trendMeta, setTrendMeta] = useState<{ lastDayZScore?: number; spike?: boolean }>({});
  const [trendHighlight, setTrendHighlight] = useState<{ label: string; value: number } | null>(null);
  const [categories, setCategories] = useState<CategoryRecord[]>([]);
  const [summary, setSummary] = useState("");
  const [anomalies, setAnomalies] = useState<AnomalyRecord[]>([]);
  const [watchPage, setWatchPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchInsights = async () => {
      try {
        setLoading(true);
        setError(null);
        const [trendRes, topRes, summaryRes, anomalyRes] = await Promise.all([
          fetch(`${API_BASE}/api/insights/trends`, { cache: "no-store" }),
          fetch(`${API_BASE}/api/insights/top-categories`, { cache: "no-store" }),
          fetch(`${API_BASE}/api/insights/admin-summary`, { cache: "no-store" }),
          fetch(`${API_BASE}/api/insights/anomalies?days=60`, { cache: "no-store" }),
        ]);

        if (!trendRes.ok || !topRes.ok || !summaryRes.ok || !anomalyRes.ok) {
          throw new Error("Failed to load insights");
        }

        const trendJson: TrendResponse = await trendRes.json();
        const topJson: CategoryRecord[] = await topRes.json();
        const summaryJson: { summary: string } = await summaryRes.json();
        const anomalyJson: AnomalyRecord[] = await anomalyRes.json();

        setTrend(
          (trendJson.data ?? []).map((pt) => ({
            label: formatDay(pt.day),
            value: pt.count,
          }))
        );
        setTrendMeta({ lastDayZScore: trendJson.lastDayZScore, spike: trendJson.spike });
        setCategories(topJson ?? []);
        setSummary(summaryJson.summary ?? "");
        setAnomalies(anomalyJson ?? []);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load insights");
      } finally {
        setLoading(false);
      }
    };

    fetchInsights();
  }, []);

  const headline = useMemo(() => {
    if (trend.length === 0) return "Not enough data yet";
    const activePoint = trendHighlight ?? trend[trend.length - 1];
    const status = trendMeta.spike ? "Spike detected" : "Stable";
    return `${status}: ${activePoint.value} incidents on ${activePoint.label}`;
  }, [trend, trendMeta, trendHighlight]);

  const watchTotalPages = Math.max(1, Math.ceil(anomalies.length / WATCH_PAGE_SIZE));
  const pagedAnomalies = anomalies.slice(
    (watchPage - 1) * WATCH_PAGE_SIZE,
    watchPage * WATCH_PAGE_SIZE
  );

  return (
    <div className="min-h-screen bg-zinc-50 text-zinc-900">
      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div>
            <p className="text-xs uppercase tracking-wide text-zinc-500">Waste Incident Reporter</p>
            <h1 className="text-xl font-semibold">Insights & Trends</h1>
          </div>
          <nav className="flex gap-4 text-sm font-medium text-zinc-600">
            {[
              { label: "Incidents", href: "/" },
              { label: "Insights", href: "/insights" },
            ].map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={
                  pathname === item.href
                    ? "text-zinc-900"
                    : "hover:text-zinc-900"
                }
              >
                {item.label}
              </Link>
            ))}
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        {loading && (
          <div className="mb-6 rounded-lg border border-zinc-200 bg-white px-4 py-3 text-sm text-zinc-600">
            Loading insights...
          </div>
        )}

        {error && (
          <div className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {!loading && !error && (
          <div className="space-y-8">
            <section className="rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm">
              <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-xs uppercase tracking-wide text-zinc-500">Daily trend</p>
                  <h2 className="text-lg font-semibold">{headline}</h2>
                </div>
                {typeof trendMeta.lastDayZScore === "number" && (
                  <span
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${
                      (trendMeta.spike ?? false)
                        ? "bg-red-100 text-red-700"
                        : "bg-emerald-100 text-emerald-700"
                    }`}
                  >
                    Z-score {trendMeta.lastDayZScore.toFixed(2)}
                  </span>
                )}
              </div>
              <LineChart data={trend} onHover={setTrendHighlight} />
            </section>

            <section className="grid gap-6 lg:grid-cols-2">
              <div className="rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm">
                <div className="mb-4">
                  <p className="text-xs uppercase tracking-wide text-zinc-500">Top categories</p>
                  <h2 className="text-lg font-semibold">Most common waste types</h2>
                </div>
                <BarChart
                  data={categories.map((c) => ({
                    label: c.category,
                    value: c.count,
                  }))}
                />
              </div>

              <div className="rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm">
                <p className="text-xs uppercase tracking-wide text-zinc-500">Summary</p>
                <h2 className="text-lg font-semibold">AI-generated admin note</h2>
                <p className="mt-4 text-sm leading-6 text-zinc-700">
                  {summary || "No summary available yet."}
                </p>
              </div>
            </section>

            <section className="rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm">
              <div className="mb-4 flex items-center justify-between">
                <div>
                  <p className="text-xs uppercase tracking-wide text-zinc-500">Anomalies</p>
                  <h2 className="text-lg font-semibold">Watchlist</h2>
                </div>
              </div>
              {anomalies.length === 0 ? (
                <p className="text-sm text-zinc-500">No anomalies detected in the selected window.</p>
              ) : (
                <>
                  <div className="space-y-3">
                    {pagedAnomalies.map((item) => (
                      <div
                        key={item.day}
                        className="flex flex-col gap-1 rounded-xl border border-zinc-100 bg-zinc-50 px-4 py-3 text-sm text-zinc-700 sm:flex-row sm:items-center sm:justify-between"
                      >
                        <div>
                          <p className="font-semibold text-zinc-900">{formatDay(item.day)}</p>
                          <p className="text-xs text-zinc-500">z-score {item.zScore.toFixed(2)}</p>
                        </div>
                        <div className="flex items-center gap-4">
                          <span className="text-sm">{item.count} incidents</span>
                          {item.isAnomaly && (
                            <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-700">
                              Spike
                            </span>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                  {anomalies.length > WATCH_PAGE_SIZE && (
                    <div className="mt-4 flex items-center justify-between text-xs text-zinc-600">
                      <span>
                        Page {watchPage} of {watchTotalPages}
                      </span>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => setWatchPage((p) => Math.max(1, p - 1))}
                          disabled={watchPage <= 1}
                          className="rounded-full border border-zinc-200 px-3 py-1 font-medium hover:bg-zinc-50 disabled:opacity-50"
                        >
                          Prev
                        </button>
                        <button
                          onClick={() => setWatchPage((p) => Math.min(watchTotalPages, p + 1))}
                          disabled={watchPage >= watchTotalPages}
                          className="rounded-full border border-zinc-200 px-3 py-1 font-medium hover:bg-zinc-50 disabled:opacity-50"
                        >
                          Next
                        </button>
                      </div>
                    </div>
                  )}
                </>
              )}
            </section>
          </div>
        )}
      </main>
    </div>
  );
}
