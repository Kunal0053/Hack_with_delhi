"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const navItems = [
  { href: "/upload", label: "Upload Feedback" },
  { href: "/agent", label: "Ask Agent" }
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="min-h-screen w-64 border-r border-line bg-[#F4F2EC] px-5 py-6">
      <div className="mb-8">
        <p className="text-sm font-semibold tracking-normal text-ink">BuildNext</p>
        <p className="mt-1 text-xs text-muted">AI Product Manager</p>
      </div>
      <nav className="space-y-1">
        {navItems.map((item) => {
          const active = pathname === item.href;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={[
                "block rounded-md px-3 py-2 text-sm transition-colors",
                active
                  ? "bg-white text-accent ring-1 ring-line"
                  : "text-muted hover:bg-white hover:text-ink"
              ].join(" ")}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
