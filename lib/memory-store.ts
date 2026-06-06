import { mkdir, readFile, unlink, writeFile } from "node:fs/promises";
import path from "node:path";
import { storeFeedbackMemory } from "@/lib/hindsight";
import type { FeedbackMemory } from "@/types/memory";

const DATA_DIR = path.join(process.cwd(), "data");
const MEMORY_FILE = path.join(DATA_DIR, "buildnext-memories.json");

export async function saveMemory(memory: FeedbackMemory) {
  const memories = await readLocalMemories();
  const nextMemories = dedupeMemories([memory, ...memories]);

  await writeLocalMemories(nextMemories);

  try {
    await storeFeedbackMemory(memory);
    return { syncedToHindsight: true };
  } catch (error) {
    return {
      syncedToHindsight: false,
      syncError:
        error instanceof Error ? error.message : "Hindsight sync failed."
    };
  }
}

export async function saveMemories(newMemories: FeedbackMemory[]) {
  const memories = await readLocalMemories();
  const nextMemories = dedupeMemories([...newMemories, ...memories]);

  await writeLocalMemories(nextMemories);

  const syncPromises = newMemories.map(async (memory) => {
    try {
      await storeFeedbackMemory(memory);
      return { syncedToHindsight: true };
    } catch (error) {
      return {
        syncedToHindsight: false,
        syncError:
          error instanceof Error ? error.message : "Hindsight sync failed."
      };
    }
  });

  return Promise.all(syncPromises);
}

export async function getMemories() {
  return readLocalMemories();
}

export async function clearMemories() {
  try {
    await unlink(MEMORY_FILE);
  } catch {
    return;
  }
}

async function readLocalMemories(): Promise<FeedbackMemory[]> {
  try {
    const file = await readFile(MEMORY_FILE, "utf8");
    const parsed = JSON.parse(file) as FeedbackMemory[];

    if (Array.isArray(parsed)) {
      return parsed.filter(isFeedbackMemory).sort(sortNewestFirst);
    }
  } catch {
    return [];
  }

  return [];
}

async function writeLocalMemories(memories: FeedbackMemory[]) {
  await mkdir(DATA_DIR, { recursive: true });
  await writeFile(
    MEMORY_FILE,
    JSON.stringify(memories.sort(sortNewestFirst), null, 2),
    "utf8"
  );
}

function dedupeMemories(memories: FeedbackMemory[]) {
  const seen = new Set<string>();
  const deduped: FeedbackMemory[] = [];

  for (const memory of memories.sort(sortNewestFirst)) {
    const key = memory.id || `${memory.featureRequest}-${memory.createdAt}`;

    if (!seen.has(key)) {
      seen.add(key);
      deduped.push(memory);
    }
  }

  return deduped;
}

function isFeedbackMemory(value: unknown): value is FeedbackMemory {
  if (!value || typeof value !== "object") {
    return false;
  }

  const memory = value as Partial<FeedbackMemory>;

  return Boolean(
    memory.id &&
      memory.featureRequest &&
      memory.customerType &&
      memory.sentiment &&
      memory.urgency &&
      memory.createdAt
  );
}

function sortNewestFirst(a: FeedbackMemory, b: FeedbackMemory) {
  return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
}
