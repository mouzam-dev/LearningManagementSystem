import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';

import { AuthService } from '../auth/auth.service';
import { Profile } from './profile.models';
import { ProfileService } from './profile.service';

interface DetailsControls {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  bio: FormControl<string>;
  profilePictureUrl: FormControl<string>;
}

interface PasswordControls {
  currentPassword: FormControl<string>;
  newPassword: FormControl<string>;
  confirmPassword: FormControl<string>;
}

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  private readonly profileService = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly profile = signal<Profile | null>(null);

  // -- Details card --------------------------------------------------------
  readonly savingDetails = signal(false);
  readonly detailsSaved = signal(false);
  readonly detailsServerError = signal<string | null>(null);
  readonly detailsFieldErrors = signal<Record<string, string[]>>({});

  readonly detailsForm: FormGroup<DetailsControls> = this.fb.nonNullable.group({
    firstName: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    lastName: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    bio: this.fb.nonNullable.control('', [Validators.maxLength(1000)]),
    profilePictureUrl: this.fb.nonNullable.control('', [Validators.maxLength(500), this.optionalHttpUrl]),
  });

  // -- Password card -------------------------------------------------------
  readonly savingPassword = signal(false);
  readonly passwordSaved = signal(false);
  readonly passwordServerError = signal<string | null>(null);
  readonly passwordFieldErrors = signal<Record<string, string[]>>({});

  readonly passwordForm: FormGroup<PasswordControls> = this.fb.nonNullable.group(
    {
      currentPassword: this.fb.nonNullable.control('', [Validators.required]),
      newPassword: this.fb.nonNullable.control('', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/),
      ]),
      confirmPassword: this.fb.nonNullable.control('', [Validators.required]),
    },
    { validators: [this.passwordsMatch] },
  );

  /** Avatar: the picture if set, otherwise the user's initials. */
  readonly initials = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return `${p.firstName.charAt(0)}${p.lastName.charAt(0)}`.toUpperCase();
  });

  constructor() {
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.profileService.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.detailsForm.patchValue({
          firstName: p.firstName,
          lastName: p.lastName,
          bio: p.bio ?? '',
          profilePictureUrl: p.profilePictureUrl ?? '',
        });
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API. Is it running on http://localhost:5116?'
          : (err.error?.message ?? err.statusText ?? 'Could not load your profile.'));
      },
    });
  }

  // -------------------------------------------------------------------------
  // Save details
  // -------------------------------------------------------------------------

  saveDetails(): void {
    this.detailsServerError.set(null);
    this.detailsFieldErrors.set({});
    this.detailsSaved.set(false);

    if (this.detailsForm.invalid) {
      this.detailsForm.markAllAsTouched();
      return;
    }

    const raw = this.detailsForm.getRawValue();
    this.savingDetails.set(true);

    this.profileService
      .updateProfile({
        firstName: raw.firstName.trim(),
        lastName: raw.lastName.trim(),
        bio: raw.bio.trim() || null,
        profilePictureUrl: raw.profilePictureUrl.trim() || null,
      })
      .subscribe({
        next: (updated) => {
          this.profile.set(updated);
          this.savingDetails.set(false);
          this.detailsSaved.set(true);
          // Keep the header chip + role nav in sync without a re-login.
          this.auth.patchStoredUser({
            firstName: updated.firstName,
            lastName: updated.lastName,
            profilePictureUrl: updated.profilePictureUrl,
          });
          setTimeout(() => this.detailsSaved.set(false), 2500);
        },
        error: (err: HttpErrorResponse) => {
          this.savingDetails.set(false);
          if (err.status === 400 && err.error?.errors) {
            this.detailsFieldErrors.set(err.error.errors);
            this.detailsServerError.set('Please fix the highlighted fields.');
          } else {
            this.detailsServerError.set(err.error?.message ?? err.statusText ?? 'Could not save changes.');
          }
        },
      });
  }

  // -------------------------------------------------------------------------
  // Change password
  // -------------------------------------------------------------------------

  changePassword(): void {
    this.passwordServerError.set(null);
    this.passwordFieldErrors.set({});
    this.passwordSaved.set(false);

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const raw = this.passwordForm.getRawValue();
    this.savingPassword.set(true);

    this.profileService
      .changePassword({ currentPassword: raw.currentPassword, newPassword: raw.newPassword })
      .subscribe({
        next: () => {
          this.savingPassword.set(false);
          this.passwordSaved.set(true);
          this.passwordForm.reset();
          setTimeout(() => this.passwordSaved.set(false), 3000);
        },
        error: (err: HttpErrorResponse) => {
          this.savingPassword.set(false);
          if (err.status === 409) {
            // Wrong current password — pin the message to that field.
            this.passwordServerError.set(err.error?.message ?? 'Your current password is incorrect.');
          } else if (err.status === 400 && err.error?.errors) {
            this.passwordFieldErrors.set(err.error.errors);
            this.passwordServerError.set('Please fix the highlighted fields.');
          } else {
            this.passwordServerError.set(err.error?.message ?? err.statusText ?? 'Could not change your password.');
          }
        },
      });
  }

  serverFieldError(errors: Record<string, string[]>, name: string): string | null {
    const key = Object.keys(errors).find((k) => k.toLowerCase() === name.toLowerCase());
    return key ? errors[key].join(' ') : null;
  }

  // -- Validators ----------------------------------------------------------

  private optionalHttpUrl(control: AbstractControl): ValidationErrors | null {
    const v = (control.value ?? '').toString().trim();
    if (!v) return null;
    try {
      const u = new URL(v);
      return u.protocol === 'http:' || u.protocol === 'https:' ? null : { url: true };
    } catch {
      return { url: true };
    }
  }

  private passwordsMatch(group: AbstractControl): ValidationErrors | null {
    const next = group.get('newPassword')?.value;
    const confirm = group.get('confirmPassword')?.value;
    if (!next || !confirm) return null;
    return next === confirm ? null : { mismatch: true };
  }
}
