import { DashboardShell } from "@/components/dashboard-shell";
import { QueryProvider } from "@/components/query-provider";

export default function Home() {
  return (
    <QueryProvider>
      <DashboardShell />
    </QueryProvider>
  );
}
