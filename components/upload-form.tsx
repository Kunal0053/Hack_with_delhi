"use client";

import { ChangeEvent, FormEvent, useEffect, useState } from "react";
import type { FeedbackMemory } from "@/types/memory";
import { MemoryTimeline } from "./memory-timeline";

type UploadResponse = {
  memories?: FeedbackMemory[];
  error?: string;
};

export function UploadForm() {
  const [feedback, setFeedback] = useState("");
  const [memories, setMemories] = useState<FeedbackMemory[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    void loadMemories();
  }, []);

  async function loadMemories() {
    try {
      const response = await fetch("/api/upload-feedback");
      const data = (await response.json()) as UploadResponse;

      if (data.memories) {
        setMemories(data.memories);
      }
    } catch {
      setError("Memory timeline could not be loaded.");
    }
  }

  async function submitFeedback(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await fetch("/api/upload-feedback", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ feedback })
      });
      const data = (await response.json()) as UploadResponse;

      if (!response.ok) {
        throw new Error(data.error ?? "Feedback could not be stored.");
      }

      setMemories(data.memories ?? []);
      setFeedback("");
    } catch (caught) {
      setError(
        caught instanceof Error ? caught.message : "Feedback could not be stored."
      );
    } finally {
      setLoading(false);
    }
  }

  async function handleCsvUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];

    if (!file) {
      return;
    }

    const text = await file.text();
    setFeedback(parseCsvFeedback(text));
  }

  async function clearStoredMemories() {
    setLoading(true);
    setError("");

    try {
      const response = await fetch("/api/upload-feedback", {
        method: "DELETE"
      });
      const data = (await response.json()) as UploadResponse;

      if (!response.ok) {
        throw new Error(data.error ?? "Memories could not be cleared.");
      }

      setMemories([]);
    } catch (caught) {
      setError(
        caught instanceof Error ? caught.message : "Memories could not be cleared."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <form
        onSubmit={submitFeedback}
        className="rounded-md border border-line bg-white p-5"
      >
        <label
          htmlFor="feedback"
          className="block text-sm font-medium text-ink"
        >
          Customer Feedback
        </label>
        <textarea
          id="feedback"
          value={feedback}
          onChange={(event) => setFeedback(event.target.value)}
          rows={8}
          className="mt-2 w-full resize-none rounded-md border border-line bg-white px-3 py-3 text-sm leading-6 outline-none focus:border-accent focus:ring-2 focus:ring-blue-100"
          placeholder="Paste a customer note, support ticket, call transcript, or churn-risk comment."
        />
        <div className="mt-4 flex items-center justify-between gap-4">
          <input
            type="file"
            accept=".csv,text/csv"
            onChange={handleCsvUpload}
            className="block text-sm text-muted file:mr-3 file:rounded-md file:border file:border-line file:bg-[#F4F2EC] file:px-3 file:py-2 file:text-sm file:text-ink"
          />
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={clearStoredMemories}
              disabled={loading || memories.length === 0}
              className="rounded-md border border-line bg-white px-4 py-2 text-sm font-medium text-muted disabled:cursor-not-allowed disabled:opacity-50"
            >
              Clear Memories
            </button>
            <button
              type="submit"
              disabled={loading || !feedback.trim()}
              className="rounded-md bg-accent px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-50"
            >
              {loading ? "Storing..." : "Store In Memory"}
            </button>
          </div>
        </div>
        {error ? <p className="mt-3 text-sm text-red-700">{error}</p> : null}
      </form>
      <MemoryTimeline memories={memories} />
    </>
  );
}

function parseCsvFeedback(csv: string) {
  const rows = csv
    .split(/\r?\n/)
    .map((row) => row.trim())
    .filter(Boolean);

  if (rows.length === 0) {
    return "";
  }

  const dataRows = hasHeaderRow(rows[0]) ? rows.slice(1) : rows;

  return dataRows
    .map((row) =>
      parseCsvRow(row)
        .filter(Boolean)
        .join(" ")
    )
    .filter(Boolean)
    .join("\n");
}

function hasHeaderRow(row: string) {
  return /feedback|customer|sentiment|urgency|feature/i.test(row);
}

function parseCsvRow(row: string) {
  const cells = row.match(/("([^"]|"")*"|[^,]+)/g) ?? [];

  return cells.map((cell) =>
    cell
      .trim()
      .replace(/^"|"$/g, "")
      .replace(/""/g, "\"")
      .trim()
  );
}
