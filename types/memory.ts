import type { ExtractedFeedback } from "./feedback";

export type FeedbackMemory = ExtractedFeedback & {
  id: string;
  rawFeedback: string;
  createdAt: string;
};
