type OpenRouterMessage = {
  role: "system" | "user";
  content: string;
};

type OpenRouterResponse = {
  choices?: Array<{
    message?: {
      content?: string;
    };
  }>;
};

const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";
const MODEL = "google/gemini-2.5-flash";

export async function runOpenRouterJson<T>(prompt: string): Promise<T> {
  const apiKey = process.env.OPENROUTER_API_KEY;

  if (!apiKey) {
    throw new Error("Missing OPENROUTER_API_KEY.");
  }

  const messages: OpenRouterMessage[] = [
    {
      role: "system",
      content:
        "You are a precise product analyst. Return valid JSON only. No markdown."
    },
    {
      role: "user",
      content: prompt
    }
  ];

  const response = await fetch(OPENROUTER_URL, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/json",
      "HTTP-Referer": "http://localhost:3000",
      "X-Title": "BuildNext"
    },
    body: JSON.stringify({
      model: MODEL,
      messages,
      temperature: 0.1,
      max_tokens: 1000,
      response_format: { type: "json_object" }
    })
  });

  if (!response.ok) {
    const errorBody = await response.text();
    throw new Error(`OpenRouter request failed: ${response.status} ${errorBody}`);
  }

  const data = (await response.json()) as any;

  if (data.error) {
    const errMsg = data.error.message || JSON.stringify(data.error);
    throw new Error(`OpenRouter error: ${errMsg}`);
  }

  const content = data.choices?.[0]?.message?.content;

  if (!content) {
    throw new Error(`OpenRouter returned an empty response. Raw response: ${JSON.stringify(data)}`);
  }

  return JSON.parse(content) as T;
}
