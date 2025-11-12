export function StatusBadge({ status }: { status: string }) {
    const palette: Record<string, string> = {
        open: 'bg-amber-100 text-amber-800',
        in_progress: 'bg-blue-100 text-blue-800',
        resolved: 'bg-emerald-100 text-emerald-800',
        closed: 'bg-zinc-200 text-zinc-800',
    };
    return (
        <span
            className={`inline-flex rounded-full px-3 py-1 text-xs font-medium capitalize ${
                palette[status] ?? 'bg-zinc-100 text-zinc-700'
            }`}
        >
            {status.replace('_', ' ')}
        </span>
    );
}



export function StatCard({
    title,
    value,
    accent,
}: {
    title: string;
    value: number;
    accent: string;
}) {
    return (
        <div className="rounded-2xl border border-zinc-200 bg-white p-4 shadow-sm">
            <p className="text-xs uppercase tracking-wide text-zinc-500">
                {title}
            </p>
            <div className="mt-2 flex items-end justify-between">
                <span className="text-3xl font-semibold">{value}</span>
                <span
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${accent}`}
                >
                    {title === 'Total incidents' ? 'All time' : 'Live'}
                </span>
            </div>
        </div>
    );
}

