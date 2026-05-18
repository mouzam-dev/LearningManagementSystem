/** "MCQ" | "TrueFalse" | "ShortAnswer" | "Essay". */
export type BankQuestionType = 'MCQ' | 'TrueFalse' | 'ShortAnswer' | 'Essay';

/** Compact row used on the bank list page. */
export interface BankQuestionListItem {
  id: string;
  questionText: string;
  type: BankQuestionType | string;
  points: number;
  tag?: string | null;
  optionCount: number;
  createdAt: string;
  updatedAt: string;
}

/** Full payload for the editor + import picker preview. */
export interface BankQuestion {
  id: string;
  questionText: string;
  type: BankQuestionType | string;
  options?: string[] | null;
  correctAnswer?: string | null;
  points: number;
  tag?: string | null;
  createdAt: string;
  updatedAt: string;
}

/** Body for create + update endpoints. */
export interface BankQuestionUpsertBody {
  questionText: string;
  type: BankQuestionType | string;
  options?: string[] | null;
  correctAnswer?: string | null;
  points: number;
  tag?: string | null;
}

export interface BankQuestionFilter {
  search?: string;
  tag?: string;
  page?: number;
  pageSize?: number;
}
