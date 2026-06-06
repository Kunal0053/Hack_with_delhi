import type { FeedbackMemory } from "@/types/memory";

export function extractionPrompt(feedback: string) {
  return [
    "Extract one product feedback memory from the customer feedback.",
    "Return strict JSON only with these fields:",
    "{",
    "  \"featureRequest\": \"short feature name\",",
    "  \"customerType\": \"Enterprise\" | \"Mid Market\" | \"SMB\",",
    "  \"sentiment\": \"Positive\" | \"Neutral\" | \"Negative\",",
    "  \"urgency\": \"Low\" | \"Medium\" | \"High\"",
    "}",
    "",
    "Rules:",
    "- Use the clearest requested product capability as featureRequest.",
    "- Choose Enterprise for large companies, security/compliance/procurement, admins, or many seats.",
    "- Choose Mid Market for teams/departments/growing companies.",
    "- Choose SMB for founders, small teams, freelancers, or local businesses.",
    "- High urgency means churn risk, blocked rollout, repeated pain, deadlines, or angry tone.",
    "",
    "Customer feedback:",
    feedback
  ].join("\n");
}

export function recommendationPrompt(memories: FeedbackMemory[]) {
  const payload = memories.map((memory) => ({
    featureRequest: memory.featureRequest,
    customerType: memory.customerType,
    sentiment: memory.sentiment,
    urgency: memory.urgency,
    rawFeedback: memory.rawFeedback
  }));

  return [
    "Recommend the single product feature to build next from these feedback memories.",
    "Use this formula: Build Score = 50% Frequency + 30% Customer Importance + 20% Churn Risk.",
    "Customer Importance weight: Enterprise > Mid Market > SMB.",
    "Churn Risk means Negative sentiment plus High urgency.",
    "Return strict JSON only:",
    "{",
    "  \"recommendedFeature\": \"feature name\",",
    "  \"buildScore\": 0,",
    "  \"evidence\": [\"Mentioned X times\", \"Requested by Y enterprise customers\", \"Appeared in Z churn-risk conversations\"],",
    "  \"confidence\": \"Low\" | \"Medium\" | \"High\"",
    "}",
    "",
    JSON.stringify(payload)
  ].join("\n");
}

export function answerQuestionPrompt(memories: FeedbackMemory[], question: string) {
  const payload = memories.map((memory) => ({
    featureRequest: memory.featureRequest,
    customerType: memory.customerType,
    sentiment: memory.sentiment,
    urgency: memory.urgency,
    rawFeedback: memory.rawFeedback
  }));

  return [
    "You are a precise AI product analyst. Your task is to answer the user's question using the provided customer feedback memories list.",
    "",
    "Format your answer strictly as a Recommendation Report JSON object with the following fields:",
    "{",
    "  \"recommendedFeature\": \"A clear, concise title/topic summarizing the answer to the user's question (e.g. 'Approval Workflows' or 'Negative Urgency Reports')\",",
    "  \"buildScore\": a calculated relevance or importance score (number from 0 to 100) based on the relevant feedback data,",
    "  \"evidence\": a list of 1 to 5 short strings describing specific details, facts, or counts from the customer feedback memories supporting this,",
    "  \"confidence\": \"Low\" | \"Medium\" | \"High\" based on the strength and quantity of relevant data found in the memories",
    "}",
    "",
    "Rules:",
    "1. Answer the question using ONLY the facts and data explicitly found in the feedback memories list.",
    "2. Do not invent details. Base counts and sentiments strictly on the input list.",
    "3. If the user's question is irrelevant to product feedback/customer requests, or asks about general knowledge not covered in the memories (e.g. 'what is the capital of France' or 'write a poem about cats'), or if the feedback data does not contain enough information to answer the question, you MUST return exactly:",
    "   { \"error\": \"No relevant data found.\" }",
    "4. Do not return any other text or explanation outside the JSON object.",
    "",
    "User Question:",
    question,
    "",
    "Feedback Memories List:",
    JSON.stringify(payload)
  ].join("\n");
}
