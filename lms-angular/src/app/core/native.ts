/**
 * Bridge to the Android app (mobile/, Capacitor). Inside the app the native shell injects
 * `window.Capacitor` with its plugins; in a normal browser it is absent and every helper
 * here falls back to plain web behaviour. No Capacitor npm package is needed on this side.
 */
interface CapacitorGlobal {
  isNativePlatform(): boolean;
  isPluginAvailable(name: string): boolean;
  Plugins: Record<string, any>;
}

function capacitor(): CapacitorGlobal | null {
  const c = (globalThis as { Capacitor?: CapacitorGlobal }).Capacitor;
  return c?.isNativePlatform?.() ? c : null;
}

/** True when running inside the Android app. */
export function isNativeApp(): boolean {
  return capacitor() !== null;
}

/** A native plugin by name, or null in the browser / when the APK doesn't include it. */
export function nativePlugin<T = any>(name: string): T | null {
  const c = capacitor();
  return c?.isPluginAvailable(name) ? (c.Plugins[name] as T) : null;
}

/**
 * Save a generated file. Browser: normal download. App: the web view can't download blobs,
 * so the file is written to the app's cache and handed to the Android share sheet
 * (Save to Files / Drive, WhatsApp, ...).
 */
export async function saveFile(blob: Blob, fileName: string): Promise<void> {
  const fs = nativePlugin('Filesystem');
  const share = nativePlugin('Share');
  if (fs && share) {
    const { uri } = await fs.writeFile({ path: fileName, data: await toBase64(blob), directory: 'CACHE' });
    try {
      await share.share({ title: fileName, files: [uri] });
    } catch {
      // The user closed the share sheet - nothing to do.
    }
    return;
  }
  if (typeof document === 'undefined') return;
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** Open an outside site (e.g. a Jitsi class). App: in-app Chrome tab; browser: new tab. */
export function openExternal(url: string): void {
  const browser = nativePlugin('Browser');
  if (browser) void browser.open({ url });
  else window.open(url, '_blank', 'noopener,noreferrer');
}

function toBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const r = new FileReader();
    r.onload = () => resolve(String(r.result).split(',')[1] ?? '');
    r.onerror = () => reject(r.error);
    r.readAsDataURL(blob);
  });
}
