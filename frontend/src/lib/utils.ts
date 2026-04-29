import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatCurrency(value: number | null | undefined, currency: string) {
  if (value == null) {
    return "n/a";
  }

  return new Intl.NumberFormat("pt-PT", {
    style: "currency",
    currency: currency.toUpperCase(),
    maximumFractionDigits: value >= 100 ? 0 : 4,
  }).format(value);
}

export function formatNumber(value: number | null | undefined) {
  if (value == null) {
    return "n/a";
  }

  return new Intl.NumberFormat("pt-PT", {
    maximumFractionDigits: 2,
    notation: value >= 1_000_000 ? "compact" : "standard",
  }).format(value);
}
