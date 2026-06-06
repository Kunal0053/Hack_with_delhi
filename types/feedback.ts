export type CustomerType = "Enterprise" | "Mid Market" | "SMB";
export type Sentiment = "Positive" | "Neutral" | "Negative";
export type Urgency = "Low" | "Medium" | "High";

export type ExtractedFeedback = {
  featureRequest: string;
  customerType: CustomerType;
  sentiment: Sentiment;
  urgency: Urgency;
};

export type UploadFeedbackRequest = {
  feedback: string;
};
