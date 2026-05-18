export interface RubricListItem {
  id: string;
  name: string;
  description?: string | null;
  criterionCount: number;
  totalPoints: number;
  createdAt: string;
  updatedAt: string;
}

export interface RubricCriterion {
  id: string;
  name: string;
  description?: string | null;
  maxPoints: number;
  order: number;
}

export interface Rubric {
  id: string;
  name: string;
  description?: string | null;
  totalPoints: number;
  criteria: RubricCriterion[];
  createdAt: string;
  updatedAt: string;
}

/** Input shape for create + update. Server preserves the array order. */
export interface RubricCriterionInput {
  name: string;
  description?: string | null;
  maxPoints: number;
}

export interface RubricUpsertBody {
  name: string;
  description?: string | null;
  criteria: RubricCriterionInput[];
}

export interface RubricFilter {
  search?: string;
  page?: number;
  pageSize?: number;
}
