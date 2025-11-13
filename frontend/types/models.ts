export type Incident = {
  id: string;
  description: string;
  timestamp: string;
  location: string;
  category: string;
  status: string;
};

export type IncidentFormState = {
  description: string;
  timestamp: string;
  location: string;
  category: string;
  status: string;
};

export type TrendPoint = { day: string; count: number };
export type TrendResponse = {
  data: TrendPoint[];
  lastDayZScore?: number;
  spike?: boolean;
};

export type CategoryRecord = { category: string; count: number };
export type AnomalyRecord = {
  day: string;
  count: number;
  zScore: number;
  isAnomaly: boolean;
};

export type SimilarIncident = Incident & { similarity: number };
