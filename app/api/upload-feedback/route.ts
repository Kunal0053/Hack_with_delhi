import { NextResponse } from "next/server";
import { extractFeedback } from "@/lib/extraction";
import { clearMemories, getMemories, saveMemories } from "@/lib/memory-store";
import type { UploadFeedbackRequest } from "@/types/feedback";
import type { FeedbackMemory } from "@/types/memory";

export const runtime = "nodejs";

export async function GET() {
  try {
    const memories = await getMemories();
    return NextResponse.json({ memories });
  } catch (error) {
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Unable to load memories." },
      { status: 500 }
    );
  }
}

export async function POST(request: Request) {
  try {
    const body = (await request.json()) as UploadFeedbackRequest;
    const feedback = body.feedback?.trim();

    if (!feedback) {
      return NextResponse.json(
        { error: "Feedback is required." },
        { status: 400 }
      );
    }

    const feedbackItems = splitFeedbackItems(feedback);

    // Extract all feedback items in parallel
    const saved = await Promise.all(
      feedbackItems.map(async (item) => {
        const extracted = await extractFeedback(item);
        return {
          id: crypto.randomUUID(),
          rawFeedback: item,
          createdAt: new Date().toISOString(),
          ...extracted
        };
      })
    );

    // Save and sync all items in parallel
    const syncResults = await saveMemories(saved);

    const memories = await getMemories();
    return NextResponse.json({
      memory: saved[0],
      memories,
      sync: syncResults,
      storedCount: saved.length
    });
  } catch (error) {
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Unable to store memory." },
      { status: 500 }
    );
  }
}

export async function DELETE() {
  try {
    await clearMemories();
    return NextResponse.json({ memories: [] });
  } catch (error) {
    return NextResponse.json(
      {
        error:
          error instanceof Error ? error.message : "Unable to clear memories."
      },
      { status: 500 }
    );
  }
}

function splitFeedbackItems(feedback: string) {
  const items = feedback
    .split(/\r?\n/)
    .map((item) => item.trim())
    .filter(Boolean);

  return items.length > 1 ? items : [feedback];
}
