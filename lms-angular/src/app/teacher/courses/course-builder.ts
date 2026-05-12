import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  TeacherCourseDetail,
  TeacherLesson,
  TeacherModule,
} from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

type LessonDraft = {
  title: string;
  type: string;
  videoUrl: string;
  duration: number | null;
  isPublished: boolean;
};

type ModuleDraft = {
  title: string;
  description: string;
};

type CourseEditDraft = {
  title: string;
  description: string;
  category: string;
  thumbnailUrl: string;
  maxStudents: number | null;
};

const EMPTY_LESSON: LessonDraft = {
  title: '',
  type: 'Video',
  videoUrl: '',
  duration: null,
  isPublished: true,
};

const EMPTY_MODULE: ModuleDraft = { title: '', description: '' };

/**
 * The authoring surface — teachers manage modules + lessons + metadata + publish state
 * for a single course here. All edits use inline modals/forms; on success the page
 * re-fetches the course to keep the local state authoritative.
 */
@Component({
  selector: 'app-teacher-course-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './course-builder.html',
})
export class TeacherCourseBuilderPage {
  private readonly teacher = inject(TeacherService);

  readonly courseId = input.required<string>();

  // -- Course state -----------------------------------------------------------
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly course = signal<TeacherCourseDetail | null>(null);
  readonly busy = signal<string | null>(null); // generic "row is being saved" key

  // -- Course metadata edit modal --------------------------------------------
  readonly editingMeta = signal(false);
  readonly metaDraft = signal<CourseEditDraft>({
    title: '', description: '', category: '', thumbnailUrl: '', maxStudents: null,
  });
  readonly metaErrors = signal<Record<string, string[]>>({});

  // -- New module form -------------------------------------------------------
  readonly addingModule = signal(false);
  readonly moduleDraft = signal<ModuleDraft>({ ...EMPTY_MODULE });
  readonly moduleErrors = signal<Record<string, string[]>>({});

  // -- Module-being-edited (inline) ------------------------------------------
  readonly editingModuleId = signal<string | null>(null);
  readonly moduleEditDraft = signal<ModuleDraft>({ ...EMPTY_MODULE });

  // -- New lesson form (keyed by moduleId so each module can have one open) --
  readonly addingLessonForModule = signal<string | null>(null);
  readonly lessonDraft = signal<LessonDraft>({ ...EMPTY_LESSON });
  readonly lessonErrors = signal<Record<string, string[]>>({});

  // -- Lesson-being-edited ---------------------------------------------------
  readonly editingLessonId = signal<string | null>(null);
  readonly lessonEditDraft = signal<LessonDraft>({ ...EMPTY_LESSON });

  // -- Publish error (separate from generic error banner) --------------------
  readonly publishError = signal<string | null>(null);

  readonly lessonTypes = ['Video', 'Document', 'Text'];

  readonly publishableHint = computed(() => {
    const c = this.course();
    if (!c) return null;
    const totalPublishedLessons = c.modules
      .flatMap((m) => m.lessons)
      .filter((l) => l.isPublished).length;
    if (totalPublishedLessons === 0) {
      return 'Publish at least one lesson before publishing the course.';
    }
    return null;
  });

  constructor() {
    effect(() => {
      const id = this.courseId();
      if (id) this.fetch(id);
    });
  }

  // -------------------------------------------------------------------------
  // Data loading
  // -------------------------------------------------------------------------

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getCourseDetail(id).subscribe({
      next: (c) => {
        this.course.set(c);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.course.set(null);
        this.loading.set(false);
        this.error.set(this.formatError(err));
      },
    });
  }

  private refetch(): void {
    this.teacher.getCourseDetail(this.courseId()).subscribe({
      next: (c) => this.course.set(c),
    });
  }

  // -------------------------------------------------------------------------
  // Metadata edit
  // -------------------------------------------------------------------------

  openMetaEdit(): void {
    const c = this.course();
    if (!c) return;
    this.metaDraft.set({
      title: c.title,
      description: c.description,
      category: c.category,
      thumbnailUrl: c.thumbnailUrl ?? '',
      maxStudents: c.maxStudents ?? null,
    });
    this.metaErrors.set({});
    this.editingMeta.set(true);
  }

  cancelMetaEdit(): void {
    this.editingMeta.set(false);
    this.metaErrors.set({});
  }

  saveMeta(): void {
    const draft = this.metaDraft();
    this.busy.set('meta');
    this.teacher
      .updateCourse(this.courseId(), {
        title: draft.title.trim(),
        description: draft.description.trim(),
        category: draft.category.trim(),
        thumbnailUrl: draft.thumbnailUrl.trim() || null,
        maxStudents: draft.maxStudents,
      })
      .subscribe({
        next: (updated) => {
          this.course.set(updated);
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

  // -------------------------------------------------------------------------
  // Publish / unpublish
  // -------------------------------------------------------------------------

  togglePublished(): void {
    const c = this.course();
    if (!c) return;
    const next = !c.isPublished;
    this.publishError.set(null);
    this.busy.set('publish');
    this.teacher.setCoursePublished(c.courseId, next).subscribe({
      next: (updated) => {
        this.course.set(updated);
        this.busy.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        if (err.status === 409) {
          this.publishError.set(err.error?.message ?? 'Cannot publish yet.');
        } else {
          this.publishError.set(this.formatError(err));
        }
      },
    });
  }

  // -------------------------------------------------------------------------
  // Modules
  // -------------------------------------------------------------------------

  openAddModule(): void {
    this.moduleDraft.set({ ...EMPTY_MODULE });
    this.moduleErrors.set({});
    this.addingModule.set(true);
  }

  cancelAddModule(): void {
    this.addingModule.set(false);
    this.moduleErrors.set({});
  }

  saveNewModule(): void {
    const draft = this.moduleDraft();
    this.busy.set('module-create');
    this.teacher
      .createModule(this.courseId(), {
        title: draft.title.trim(),
        description: draft.description.trim() || null,
      })
      .subscribe({
        next: () => {
          this.addingModule.set(false);
          this.busy.set(null);
          this.refetch();
        },
        error: (err: HttpErrorResponse) => {
          this.busy.set(null);
          if (err.status === 400 && err.error?.errors) {
            this.moduleErrors.set(err.error.errors);
          } else {
            this.error.set(this.formatError(err));
          }
        },
      });
  }

  startEditModule(m: TeacherModule): void {
    this.moduleEditDraft.set({
      title: m.title,
      description: m.description ?? '',
    });
    this.editingModuleId.set(m.moduleId);
  }

  cancelEditModule(): void {
    this.editingModuleId.set(null);
  }

  saveModuleEdit(): void {
    const id = this.editingModuleId();
    if (!id) return;
    const draft = this.moduleEditDraft();
    this.busy.set(`module-${id}`);
    this.teacher
      .updateModule(id, {
        title: draft.title.trim(),
        description: draft.description.trim() || null,
      })
      .subscribe({
        next: () => {
          this.editingModuleId.set(null);
          this.busy.set(null);
          this.refetch();
        },
        error: (err) => {
          this.busy.set(null);
          this.error.set(this.formatError(err));
        },
      });
  }

  deleteModule(m: TeacherModule): void {
    const lessonCount = m.lessons.length;
    const msg = lessonCount > 0
      ? `Delete "${m.title}" and its ${lessonCount} lesson(s)? This cannot be undone.`
      : `Delete "${m.title}"?`;
    if (typeof window !== 'undefined' && !window.confirm(msg)) return;

    this.busy.set(`module-${m.moduleId}`);
    this.teacher.deleteModule(m.moduleId).subscribe({
      next: () => { this.busy.set(null); this.refetch(); },
      error: (err) => { this.busy.set(null); this.error.set(this.formatError(err)); },
    });
  }

  // -------------------------------------------------------------------------
  // Lessons
  // -------------------------------------------------------------------------

  openAddLesson(moduleId: string): void {
    this.lessonDraft.set({ ...EMPTY_LESSON });
    this.lessonErrors.set({});
    this.addingLessonForModule.set(moduleId);
  }

  cancelAddLesson(): void {
    this.addingLessonForModule.set(null);
    this.lessonErrors.set({});
  }

  saveNewLesson(): void {
    const moduleId = this.addingLessonForModule();
    if (!moduleId) return;
    const draft = this.lessonDraft();

    this.busy.set(`lesson-create-${moduleId}`);
    this.teacher
      .createLesson(moduleId, {
        title: draft.title.trim(),
        type: draft.type,
        content: this.buildContent(draft),
        duration: draft.duration,
        isPublished: draft.isPublished,
      })
      .subscribe({
        next: () => {
          this.addingLessonForModule.set(null);
          this.busy.set(null);
          this.refetch();
        },
        error: (err: HttpErrorResponse) => {
          this.busy.set(null);
          if (err.status === 400 && err.error?.errors) {
            this.lessonErrors.set(err.error.errors);
          } else {
            this.error.set(this.formatError(err));
          }
        },
      });
  }

  startEditLesson(l: TeacherLesson): void {
    let videoUrl = '';
    try {
      if (l.content) videoUrl = (JSON.parse(l.content)?.videoUrl as string) ?? '';
    } catch {
      videoUrl = '';
    }
    this.lessonEditDraft.set({
      title: l.title,
      type: l.type,
      videoUrl,
      duration: l.duration ?? null,
      isPublished: l.isPublished,
    });
    this.editingLessonId.set(l.lessonId);
  }

  cancelEditLesson(): void {
    this.editingLessonId.set(null);
  }

  saveLessonEdit(): void {
    const id = this.editingLessonId();
    if (!id) return;
    const draft = this.lessonEditDraft();

    this.busy.set(`lesson-${id}`);
    this.teacher
      .updateLesson(id, {
        title: draft.title.trim(),
        type: draft.type,
        content: this.buildContent(draft),
        duration: draft.duration,
        isPublished: draft.isPublished,
      })
      .subscribe({
        next: () => {
          this.editingLessonId.set(null);
          this.busy.set(null);
          this.refetch();
        },
        error: (err) => {
          this.busy.set(null);
          this.error.set(this.formatError(err));
        },
      });
  }

  deleteLesson(l: TeacherLesson): void {
    if (typeof window !== 'undefined' && !window.confirm(`Delete lesson "${l.title}"?`)) return;
    this.busy.set(`lesson-${l.lessonId}`);
    this.teacher.deleteLesson(l.lessonId).subscribe({
      next: () => { this.busy.set(null); this.refetch(); },
      error: (err) => { this.busy.set(null); this.error.set(this.formatError(err)); },
    });
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  /** Pack the type-specific content into the Lesson.Content JSON column. */
  private buildContent(draft: LessonDraft): string | null {
    if (draft.type === 'Video') {
      const url = draft.videoUrl.trim();
      return url ? JSON.stringify({ videoUrl: url }) : null;
    }
    return null;
  }

  fieldError(errors: Record<string, string[]>, name: string): string | null {
    const key = Object.keys(errors).find((k) => k.toLowerCase() === name.toLowerCase());
    return key ? errors[key].join(' ') : null;
  }

  totalLessons(c: TeacherCourseDetail): number {
    return c.modules.reduce((sum, m) => sum + m.lessons.length, 0);
  }

  formatDuration(minutes?: number | null): string {
    if (!minutes || minutes <= 0) return '';
    if (minutes < 60) return `${minutes} min`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m === 0 ? `${h}h` : `${h}h ${m}m`;
  }

  /** Two-way binding glue for typed signal drafts. */
  updateDraft<T extends object, K extends keyof T>(
    draft: import('@angular/core').WritableSignal<T>,
    key: K,
    value: T[K],
  ): void {
    draft.update((curr) => ({ ...curr, [key]: value }));
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API.';
    if (err.status === 404) return 'This course no longer exists.';
    return err.error?.message ?? err.statusText ?? 'Something went wrong.';
  }
}
