import type { ExtractedFeedback } from "@/types/feedback";
import { extractionPrompt } from "./llm/prompt-templates";
import { runJsonModel } from "./llm/router";

const customerTypes = ["Enterprise", "Mid Market", "SMB"] as const;
const sentiments = ["Positive", "Neutral", "Negative"] as const;
const urgencies = ["Low", "Medium", "High"] as const;

export async function extractFeedback(
  feedback: string
): Promise<ExtractedFeedback> {
  const extracted = await runJsonModel<Partial<ExtractedFeedback>>(
    extractionPrompt(feedback)
  );

  const featureRequest = cleanFeature(extracted.featureRequest);
  const customerType = normalizeChoice(extracted.customerType, customerTypes);
  const sentiment = normalizeChoice(extracted.sentiment, sentiments);
  const urgency = normalizeChoice(extracted.urgency, urgencies);

  return {
    featureRequest,
    customerType,
    sentiment,
    urgency
  };
}

function cleanFeature(value: unknown) {
  if (typeof value !== "string" || !value.trim()) {
    throw new Error("Extraction did not include a feature request.");
  }

  return value.trim().replace(/\s+/g, " ");
}

function normalizeChoice<T extends readonly string[]>(
  value: unknown,
  choices: T
): T[number] {
  if (typeof value !== "string") {
    throw new Error("Extraction returned an invalid classification.");
  }

  const normalized = choices.find(
    (choice) => choice.toLowerCase() === value.trim().toLowerCase()
  );

  if (!normalized) {
    throw new Error(`Extraction returned an unsupported value: ${value}`);
  }

  return normalized;
}
