import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export type ImageUploadKind = 'logo' | 'avatar' | 'thumbnail' | 'misc';

/**
 * Wraps the role-agnostic POST /api/uploads/image endpoint. Returns the
 * absolute URL of the stored image, which the caller is expected to persist
 * onto an entity (Organization.logoUrl, User.profilePictureUrl, etc.).
 */
@Injectable({ providedIn: 'root' })
export class UploadsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/uploads`;

  uploadImage(file: File, kind: ImageUploadKind = 'misc'): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file);
    const params = new HttpParams().set('kind', kind);
    return this.http.post<{ url: string }>(`${this.baseUrl}/image`, form, { params });
  }
}
