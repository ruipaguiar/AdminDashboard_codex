import { AuthGate } from "@/components/auth-gate";
import { QueryProvider } from "@/components/query-provider";

export default function Home() {
  return (
    <QueryProvider>
      <AuthGate />
    </QueryProvider>
  );
}
