'use client';

import Link from "next/link";
import { usePathname } from "next/navigation";
import { StatCard, StatusBadge } from "@/components/incident";
import { formatTimestamp, toInputDate } from "@/utils/incident";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

const API_BASE =
  process.env.NEXT_PUBLIC_API_BASE?.replace(/\/$/, "");
const PAGE_SIZE = 10;

type Incident = {
  id: string;
  description: string;
  timestamp: string;
  location: string;
  category: string;
  status: string;
};

type IncidentFormState = {
  description: string;
  timestamp: string;
  location: string;
  category: string;
  status: string;
};

const statusOptions = ["open", "in_progress", "resolved", "closed"];

const defaultFormState = (): IncidentFormState => ({
  description: "",
  timestamp: new Date().toISOString().slice(0, 16),
  location: "",
  category: "",
  status: "open",
});

export default function IncidentsPage() {
  const pathname = usePathname();
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [form, setForm] = useState<IncidentFormState>(defaultFormState());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [fetching, setFetching] = useState(true);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [importing, setImporting] = useState(false);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const fetchIncidents = useCallback(async (pageToLoad: number = 1) => {
    try {
      setFetching(true);
      setError(null);
      const skip = (pageToLoad - 1) * PAGE_SIZE;
      const res = await fetch(`${API_BASE}/api/incidents?skip=${skip}&take=${PAGE_SIZE}`, {
        cache: "no-store",
      });
      if (!res.ok) {
        throw new Error(`Failed to load incidents (${res.status})`);
      }
      const data = await res.json();
      setIncidents(data?.items ?? []);
      setTotal(data?.total ?? 0);
      setPage(pageToLoad);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setFetching(false);
    }
  }, []);

  useEffect(() => {
    fetchIncidents(1);
  }, [fetchIncidents]);

  const resetForm = () => {
    setForm(defaultFormState());
    setEditingId(null);
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);

    const payload = {
      description: form.description.trim(),
      location: form.location.trim(),
      category: form.category.trim(),
      status: form.status,
      timestamp: new Date(form.timestamp).toISOString(),
    };

    const url = editingId
      ? `${API_BASE}/api/incidents/${editingId}`
      : `${API_BASE}/api/incidents`;
    const method = editingId ? "PUT" : "POST";

    try {
      const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        const detail = await res.text();
        throw new Error(detail || "API error");
      }

      await fetchIncidents(1);
      setMessage(editingId ? "Incident updated" : "Incident created");
      resetForm();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unexpected error");
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = (incident: Incident) => {
    setEditingId(incident.id);
    setForm({
      description: incident.description,
      location: incident.location,
      category: incident.category,
      status: incident.status,
      timestamp: toInputDate(incident.timestamp),
    });
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const handleDelete = async (incident: Incident) => {
    const confirmed = window.confirm(
      `Delete incident "${incident.description.slice(0, 40)}"?`
    );
    if (!confirmed) return;

    try {
      const res = await fetch(`${API_BASE}/api/incidents/${incident.id}`, {
        method: "DELETE",
      });
      if (!res.ok) {
        throw new Error("Failed to delete incident");
      }
      setMessage("Incident deleted");
      await fetchIncidents(page);
      if (editingId === incident.id) {
        resetForm();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Delete failed");
    }
  };

  const stats = useMemo(() => {
    const totalCount = total;
    const open = incidents.filter((i) => i.status === "open").length;
    const resolved = incidents.filter((i) => i.status === "resolved").length;
    return { total: totalCount, open, resolved };
  }, [incidents, total]);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const pageStart = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const pageEnd = total === 0 ? 0 : Math.min(page * PAGE_SIZE, total);

  const handlePageChange = (nextPage: number) => {
    if (nextPage < 1 || nextPage > totalPages) return;
    fetchIncidents(nextPage);
  };

  const handleImportClick = () => {
    fileInputRef.current?.click();
  };

  const handleImportChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }
    setImporting(true);
    setError(null);
    setMessage(null);
    try {
      const formData = new FormData();
      formData.append("file", file);
      const res = await fetch(`${API_BASE}/api/incidents/import`, {
        method: "POST",
        body: formData,
      });
      if (!res.ok) {
        const detail = await res.text();
        throw new Error(detail || "Import failed");
      }
      const result = await res.json();
      setMessage(`Imported ${result.imported ?? 0} incidents`);
      await fetchIncidents(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Import failed");
    } finally {
      setImporting(false);
      event.target.value = "";
    }
  };

  return (
    <div className="min-h-screen bg-zinc-50 text-zinc-900">
      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-6xl flex-col gap-4 px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-xs uppercase tracking-wide text-zinc-500">
              Waste Incident Reporter
            </p>
            <h1 className="text-xl font-semibold">Operations Console</h1>
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
        <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatCard title="Total incidents" value={stats.total} accent="bg-blue-100 text-blue-800" />
          <StatCard title="Open" value={stats.open} accent="bg-amber-100 text-amber-800" />
          <StatCard title="Resolved" value={stats.resolved} accent="bg-emerald-100 text-emerald-800" />
        </section>

        {(message || error) && (
          <div
            className={`mt-6 rounded-lg border px-4 py-3 text-sm ${
              error
                ? "border-red-200 bg-red-50 text-red-800"
                : "border-emerald-200 bg-emerald-50 text-emerald-800"
            }`}
          >
            {error ?? message}
          </div>
        )}

        <section className="mt-8 flex flex-col gap-6 lg:flex-row">
          <div className="rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm lg:w-[360px]">
            <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="text-xs uppercase tracking-wide text-zinc-500">
                  Incident form
                </p>
                <h2 className="text-lg font-semibold">
                  {editingId ? "Update incident" : "Report new incident"}
                </h2>
              </div>
              {editingId && (
                <button
                  type="button"
                  className="text-sm font-medium text-blue-600 hover:text-blue-700"
                  onClick={resetForm}
                >
                  + New
                </button>
              )}
            </div>

            <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
              <label className="text-sm font-medium text-zinc-700">
                Description
                <textarea
                  className="mt-1 w-full rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm focus:border-blue-500 focus:bg-white focus:outline-none"
                  placeholder="Describe what happened..."
                  rows={3}
                  required
                  value={form.description}
                  onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
                />
              </label>

              <div className="grid gap-4 sm:grid-cols-2">
                <label className="text-sm font-medium text-zinc-700">
                  Location
                  <input
                    className="mt-1 w-full rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm focus:border-blue-500 focus:bg-white focus:outline-none"
                    placeholder="e.g., Pier 14"
                    value={form.location}
                    onChange={(e) => setForm((prev) => ({ ...prev, location: e.target.value }))}
                  />
                </label>

                <label className="text-sm font-medium text-zinc-700">
                  Category
                  <input
                    className="mt-1 w-full rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm focus:border-blue-500 focus:bg-white focus:outline-none"
                    placeholder="e.g., Chemical spill"
                    value={form.category}
                    onChange={(e) => setForm((prev) => ({ ...prev, category: e.target.value }))}
                  />
                </label>
              </div>

              <label className="text-sm font-medium text-zinc-700">
                Status
                <select
                  className="mt-1 w-full rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm focus:border-blue-500 focus:bg-white focus:outline-none"
                  value={form.status}
                  onChange={(e) => setForm((prev) => ({ ...prev, status: e.target.value }))}
                >
                  {statusOptions.map((status) => (
                    <option key={status} value={status}>
                      {status.replace("_", " ")}
                    </option>
                  ))}
                </select>
              </label>

              <label className="text-sm font-medium text-zinc-700">
                Timestamp
                <input
                  type="datetime-local"
                  className="mt-1 w-full rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm focus:border-blue-500 focus:bg-white focus:outline-none"
                  value={form.timestamp}
                  onChange={(e) => setForm((prev) => ({ ...prev, timestamp: e.target.value }))}
                />
              </label>

              <button
                type="submit"
                disabled={loading}
                className="mt-2 inline-flex items-center justify-center rounded-full bg-blue-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:opacity-60"
              >
                {loading ? "Saving..." : editingId ? "Save changes" : "Create incident"}
              </button>
            </form>
          </div>

          <div className="rounded-2xl border border-zinc-200 bg-white p-4 shadow-sm lg:flex-1">
            <div className="flex flex-col gap-3 border-b border-zinc-100 pb-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="text-xs uppercase tracking-wide text-zinc-500">Live feed</p>
                <h2 className="text-lg font-semibold">Recent incidents</h2>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <button
                  onClick={() => fetchIncidents(page)}
                  className="inline-flex items-center justify-center rounded-full border border-zinc-200 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50"
                >
                  Refresh
                </button>
                <button
                  type="button"
                  onClick={handleImportClick}
                  disabled={importing}
                  className="inline-flex items-center justify-center rounded-full border border-blue-200 px-4 py-2 text-sm font-medium text-blue-700 hover:bg-blue-50 disabled:opacity-60"
                >
                  {importing ? "Importing..." : "Import CSV"}
                </button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv,text/csv"
                  className="hidden"
                  onChange={handleImportChange}
                />
              </div>
            </div>

            <div className="mt-4 overflow-x-auto">
              <table className="min-w-full text-left text-sm">
                <thead className="text-xs uppercase tracking-wide text-zinc-500">
                  <tr>
                    <th className="py-2 pr-4">Incident</th>
                    <th className="px-4 py-2">Location</th>
                    <th className="px-4 py-2">Category</th>
                    <th className="px-4 py-2">Status</th>
                    <th className="px-4 py-2 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {fetching && (
                    <tr>
                      <td colSpan={5} className="py-6 text-center text-zinc-500">
                        Loading incidents...
                      </td>
                    </tr>
                  )}
                  {!fetching && incidents.length === 0 && (
                    <tr>
                      <td colSpan={5} className="py-6 text-center text-zinc-500">
                        No incidents reported yet.
                      </td>
                    </tr>
                  )}
                  {incidents.map((incident) => (
                    <tr
                      key={incident.id}
                      className="border-t border-zinc-100 text-zinc-800 hover:bg-zinc-50"
                    >
                      <td className="py-3 pr-4">
                        <p className="font-medium">{incident.description}</p>
                        <p className="text-xs text-zinc-500">
                          {formatTimestamp(incident.timestamp)}
                        </p>
                      </td>
                      <td className="px-4 py-3">{incident.location || "—"}</td>
                      <td className="px-4 py-3">{incident.category || "—"}</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={incident.status} />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-2">
                          <button
                            className="text-sm font-medium text-blue-600 hover:text-blue-800"
                            onClick={() => handleEdit(incident)}
                          >
                            Edit
                          </button>
                          <button
                            className="text-sm font-medium text-red-600 hover:text-red-800"
                            onClick={() => handleDelete(incident)}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="mt-4 flex flex-col gap-3 border-t border-zinc-100 pt-4 text-sm text-zinc-600 sm:flex-row sm:items-center sm:justify-between">
              <p>
                {total === 0
                  ? "No incidents to display"
                  : `Showing ${pageStart}-${pageEnd} of ${total} incidents`}
              </p>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => handlePageChange(page - 1)}
                  disabled={page <= 1 || fetching}
                  className="rounded-full border border-zinc-200 px-3 py-1 text-xs font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50"
                >
                  Prev
                </button>
                <span className="text-xs font-medium">
                  Page {Math.min(page, totalPages)} of {totalPages}
                </span>
                <button
                  type="button"
                  onClick={() => handlePageChange(page + 1)}
                  disabled={page >= totalPages || fetching}
                  className="rounded-full border border-zinc-200 px-3 py-1 text-xs font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
