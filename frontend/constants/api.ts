const API_BASE = process.env.NEXT_PUBLIC_API_BASE?.replace(/\/$/, '') ?? '';

export const API_ENDPOINTS = {
  INCIDENTS: `${API_BASE}/api/incidents`,
  IMPORT_INCIDENTS: `${API_BASE}/api/incidents/import`,
  INSIGHTS_TRENDS: `${API_BASE}/api/insights/trends`,
  INSIGHTS_TOP_CATEGORIES: `${API_BASE}/api/insights/top-categories`,
  INSIGHTS_ADMIN_SUMMARY: `${API_BASE}/api/insights/admin-summary`,
  INSIGHTS_ANOMALIES: `${API_BASE}/api/insights/anomalies`,
} as const;


