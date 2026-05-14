import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { ChangePasswordBody, Profile, UpdateProfileBody } from './profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/profile`;

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(this.baseUrl);
  }

  updateProfile(body: UpdateProfileBody): Observable<Profile> {
    return this.http.put<Profile>(this.baseUrl, body);
  }

  changePassword(body: ChangePasswordBody): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/password`, body);
  }
}
