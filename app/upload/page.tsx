import { UploadForm } from "@/components/upload-form";

export default function UploadPage() {
  return (
    <div className="mx-auto max-w-4xl">
      <header className="mb-6 border-b border-line pb-5">
        <h1 className="text-2xl font-semibold tracking-normal text-ink">
          Feedback Upload
        </h1>
        <p className="mt-2 text-sm text-muted">
          Extract feature signals from customer feedback and store them as
          product memories.
        </p>
      </header>
      <UploadForm />
    </div>
  );
}
