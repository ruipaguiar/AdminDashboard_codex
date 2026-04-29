"use client";

import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Coins, Lock } from "lucide-react";
import { DashboardShell } from "@/components/dashboard-shell";
import { getCsrfToken, getCurrentUser, login, logout } from "@/lib/api";

export function AuthGate() {
  const queryClient = useQueryClient();
  const currentUserQuery = useQuery({
    queryKey: ["auth-user"],
    queryFn: getCurrentUser,
    retry: false,
  });
  const logoutMutation = useMutation({
    mutationFn: logout,
    onSettled: async () => {
      await queryClient.clear();
    },
  });

  if (currentUserQuery.isLoading) {
    return (
      <div className="grid min-h-screen place-items-center bg-[#0c0f14] text-slate-100">
        <div className="h-10 w-10 animate-spin rounded-full border-2 border-teal-300 border-t-transparent" />
      </div>
    );
  }

  if (!currentUserQuery.data) {
    return (
      <LoginScreen
        onLoggedIn={async () => {
          await currentUserQuery.refetch();
        }}
      />
    );
  }

    return (
      <DashboardShell
        currentUserEmail={currentUserQuery.data.email}
      onLogout={() => logoutMutation.mutate()}
    />
  );
}

function LoginScreen({ onLoggedIn }: { onLoggedIn: () => Promise<void> }) {
  const [email, setEmail] = useState("ruipaguiar@gmail.com");
  const [password, setPassword] = useState("");
  const loginMutation = useMutation({
    mutationFn: () => login(email, password),
    onSuccess: async () => {
      await getCsrfToken();
      await onLoggedIn();
    },
  });

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    loginMutation.mutate();
  };

  return (
    <main className="grid min-h-screen place-items-center bg-[#0c0f14] px-4 text-slate-100">
      <form
        onSubmit={onSubmit}
        className="w-full max-w-sm rounded-lg border border-white/10 bg-[#11161d] p-6 shadow-2xl shadow-black/30"
      >
        <div className="flex items-center gap-3">
          <div className="grid size-10 place-items-center rounded-md bg-teal-400 text-slate-950">
            <Coins className="size-5" aria-hidden="true" />
          </div>
          <div>
            <h1 className="text-lg font-semibold text-slate-50">AdminDashBoard</h1>
            <p className="text-sm text-slate-400">Local crypto analytics</p>
          </div>
        </div>

        <div className="mt-6 space-y-4">
          <label className="block">
            <span className="text-sm font-medium text-slate-300">Email</span>
            <input
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              autoComplete="email"
              className="mt-2 h-11 w-full rounded-md border border-white/10 bg-[#151b23] px-3 text-sm outline-none ring-teal-300/40 focus:ring-2"
            />
          </label>

          <label className="block">
            <span className="text-sm font-medium text-slate-300">Password</span>
            <input
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              autoComplete="current-password"
              className="mt-2 h-11 w-full rounded-md border border-white/10 bg-[#151b23] px-3 text-sm outline-none ring-teal-300/40 focus:ring-2"
            />
          </label>
        </div>

        {loginMutation.isError ? (
          <div className="mt-4 rounded-md border border-rose-400/30 bg-rose-950/20 p-3 text-sm text-rose-100">
            Invalid email or password.
          </div>
        ) : null}

        <button
          type="submit"
          disabled={loginMutation.isPending}
          className="mt-6 flex h-11 w-full items-center justify-center gap-2 rounded-md bg-teal-400 px-4 text-sm font-semibold text-slate-950 transition hover:bg-teal-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Lock className="size-4" aria-hidden="true" />
          {loginMutation.isPending ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </main>
  );
}
