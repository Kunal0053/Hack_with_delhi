import type { RecommendationReport } from "@/types/recommendation";

export function DecisionReport({
  report
}: {
  report: RecommendationReport | null;
}) {
  if (!report) {
    return (
      <section className="mt-6 rounded-md border border-dashed border-line bg-white px-5 py-8 text-sm text-muted">
        The recommendation report will appear after the agent reviews memories.
      </section>
    );
  }

  return (
    <section className="mt-6 rounded-md border border-line bg-white">
      <div className="border-b border-line px-5 py-4">
        <h2 className="text-sm font-semibold text-ink">Recommendation Report</h2>
      </div>
      <div className="grid grid-cols-[180px_1fr] gap-x-8 gap-y-5 px-5 py-5 text-sm">
        <div className="text-muted">Recommended Feature</div>
        <div className="font-semibold text-ink">{report.recommendedFeature}</div>
        <div className="text-muted">Build Score</div>
        <div className="text-2xl font-semibold text-ink">{report.buildScore}</div>
        <div className="text-muted">Evidence</div>
        <ul className="space-y-2 text-ink">
          {report.evidence.map((item) => (
            <li key={item} className="flex gap-2">
              <span className="mt-2 h-1.5 w-1.5 rounded-full bg-accent" />
              <span>{item}</span>
            </li>
          ))}
        </ul>
        <div className="text-muted">Confidence</div>
        <div className="font-medium text-ink">{report.confidence}</div>
      </div>
    </section>
  );
}
