"use client";

import { FormEvent, useState } from "react";
import { DecisionReport } from "@/components/decision-report";
import type { RecommendationReport } from "@/types/recommendation";

type RecommendResponse = {
  report?: RecommendationReport;
  error?: string;
};

const SUGGESTIONS = [
  "What should we build next?",
  "What features are enterprise users requesting?",
  "List all integrations requested and their sentiment.",
  "Which requests are flagged as negative sentiment and high urgency?"
];

export default function AgentPage() {
  const [question, setQuestion] = useState("What should we build next?");
  const [report, setReport] = useState<RecommendationReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!question.trim()) return;

    setLoading(true);
    setError("");
    setReport(null);

    try {
      const response = await fetch("/api/recommend", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ question: question.trim() })
      });
      const data = (await response.json()) as RecommendResponse;

      if (!response.ok) {
        throw new Error(data.error ?? "Recommendation could not be generated.");
      }

      setReport(data.report ?? null);
    } catch (caught) {
      setReport(null);
      setError(
        caught instanceof Error
          ? caught.message
          : "Recommendation could not be generated."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-4xl">
      <header className="mb-6 border-b border-line pb-5">
        <h1 className="text-2xl font-semibold tracking-normal text-ink">
          BuildNext Agent
        </h1>
        <p className="mt-2 text-sm text-muted">
          Query client feedback memories and receive answers directly from the agent.
        </p>
      </header>

      <form
        onSubmit={handleSubmit}
        className="rounded-md border border-line bg-white p-5 shadow-sm"
      >
        <label
          htmlFor="question"
          className="block text-sm font-medium text-ink"
        >
          Ask the Agent
        </label>
        <textarea
          id="question"
          value={question}
          onChange={(event) => setQuestion(event.target.value)}
          rows={3}
          className="mt-2 w-full resize-none rounded-md border border-line bg-white px-3 py-3 text-sm leading-6 outline-none focus:border-accent focus:ring-2 focus:ring-blue-100 transition-shadow"
          placeholder="Ask a question about feature requests, segments, or priorities..."
        />

        <div className="mt-3">
          <span className="text-xs font-medium text-muted">Suggestions:</span>
          <div className="mt-1 flex flex-wrap gap-2">
            {SUGGESTIONS.map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setQuestion(item)}
                className="rounded border border-line bg-[#FAFAF8] px-2.5 py-1 text-xs font-normal text-muted hover:border-accent hover:bg-white hover:text-ink transition-colors"
              >
                {item}
              </button>
            ))}
          </div>
        </div>

        <div className="mt-4 flex justify-end">
          <button
            type="submit"
            disabled={loading || !question.trim()}
            className="rounded-md bg-accent px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-50 transition-opacity"
          >
            {loading ? "Analyzing..." : "Ask Agent"}
          </button>
        </div>
      </form>

      {error ? (
        <p className="mt-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      ) : null}

      <DecisionReport report={report} />
    </div>
  );
}
