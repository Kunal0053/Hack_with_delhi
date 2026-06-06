import type { FeedbackMemory } from "@/types/memory";

const BASE_URL = "https://api.hindsight.vectorize.io";
const BANK_ID = process.env.HINDSIGHT_BANK_ID ?? "buildnext-feedback";

type HindsightRecallResponse = {
  results?: Array<{
    id?: string;
    text?: string;
    content?: string;
    context?: string;
    created_at?: string;
    createdAt?: string;
  }>;
};

type HindsightListResponse = {
  memories?: Array<{
    id?: string;
    text?: string;
    content?: string;
    context?: string;
    created_at?: string;
    createdAt?: string;
  }>;
  results?: HindsightRecallResponse["results"];
  items?: HindsightRecallResponse["results"];
};

export async function storeFeedbackMemory(memory: FeedbackMemory) {
  assertHindsightConfigured();

  const content = serializeMemory(memory);

  await requestHindsight(`/v1/default/banks/${BANK_ID}/memories`, {
    method: "POST",
    body: JSON.stringify({
      items: [
        {
          content,
          context: "BuildNext customer feedback memory"
        }
      ]
    })
  });
}

export async function retrieveFeedbackMemories(): Promise<FeedbackMemory[]> {
  if (!isHindsightConfigured()) {
    return [];
  }

  const listed = await listMemories();

  if (listed.length > 0) {
    return listed;
  }

  const recalled = await recallMemories();
  return recalled;
}

async function listMemories() {
  const response = await requestHindsight(
    `/v1/default/banks/${BANK_ID}/memories/list`,
    { method: "GET" }
  );
  const data = (await response.json()) as HindsightListResponse;
  const rows = data.memories ?? data.results ?? data.items ?? [];

  return normalizeHindsightRows(rows);
}

async function recallMemories() {
  const response = await requestHindsight(
    `/v1/default/banks/${BANK_ID}/memories/recall`,
    {
      method: "POST",
      body: JSON.stringify({
        query: "BuildNext customer feedback feature requests"
      })
    }
  );
  const data = (await response.json()) as HindsightRecallResponse;

  return normalizeHindsightRows(data.results ?? []);
}

async function requestHindsight(path: string, init: RequestInit) {
  const apiKey = assertHindsightConfigured();

  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/json",
      ...(init.headers ?? {})
    }
  });

  if (!response.ok) {
    const errorBody = await response.text();
    throw new Error(`Hindsight request failed: ${response.status} ${errorBody}`);
  }

  return response;
}

export function isHindsightConfigured() {
  return Boolean(process.env.HINDSIGHT_API_KEY);
}

function assertHindsightConfigured() {
  const apiKey = process.env.HINDSIGHT_API_KEY;

  if (!apiKey) {
    throw new Error("Missing HINDSIGHT_API_KEY. Add it to .env.local.");
  }

  return apiKey;
}

function serializeMemory(memory: FeedbackMemory) {
  return `BuildNext feedback memory\n${JSON.stringify(memory)}`;
}

function normalizeHindsightRows(
  rows: NonNullable<HindsightRecallResponse["results"]>
) {
  return rows
    .map((row) => parseMemory(row.content ?? row.text ?? row.context ?? ""))
    .filter((memory): memory is FeedbackMemory => Boolean(memory))
    .sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
}

function parseMemory(content: string): FeedbackMemory | null {
  const jsonStart = content.indexOf("{");

  if (jsonStart === -1) {
    return null;
  }

  try {
    const parsed = JSON.parse(content.slice(jsonStart)) as FeedbackMemory;

    if (
      parsed.featureRequest &&
      parsed.customerType &&
      parsed.sentiment &&
      parsed.urgency &&
      parsed.createdAt
    ) {
      return parsed;
    }
  } catch {
    return null;
  }

  return null;
}
