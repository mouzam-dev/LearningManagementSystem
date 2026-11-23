import { Component, inject, signal } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';
import { filter } from 'rxjs/operators';

import { isNativeApp, nativePlugin, openExternal } from '../core/native';

/** /app/version.json - bump versionCode there after publishing a new APK. */
interface AppVersion {
  versionCode: number;
  versionName: string;
  apk: string;
  notes?: string;
}

/**
 * Bottom toast for two kinds of update:
 *  - site: the service worker has a newer build ready -> Refresh.
 *  - app: the Android app is older than /app/version.json -> Download the new APK.
 */
@Component({
  selector: 'app-update-banner',
  template: `
    @if (apk(); as v) {
      <div class="fixed inset-x-3 bottom-[calc(5rem+var(--safe-bottom))] z-50 mx-auto flex max-w-md items-center gap-3 rounded-2xl border border-line bg-surface p-3 shadow-lg lg:bottom-4" role="status">
        <div class="min-w-0 flex-1 text-sm">
          <p class="font-semibold text-ink">New app version {{ v.versionName }}</p>
          <p class="text-xs text-muted">{{ v.notes || 'Update to get the latest features.' }}</p>
        </div>
        <button type="button" class="shrink-0 rounded-lg px-2 py-2 text-sm font-semibold text-muted hover:text-ink" (click)="apk.set(null)">Later</button>
        <button type="button" class="shrink-0 rounded-lg bg-primary-600 px-3 py-2 text-sm font-semibold text-white hover:bg-primary-700" (click)="download(v)">Download</button>
      </div>
    } @else if (siteUpdate()) {
      <div class="fixed inset-x-3 bottom-[calc(5rem+var(--safe-bottom))] z-50 mx-auto flex max-w-md items-center gap-3 rounded-2xl border border-line bg-surface p-3 shadow-lg lg:bottom-4" role="status">
        <p class="min-w-0 flex-1 text-sm font-semibold text-ink">A new version of the site is ready.</p>
        <button type="button" class="shrink-0 rounded-lg px-2 py-2 text-sm font-semibold text-muted hover:text-ink" (click)="siteUpdate.set(false)">Later</button>
        <button type="button" class="shrink-0 rounded-lg bg-primary-600 px-3 py-2 text-sm font-semibold text-white hover:bg-primary-700" (click)="reload()">Refresh</button>
      </div>
    }
  `,
})
export class AppUpdateBanner {
  private readonly sw = inject(SwUpdate);
  readonly siteUpdate = signal(false);
  readonly apk = signal<AppVersion | null>(null);

  constructor() {
    if (this.sw.isEnabled) {
      this.sw.versionUpdates.pipe(filter((e) => e.type === 'VERSION_READY')).subscribe(() => this.siteUpdate.set(true));
      // A cache the worker can't recover from: only a full reload fixes it.
      this.sw.unrecoverable.subscribe(() => this.reload());
    }
    if (isNativeApp()) void this.checkApk();
  }

  reload(): void {
    document.location.reload();
  }

  download(v: AppVersion): void {
    openExternal(new URL(v.apk, document.location.origin).href);
    this.apk.set(null);
  }

  private async checkApk(): Promise<void> {
    try {
      const info = await nativePlugin('App')?.getInfo();
      const res = await fetch('/app/version.json', { cache: 'no-store' });
      if (!info || !res.ok) return;
      const latest = (await res.json()) as AppVersion;
      if (latest.versionCode > Number(info.build)) this.apk.set(latest);
    } catch {
      // Offline or no version file - try again next launch.
    }
  }
}
