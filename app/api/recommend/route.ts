import { NextResponse } from "next/server";
import { getMemories } from "@/lib/memory-store";
import { recommendFromMemories } from "@/lib/recommendation";
import { runJsonModel } from "@/lib/llm/router";
import { answerQuestionPrompt } from "@/lib/llm/prompt-templates";

export const runtime = "nodejs";

type QAResponse = {
  recommendedFeature?: string;
  buildScore?: number;
  evidence?: string[];
  confidence?: "Low" | "Medium" | "High";
  error?: string;
};

export async function POST(request: Request) {
  try {
    const body = await request.json().catch(() => ({}));
    const question = body.question?.trim();
    const memories = await getMemories();

    if (question) {
      if (memories.length === 0) {
        return NextResponse.json(
          { error: "No memories found. Upload feedback first." },
          { status: 400 }
        );
      }

      const rawResult = await runJsonModel<QAResponse>(
        answerQuestionPrompt(memories, question)
      );

      if (rawResult.error) {
        return NextResponse.json(
          { error: rawResult.error },
          { status: 400 }
        );
      }

      if (
        !rawResult.recommendedFeature ||
        rawResult.buildScore === undefined ||
        !rawResult.evidence
      ) {
        return NextResponse.json(
          { error: "No relevant data found." },
          { status: 400 }
        );
      }

      return NextResponse.json({ report: rawResult, memories });
    }

    const report = recommendFromMemories(memories);
    return NextResponse.json({ report, memories });
  } catch (error) {
    return NextResponse.json(
      {
        error:
          error instanceof Error
            ? error.message
            : "Unable to process request."
      },
      { status: 500 }
    );
  }
}
