import type { FeedbackMemory } from "@/types/memory";
import { MemoryItem } from "./memory-item";

export function MemoryTimeline({ memories }: { memories: FeedbackMemory[] }) {
  return (
    <section className="mt-8">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-ink">Memory Timeline</h2>
        <span className="text-xs text-muted">{memories.length} memories</span>
      </div>
      {memories.length === 0 ? (
        <div className="rounded-md border border-dashed border-line bg-white px-4 py-8 text-sm text-muted">
          Stored feedback memories will appear here.
        </div>
      ) : (
        <div className="space-y-3">
          {memories.map((memory) => (
            <MemoryItem key={memory.id} memory={memory} />
          ))}
        </div>
      )}
    </section>
  );
}
