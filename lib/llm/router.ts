import { runOpenRouterJson } from "./openrouter";

export async function runJsonModel<T>(prompt: string): Promise<T> {
  return runOpenRouterJson<T>(prompt);
}
