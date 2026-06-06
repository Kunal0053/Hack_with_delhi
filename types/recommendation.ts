export type RecommendationReport = {
  recommendedFeature: string;
  buildScore: number;
  evidence: string[];
  confidence: "Low" | "Medium" | "High";
};
