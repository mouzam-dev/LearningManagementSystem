import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { PagedResult } from '../../student/models/course.models';
import { BankQuestionListItem } from '../models/bank-question.models';
import { RubricListItem } from '../models/rubric.models';
import {
  TeacherAssessment,
  TeacherQuestion,
} from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

type QuestionDraft = {
  questionText: string;
  type: string;
  options: string[];
  correctAnswer: string;
  points: number;
};

type MetaDraft = {
  title: string;
  passingScore: number;
  timeLimit: number | null;
  dueDate: string;
  maxAttempts: number | null;
  rubricId: string | null;
};

const EMPTY_QUESTION: QuestionDraft = {
  questionText: '',
  type: 'MCQ',
  options: ['', ''],
  correctAnswer: '',
  points: 1,
};

const TRUE_FALSE_OPTIONS = ['True', 'False'];

/**
 * Assessment editor — single page where teachers manage assessment metadata
 * AND its questions. Mirrors the course-builder pattern: edit-in-place via
 * signal drafts, server-side field errors merged into the same red-border UI,
 * confirm dialogs on delete.
 */
@Component({
  selector: 'app-teacher-assessment-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './assessment-editor.html',
})
export class TeacherAssessmentEditorPage {
  private readonly teacher = inject(TeacherService);
  private readonly router = inject(Router);

  readonly assessmentId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly assessment = signal<TeacherAssessment | null>(null);
  readonly busy = signal<string | null>(null);

  // -- Metadata edit modal --------------------------------------------------
  readonly editingMeta = signal(false);
  readonly metaDraft = signal<MetaDraft>({
    title: '', passingScore: 70, timeLimit: null, dueDate: '', maxAttempts: null, rubricId: null,
  });
  readonly metaErrors = signal<Record<string, string[]>>({});
  readonly availableRubrics = signal<RubricListItem[]>([]);

  // -- New question form ----------------------------------------------------
  readonly addingQuestion = signal(false);
  readonly questionDraft = signal<QuestionDraft>({ ...EMPTY_QUESTION });
  readonly questionErrors = signal<Record<string, string[]>>({});

  // -- Question-being-edited ------------------------------------------------
  readonly editingQuestionId = signal<string | null>(null);
  readonly questionEditDraft = signal<QuestionDraft>({ ...EMPTY_QUESTION });

  // -- Import-from-bank picker ---------------------------------------------
  readonly importing = signal(false);
  readonly importLoading = signal(false);
  readonly importError = signal<string | null>(null);
  readonly importBusy = signal(false);
  readonly importQuery = signal('');
  readonly importTag = signal('');
  readonly importPage = signal(1);
  readonly importResult = signal<PagedResult<BankQuestionListItem> | null>(null);
  readonly importTags = signal<string[]>([]);
  readonly selectedImportIds = signal<Set<string>>(new Set());
  readonly selectedImportCount = computed(() => this.selectedImportIds().size);

  readonly questionTypes = computed(() => {
    // For Quiz assessments: every type makes sense. For Assignment: typically
    // just Essay/ShortAnswer (free-text). Keep all 4 visible — teachers know
    // what they're doing.
    return ['MCQ', 'TrueFalse', 'ShortAnswer', 'Essay'];
  });

  constructor() {
    effect(() => {
      const id = this.assessmentId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getAssessment(id).subscribe({
      next: (a) => {
        this.assessment.set(a);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.assessment.set(null);
        this.loading.set(false);
        this.error.set(this.formatError(err));
      },
    });
  }

  private refetch(): void {
    this.teacher.getAssessment(this.assessmentId()).subscribe({
      next: (a) => this.assessment.set(a),
    });
  }

  // -------------------------------------------------------------------------
  // Metadata edit
  // -------------------------------------------------------------------------

  openMetaEdit(): void {
    const a = this.assessment();
    if (!a) return;
    this.metaDraft.set({
      title: a.title,
      passingScore: a.passingScore,
      timeLimit: a.timeLimit ?? null,
      dueDate: a.dueDate ? this.toLocalDateTimeString(a.dueDate) : '',
      maxAttempts: a.maxAttempts ?? null,
      rubricId: a.rubricId ?? null,
    });
    this.metaErrors.set({});
    this.editingMeta.set(true);

    // Lazy-load the rubric dropdown options. Only relevant for Assignment-
    // type assessments; the template hides the field for quizzes anyway.
    if (a.type === 'Assignment' && this.availableRubrics().length === 0) {
      this.teacher.getRubrics({ pageSize: 100 }).subscribe({
        next: (r) => this.availableRubrics.set(r.items),
        error: () => this.availableRubrics.set([]),
      });
    }
  }

  cancelMetaEdit(): void {
    this.editingMeta.set(false);
  }

  saveMeta(): void {
    const a = this.assessment();
    if (!a) return;
    const draft = this.metaDraft();
    this.busy.set('meta');

    this.teacher.updateAssessment(a.assessmentId, {
      title: draft.title.trim(),
      timeLimit: draft.timeLimit,
      passingScore: draft.passingScore,
      dueDate: draft.dueDate ? new Date(draft.dueDate).toISOString() : null,
      maxAttempts: draft.maxAttempts,
      rubricId: a.type === 'Assignment' ? (draft.rubricId || null) : null,
    }).subscribe({
      next: (updated) => {
        this.assessment.set(updated);
        this.editingMeta.set(false);
        this.busy.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        if (err.status === 400 && err.error?.errors) {
          this.metaErrors.set(err.error.errors);
        } else {
          this.error.set(this.formatError(err));
        }
      },
    });
  }

  deleteAssessment(): void {
    const a = this.assessment();
    if (!a) return;
    if (typeof window !== 'undefined' && !window.confirm(`Delete "${a.title}" and all its questions and submissions?`)) {
      return;
    }
    this.busy.set('delete');
    this.teacher.deleteAssessment(a.assessmentId).subscribe({
      next: () => {
        this.busy.set(null);
        this.router.navigate(['/teacher/courses', a.courseId]);
      },
      error: (err) => {
        this.busy.set(null);
        this.error.set(this.formatError(err));
      },
    });
  }

  // -------------------------------------------------------------------------
  // Question CRUD
  // -------------------------------------------------------------------------

  openAddQuestion(): void {
    this.questionDraft.set({ ...EMPTY_QUESTION, options: ['', ''] });
    this.questionErrors.set({});
    this.addingQuestion.set(true);
  }

  cancelAddQuestion(): void {
    this.addingQuestion.set(false);
  }

  saveNewQuestion(): void {
    const draft = this.normalizedDraft(this.questionDraft());
    this.busy.set('question-create');
    this.teacher.createQuestion(this.assessmentId(), {
      questionText: draft.questionText.trim(),
      type: draft.type,
      options: this.optionsForType(draft.type, draft.options),
      correctAnswer: draft.correctAnswer.trim() || null,
      points: draft.points,
    }).subscribe({
      next: () => {
        this.busy.set(null);
        this.addingQuestion.set(false);
        this.refetch();
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        if (err.status === 400 && err.error?.errors) {
          this.questionErrors.set(err.error.errors);
        } else {
          this.error.set(this.formatError(err));
        }
      },
    });
  }

  startEditQuestion(q: TeacherQuestion): void {
    this.questionEditDraft.set({
      questionText: q.questionText,
      type: q.type,
      options: q.options && q.options.length >= 2 ? [...q.options] : ['', ''],
      correctAnswer: q.correctAnswer ?? '',
      points: q.points,
    });
    this.editingQuestionId.set(q.questionId);
  }

  cancelEditQuestion(): void {
    this.editingQuestionId.set(null);
  }

  saveQuestionEdit(): void {
    const id = this.editingQuestionId();
    if (!id) return;
    const draft = this.normalizedDraft(this.questionEditDraft());

    this.busy.set(`question-${id}`);
    this.teacher.updateQuestion(id, {
      questionText: draft.questionText.trim(),
      type: draft.type,
      options: this.optionsForType(draft.type, draft.options),
      correctAnswer: draft.correctAnswer.trim() || null,
      points: draft.points,
    }).subscribe({
      next: () => {
        this.busy.set(null);
        this.editingQuestionId.set(null);
        this.refetch();
      },
      error: (err) => {
        this.busy.set(null);
        this.error.set(this.formatError(err));
      },
    });
  }

  deleteQuestion(q: TeacherQuestion): void {
    if (typeof window !== 'undefined' && !window.confirm(`Delete this question?`)) return;
    this.busy.set(`question-${q.questionId}`);
    this.teacher.deleteQuestion(q.questionId).subscribe({
      next: () => { this.busy.set(null); this.refetch(); },
      error: (err) => { this.busy.set(null); this.error.set(this.formatError(err)); },
    });
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  /** When the user flips a draft's type, normalize options/correct answer. */
  onTypeChange(draft: import('@angular/core').WritableSignal<QuestionDraft>, value: string): void {
    draft.update((curr) => {
      if (value === 'TrueFalse') {
        return { ...curr, type: value, options: TRUE_FALSE_OPTIONS, correctAnswer: curr.correctAnswer || 'True' };
      }
      if (value === 'MCQ') {
        return { ...curr, type: value, options: curr.options.length >= 2 ? curr.options : ['', ''] };
      }
      // ShortAnswer / Essay → no options
      return { ...curr, type: value, options: [] };
    });
  }

  addOption(draft: import('@angular/core').WritableSignal<QuestionDraft>): void {
    draft.update((curr) => ({ ...curr, options: [...curr.options, ''] }));
  }

  removeOption(draft: import('@angular/core').WritableSignal<QuestionDraft>, index: number): void {
    draft.update((curr) => ({
      ...curr,
      options: curr.options.length > 2 ? curr.options.filter((_, i) => i !== index) : curr.options,
    }));
  }

  updateOption(draft: import('@angular/core').WritableSignal<QuestionDraft>, index: number, value: string): void {
    draft.update((curr) => ({
      ...curr,
      options: curr.options.map((o, i) => (i === index ? value : o)),
    }));
  }

  updateDraft<T extends object, K extends keyof T>(
    draft: import('@angular/core').WritableSignal<T>,
    key: K,
    value: T[K],
  ): void {
    draft.update((curr) => ({ ...curr, [key]: value }));
  }

  // Angular's template type checker can't infer the generic K when the draft
  // signal is passed via ngTemplateOutlet context, so the generic updateDraft
  // collapses to `never`. Use these non-generic shims from the template.
  setQText(draft: import('@angular/core').WritableSignal<QuestionDraft>, v: string): void {
    draft.update((d) => ({ ...d, questionText: v }));
  }
  setQPoints(draft: import('@angular/core').WritableSignal<QuestionDraft>, v: number): void {
    draft.update((d) => ({ ...d, points: v }));
  }
  setQCorrect(draft: import('@angular/core').WritableSignal<QuestionDraft>, v: string): void {
    draft.update((d) => ({ ...d, correctAnswer: v }));
  }

  serverFieldError(errors: Record<string, string[]>, name: string): string | null {
    const key = Object.keys(errors).find((k) => k.toLowerCase() === name.toLowerCase());
    return key ? errors[key].join(' ') : null;
  }

  private normalizedDraft(d: QuestionDraft): QuestionDraft {
    if (d.type === 'TrueFalse') {
      return { ...d, options: TRUE_FALSE_OPTIONS };
    }
    if (d.type === 'MCQ') {
      return { ...d, options: d.options.map((o) => o.trim()).filter(Boolean) };
    }
    return { ...d, options: [], correctAnswer: '' };
  }

  /** Returns the options array to send to the API; null when not applicable. */
  private optionsForType(type: string, options: string[]): string[] | null {
    if (type === 'TrueFalse') return TRUE_FALSE_OPTIONS;
    if (type === 'MCQ') return options.map((o) => o.trim()).filter(Boolean);
    return null;
  }

  private toLocalDateTimeString(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API.';
    if (err.status === 404) return 'This assessment no longer exists.';
    return err.error?.message ?? err.statusText ?? 'Something went wrong.';
  }

  // -------------------------------------------------------------------------
  // Import from question bank (BRD TCH-021)
  // -------------------------------------------------------------------------

  openImport(): void {
    this.importing.set(true);
    this.importError.set(null);
    this.importQuery.set('');
    this.importTag.set('');
    this.importPage.set(1);
    this.selectedImportIds.set(new Set());
    this.fetchImportPage();
    this.teacher.getBankQuestionTags().subscribe({
      next: (t) => this.importTags.set(t),
      error: () => this.importTags.set([]),
    });
  }

  closeImport(): void {
    this.importing.set(false);
    this.importResult.set(null);
    this.selectedImportIds.set(new Set());
  }

  applyImportFilter(): void {
    this.importPage.set(1);
    this.fetchImportPage();
  }

  importGoToPage(p: number): void {
    const total = this.importResult()?.totalPages ?? 0;
    if (p < 1 || p > total) return;
    this.importPage.set(p);
    this.fetchImportPage();
  }

  toggleImportSelected(id: string): void {
    const cur = new Set(this.selectedImportIds());
    if (cur.has(id)) cur.delete(id);
    else cur.add(id);
    this.selectedImportIds.set(cur);
  }

  isImportSelected(id: string): boolean {
    return this.selectedImportIds().has(id);
  }

  private fetchImportPage(): void {
    this.importLoading.set(true);
    this.importError.set(null);
    this.teacher.getBankQuestions({
      search: this.importQuery().trim() || undefined,
      tag: this.importTag().trim() || undefined,
      page: this.importPage(),
      pageSize: 10,
    }).subscribe({
      next: (r) => {
        this.importResult.set(r);
        this.importLoading.set(false);
      },
      error: () => {
        this.importLoading.set(false);
        this.importError.set('Failed to load bank questions.');
      },
    });
  }

  doImport(): void {
    const a = this.assessment();
    if (!a || this.importBusy() || this.selectedImportCount() === 0) return;
    const ids = Array.from(this.selectedImportIds());
    this.importBusy.set(true);
    this.importError.set(null);

    this.teacher.importBankQuestionsToAssessment(a.assessmentId, ids).subscribe({
      next: (updated) => {
        this.assessment.set(updated);
        this.importBusy.set(false);
        this.closeImport();
      },
      error: (err: HttpErrorResponse) => {
        this.importBusy.set(false);
        this.importError.set(err.status === 400
          ? 'Could not import those questions.'
          : err.status === 404 ? 'Assessment or questions no longer exist.'
          : 'Import failed. Please try again.');
      },
    });
  }

  importTypeLabel(t: string): string {
    switch (t) {
      case 'MCQ': return 'Multiple choice';
      case 'TrueFalse': return 'True / false';
      case 'ShortAnswer': return 'Short answer';
      case 'Essay': return 'Essay';
      default: return t;
    }
  }
}
