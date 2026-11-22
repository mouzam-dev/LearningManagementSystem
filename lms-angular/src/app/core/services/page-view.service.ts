import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';

/** Same key the embedded Learn Arabic app (public/learn-arabic/boot.js) uses, so a browser is one visitor. */
export const VISITOR_KEY = 'lms.visitorId';

/**
 * First-party page-view tracking for the admin Analytics page. Sends only the route and a random
 * per-browser id; the signed-in user is read from the token on the server. Failures are ignored.
 */
@Injectable({ providedIn: 'root' })
export class PageViewService {
  private readonly http = inject(HttpClient);
  private readonly visitorId = PageViewService.visitor();

  track(path: string): void {
    this.http.post(`${environment.apiUrl}/analytics/pageview`, { visitorId: this.visitorId, path }).subscribe({ error: () => {} });
  }

  private static visitor(): string {
    try {
      let id = localStorage.getItem(VISITOR_KEY);
      if (!id) { id = crypto.randomUUID(); localStorage.setItem(VISITOR_KEY, id); }
      return id;
    } catch {
      return crypto.randomUUID(); // storage blocked: count this page load on its own
    }
  }
}
