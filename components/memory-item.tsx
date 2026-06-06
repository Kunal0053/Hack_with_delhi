import type { FeedbackMemory } from "@/types/memory";

export function MemoryItem({ memory }: { memory: FeedbackMemory }) {
  return (
    <article className="rounded-md border border-line bg-white px-4 py-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h3 className="text-sm font-semibold text-ink">
            {memory.featureRequest}
          </h3>
          <div className="mt-2 flex flex-wrap gap-2 text-xs">
            <span className="rounded border border-line px-2 py-1 text-muted">
              {memory.customerType} Customer
            </span>
            <span className="rounded border border-line px-2 py-1 text-muted">
              {memory.sentiment} Sentiment
            </span>
            <span className="rounded border border-line px-2 py-1 text-muted">
              {memory.urgency} Urgency
            </span>
          </div>
        </div>
        <time className="whitespace-nowrap text-xs text-muted">
          {formatRelativeTime(memory.createdAt)}
        </time>
      </div>
      <p className="mt-3 line-clamp-2 text-sm leading-6 text-muted">
        {memory.rawFeedback}
      </p>
    </article>
  );
}

function formatRelativeTime(value: string) {
  const then = new Date(value).getTime();
  const now = Date.now();
  const seconds = Math.max(1, Math.floor((now - then) / 1000));

  if (seconds < 60) {
    return `${seconds}s ago`;
  }

  const minutes = Math.floor(seconds / 60);

  if (minutes < 60) {
    return `${minutes}m ago`;
  }

  const hours = Math.floor(minutes / 60);

  if (hours < 24) {
    return `${hours}h ago`;
  }

  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}
