'use client';

import { useState } from "react";

export type TrendDatum = { label: string; value: number };

const CHART_WIDTH = 600;
const CHART_HEIGHT = 220;
const CHART_PADDING = 24;

export function LineChart({
  data,
  onHover,
}: {
  data: TrendDatum[];
  onHover?: (point: TrendDatum | null) => void;
}) {
  const [hovered, setHovered] = useState<number | null>(null);

  if (data.length === 0) {
    return <p className="text-sm text-zinc-500">Not enough data yet.</p>;
  }

  const maxValue = Math.max(...data.map((d) => d.value), 1);
  const coords = data.map((point, idx) => {
    const x =
      CHART_PADDING +
      (idx / Math.max(1, data.length - 1)) * (CHART_WIDTH - CHART_PADDING * 2);
    const y =
      CHART_HEIGHT -
      CHART_PADDING -
      (point.value / maxValue) * (CHART_HEIGHT - CHART_PADDING * 2);
    return { x, y };
  });

  const polyPoints = coords.map((p) => `${p.x},${p.y}`).join(" ");

  const handleMove = (event: React.MouseEvent<SVGRectElement, MouseEvent>) => {
    const bounds = event.currentTarget.getBoundingClientRect();
    const relativeX = event.clientX - bounds.left;
    const ratio = Math.min(
      1,
      Math.max(
        0,
        (relativeX - CHART_PADDING) / Math.max(1, bounds.width - CHART_PADDING * 2)
      )
    );
    const idx = Math.round(ratio * (data.length - 1));
    setHovered(idx);
    onHover?.(data[idx]);
  };

  return (
    <div className="relative">
      <svg viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`} className="w-full">
        <polyline
          fill="none"
          stroke="#2563eb"
          strokeWidth={3}
          strokeLinejoin="round"
          strokeLinecap="round"
          points={polyPoints}
        />
        {coords.map((point, idx) => (
          <circle
            key={`${data[idx].label}-${idx}`}
            cx={point.x}
            cy={point.y}
            r={hovered === idx ? 6 : 4}
            fill={hovered === idx ? "#1d4ed8" : "#2563eb"}
          />
        ))}
        <rect
          x={CHART_PADDING}
          y={0}
          width={CHART_WIDTH - CHART_PADDING * 2}
          height={CHART_HEIGHT}
          fill="transparent"
          onMouseMove={handleMove}
          onMouseLeave={() => {
            setHovered(null);
            onHover?.(null);
          }}
        />
      </svg>

      {hovered !== null && coords[hovered] && (
        <div
          className="pointer-events-none absolute rounded-lg border border-zinc-200 bg-white px-3 py-2 text-xs shadow-lg"
          style={{
            left: `${(coords[hovered].x / CHART_WIDTH) * 100}%`,
            top: `${(coords[hovered].y / CHART_HEIGHT) * 100}%`,
            transform: "translate(-50%, -120%)",
          }}
        >
          <p className="font-semibold text-zinc-900">{data[hovered].label}</p>
          <p className="text-zinc-600">{data[hovered].value} incidents</p>
        </div>
      )}
    </div>
  );
}

export function BarChart({
  data,
}: {
  data: { label: string; value: number }[];
}) {
  if (data.length === 0) {
    return <p className="text-sm text-zinc-500">No category data yet.</p>;
  }

  const maxValue = Math.max(...data.map((d) => d.value), 1);

  return (
    <div className="flex flex-col gap-3">
      {data.map((item) => (
        <div key={item.label}>
          <div className="flex items-center justify-between text-sm">
            <span className="font-medium text-zinc-800">
              {item.label || "Unknown"}
            </span>
            <span className="text-zinc-500">{item.value}</span>
          </div>
          <div className="mt-1 h-2 rounded-full bg-zinc-100">
            <div
              className="h-2 rounded-full bg-emerald-500"
              style={{ width: `${(item.value / maxValue) * 100}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}
