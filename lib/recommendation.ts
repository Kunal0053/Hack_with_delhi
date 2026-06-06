import type { FeedbackMemory } from "@/types/memory";
import type { RecommendationReport } from "@/types/recommendation";

type FeatureStats = {
  feature: string;
  mentions: number;
  enterprise: number;
  midMarket: number;
  smb: number;
  churnRisk: number;
};

export function recommendFromMemories(
  memories: FeedbackMemory[]
): RecommendationReport {
  if (memories.length === 0) {
    throw new Error("No memories found. Upload feedback first.");
  }

  const grouped = groupByFeature(memories);
  const stats = Array.from(grouped.values());
  const maxMentions = Math.max(...stats.map((item) => item.mentions));
  const maxImportance = Math.max(...stats.map(customerImportance));
  const maxChurnRisk = Math.max(...stats.map((item) => item.churnRisk), 1);

  const ranked = stats
    .map((item) => {
      const frequencyScore = item.mentions / maxMentions;
      const importanceScore = customerImportance(item) / maxImportance;
      const churnScore = item.churnRisk / maxChurnRisk;

      return {
        ...item,
        score: Math.round(
          (frequencyScore * 0.5 + importanceScore * 0.3 + churnScore * 0.2) *
            100
        )
      };
    })
    .sort((a, b) => b.score - a.score);

  const winner = ranked[0];

  return {
    recommendedFeature: winner.feature,
    buildScore: winner.score,
    evidence: [
      `Mentioned ${winner.mentions} ${pluralize("time", winner.mentions)}`,
      `Requested by ${winner.enterprise} enterprise ${pluralize(
        "customer",
        winner.enterprise
      )}`,
      `Appeared in ${winner.churnRisk} churn-risk ${pluralize(
        "conversation",
        winner.churnRisk
      )}`
    ],
    confidence: confidenceFor(winner.score, memories.length)
  };
}

function groupByFeature(memories: FeedbackMemory[]) {
  const grouped = new Map<string, FeatureStats>();

  for (const memory of memories) {
    const key = memory.featureRequest.trim().toLowerCase();
    const existing = grouped.get(key) ?? {
      feature: memory.featureRequest,
      mentions: 0,
      enterprise: 0,
      midMarket: 0,
      smb: 0,
      churnRisk: 0
    };

    existing.mentions += 1;

    if (memory.customerType === "Enterprise") {
      existing.enterprise += 1;
    }

    if (memory.customerType === "Mid Market") {
      existing.midMarket += 1;
    }

    if (memory.customerType === "SMB") {
      existing.smb += 1;
    }

    if (memory.sentiment === "Negative" && memory.urgency === "High") {
      existing.churnRisk += 1;
    }

    grouped.set(key, existing);
  }

  return grouped;
}

function customerImportance(stats: FeatureStats) {
  return stats.enterprise * 3 + stats.midMarket * 2 + stats.smb;
}

function confidenceFor(score: number, memoryCount: number) {
  if (score >= 80 && memoryCount >= 3) {
    return "High";
  }

  if (score >= 60) {
    return "Medium";
  }

  return "Low";
}

function pluralize(word: string, count: number) {
  return count === 1 ? word : `${word}s`;
}
