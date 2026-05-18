import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { BankQuestion, BankQuestionUpsertBody } from '../models/bank-question.models';
import { TeacherService } from '../teacher.service';

interface DraftQuestion {
  questionText: string;
  type: string;
  options: string[];
  correctAnswer: string;
  points: number;
  tag: string;
}

const QUESTION_TYPES = [
  { value: 'MCQ', label: 'Multiple choice' },
  { value: 'TrueFalse', label: 'True / false' },
  { value: 'ShortAnswer', label: 'Short answer' },
  { value: 'Essay', label: 'Essay' },
];

/**
 * Single editor used for both create (/teacher/question-bank/new) and edit
 * (/teacher/question-bank/:id) — the route param differentiates. Fields shown
 * are conditional on the selected question type, matching the assessment
 * editor's question form.
 */
@Component({
  selector: 'app-teacher-question-bank-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './question-bank-edit.html',
})
export class TeacherQuestionBankEditPage {
  private readonly service = inject(TeacherService);
  private readonly router = inject(Router);

  /** Present when editing an existing bank question; absent for "new". */
  readonly id = input<string | undefined>(undefined);

  readonly isEdit = computed(() => !!this.id());
  readonly types = QUESTION_TYPES;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly fieldErrors = signal<Record<string, string[]>>({});

  readonly draft = signal<DraftQuestion>(emptyDraft());

  readonly requiresOptions = computed(() => this.draft().type === 'MCQ');
  readonly requiresCorrectAnswer = computed(() =>
    this.draft().type === 'MCQ' || this.draft().type === 'TrueFalse');

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) this.load(id);
      else this.draft.set(emptyDraft());
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.service.getBankQuestion(id).subscribe({
      next: (q) => {
        this.draft.set(toDraft(q));
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.loadError.set(err.status === 404
          ? "That question doesn't exist or isn't yours."
          : err.status === 0 ? 'Cannot reach the API.' : 'Failed to load question.');
      },
    });
  }

  // --- Editing helpers ----------------------------------------------------

  update<K extends keyof DraftQuestion>(field: K, value: DraftQuestion[K]): void {
    const cur = this.draft();
    const next = { ...cur, [field]: value };

    // Switching to True/False auto-populates the option set so the correct-
    // answer dropdown always has something to choose from.
    if (field === 'type') {
      if (value === 'MCQ' && next.options.length < 2) next.options = ['', ''];
      if (value === 'TrueFalse') {
        next.options = ['True', 'False'];
        if (next.correctAnswer !== 'True' && next.correctAnswer !== 'False') next.correctAnswer = '';
      }
      if (value !== 'MCQ' && value !== 'TrueFalse') next.correctAnswer = '';
    }
    this.draft.set(next);
  }

  addOption(): void {
    const cur = this.draft();
    if (cur.options.length >= 8) return;
    this.draft.set({ ...cur, options: [...cur.options, ''] });
  }

  removeOption(idx: number): void {
    const cur = this.draft();
    if (cur.options.length <= 2) return;
    const dropped = cur.options[idx];
    const opts = cur.options.filter((_, i) => i !== idx);
    const ca = cur.correctAnswer === dropped ? '' : cur.correctAnswer;
    this.draft.set({ ...cur, options: opts, correctAnswer: ca });
  }

  setOption(idx: number, value: string): void {
    const cur = this.draft();
    const opts = cur.options.map((o, i) => (i === idx ? value : o));
    this.draft.set({ ...cur, options: opts });
  }

  // --- Save ---------------------------------------------------------------

  save(): void {
    if (this.saving()) return;
    const body = this.toBody();
    if (!body) return;

    this.saving.set(true);
    this.saveError.set(null);
    this.fieldErrors.set({});

    const obs = this.id()
      ? this.service.updateBankQuestion(this.id()!, body)
      : this.service.createBankQuestion(body);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/teacher/question-bank']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        if (err.status === 400 && err.error?.errors) {
          this.fieldErrors.set(err.error.errors as Record<string, string[]>);
          this.saveError.set('Please fix the highlighted fields.');
        } else if (err.status === 404) {
          this.saveError.set('Question no longer exists.');
        } else {
          this.saveError.set('Could not save. Please try again.');
        }
      },
    });
  }

  private toBody(): BankQuestionUpsertBody | null {
    const d = this.draft();
    const text = d.questionText.trim();
    if (!text) {
      this.saveError.set('Question text is required.');
      return null;
    }
    if (this.requiresOptions()) {
      const cleaned = d.options.map((o) => o.trim()).filter(Boolean);
      if (cleaned.length < 2) {
        this.saveError.set('Multiple-choice needs at least 2 non-empty options.');
        return null;
      }
    }
    if (this.requiresCorrectAnswer() && !d.correctAnswer.trim()) {
      this.saveError.set('Pick the correct answer.');
      return null;
    }
    return {
      questionText: text,
      type: d.type,
      options: this.requiresOptions()
        ? d.options.map((o) => o.trim()).filter(Boolean)
        : null,
      correctAnswer: this.requiresCorrectAnswer() ? d.correctAnswer.trim() : null,
      points: d.points,
      tag: d.tag.trim() || null,
    };
  }
}

function emptyDraft(): DraftQuestion {
  return {
    questionText: '',
    type: 'MCQ',
    options: ['', ''],
    correctAnswer: '',
    points: 1,
    tag: '',
  };
}

function toDraft(q: BankQuestion): DraftQuestion {
  return {
    questionText: q.questionText,
    type: q.type,
    options: q.options?.length ? [...q.options] : (q.type === 'MCQ' ? ['', ''] : []),
    correctAnswer: q.correctAnswer ?? '',
    points: q.points,
    tag: q.tag ?? '',
  };
}
