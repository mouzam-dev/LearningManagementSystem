import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { nativePlugin } from '../native';

/**
 * Phone push notifications (Android app only). After sign-in the app asks for permission,
 * gets an FCM token from the device and stores it against the user (POST /api/devices);
 * the server then pushes every in-app notification to that phone. Tapping one opens the page.
 */
@Injectable({ providedIn: 'root' })
export class PushService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = `${environment.apiUrl}/devices`;
  private token: string | null = null;
  private started = false;

  async register(): Promise<void> {
    const push = nativePlugin('PushNotifications');
    if (!push || this.started) return;
    this.started = true;
    try {
      let perm = await push.checkPermissions();
      if (perm.receive === 'prompt' || perm.receive === 'prompt-with-rationale') perm = await push.requestPermissions();
      if (perm.receive !== 'granted') return;

      await push.removeAllListeners();
      await push.addListener('registration', (t: { value: string }) => {
        this.token = t.value;
        this.http.post(this.base, { token: t.value, platform: 'android' }).subscribe({ error: () => {} });
      });
      await push.addListener('pushNotificationActionPerformed', (a: { notification: { data?: { path?: string } } }) => {
        const path = a.notification.data?.path;
        void this.router.navigateByUrl(path?.startsWith('/') ? path : '/notifications');
      });
      await push.register();
    } catch {
      this.started = false; // e.g. Google Play services missing - just no push on this phone.
    }
  }

  /** Call before signing out so the next person on this phone doesn't get these pushes. */
  unregister(): void {
    if (!this.token) return;
    this.http.post(`${this.base}/unregister`, { token: this.token }).subscribe({ error: () => {} });
    this.token = null;
    this.started = false;
  }
}
