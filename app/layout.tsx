import type { Metadata } from "next";
import "./globals.css";
import { Sidebar } from "@/components/sidebar";

export const metadata: Metadata = {
  title: "BuildNext",
  description: "AI product manager for customer feedback memories"
};

const isDev = process.env.NODE_ENV === "development";

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        {isDev && (
          <script
            suppressHydrationWarning
            dangerouslySetInnerHTML={{
              __html: `
                window.addEventListener('error', (event) => {
                  const error = event.error;
                  const message = event.message || '';
                  const filename = event.filename || '';
                  const stack = (error && error.stack) || '';
                  
                  if (
                    filename.indexOf('chrome-extension://') !== -1 ||
                    stack.indexOf('chrome-extension://') !== -1 ||
                    filename.indexOf('blob:') === 0 ||
                    stack.indexOf('blob:') !== -1 ||
                    message.indexOf("reading 'addListener'") !== -1
                  ) {
                    event.stopImmediatePropagation();
                    event.preventDefault();
                  }
                }, true);

                window.addEventListener('unhandledrejection', (event) => {
                  const reason = event.reason;
                  const message = (reason && reason.message) || '';
                  const stack = (reason && reason.stack) || '';
                  
                  if (
                    stack.indexOf('chrome-extension://') !== -1 ||
                    stack.indexOf('blob:') !== -1 ||
                    message.indexOf("reading 'addListener'") !== -1
                  ) {
                    event.stopImmediatePropagation();
                    event.preventDefault();
                  }
                }, true);
              `
            }}
          />
        )}
      </head>
      <body
        className="min-h-screen bg-canvas font-sans text-ink antialiased"
        suppressHydrationWarning
      >
        <div className="flex min-h-screen">
          <Sidebar />
          <main className="flex-1 px-10 py-8">{children}</main>
        </div>
      </body>
    </html>
  );
}
