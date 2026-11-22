# Android app

A thin Android shell (Capacitor) that opens the live site, https://duare-shariye.fly.dev, full-screen
with the app icon and splash. Site changes reach the app on the next deploy - rebuild only to change
the icon, name, version or URL. `www/offline.html` shows when the phone has no internet.

## Build the APK (Windows)

Needs Java 21 and the Android SDK (platform 36, build-tools 36):

```powershell
$env:JAVA_HOME = (Get-ChildItem "$env:LOCALAPPDATA\Java\jdk-21*")[0].FullName
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
npm install
npm run apk     # -> android/app/build/outputs/apk/release/app-release.apk
```

## Releasing a new version

1. Bump `versionCode` (+1) and `versionName` in `android/app/build.gradle`, then `npm run apk`.
2. Copy `android/app/build/outputs/apk/release/app-release.apk` to
   `lms-angular/public/app/Daura-e-Ilmiyah.apk` and set the same `versionCode` / `versionName`
   (and a one-line `notes`) in `lms-angular/public/app/version.json`.
3. Deploy the site. Phones running an older app see a "New app version" banner with a Download
   button; the APK is always at https://duare-shariye.fly.dev/app/Daura-e-Ilmiyah.apk.

## What the app adds over the website

- Downloads (certificate PDF, CSV exports) open the Android share sheet: save, print, WhatsApp.
- Jitsi live classes open in a Chrome tab (camera and microphone work there).
- "Save for offline" on the Qur'an and hadith pages (service worker, also works on the website).
- Update banner (above). Push notifications once Firebase is set up (below).

## Push notifications (Firebase)

The server already sends every in-app notification as a push (`FcmPushSender`); it stays off
until both halves exist:

1. Firebase console -> Add project -> Add app -> Android, package `org.dauraeilmiyah.shariyah`.
   Download `google-services.json` into `android/app/` (it is not a secret).
2. `npm install @capacitor/push-notifications@8`, bump the version, `npm run apk`, release.
   (The plugin is left out until step 1: without the file it would crash on start-up.)
3. Firebase -> Project settings -> Service accounts -> Generate new private key, then
   `./deploy/fly/set-firebase-push.ps1 -KeyFile <the downloaded .json>` and delete the file.

## Signing key - back it up

`android/daura-release.keystore` + `android/keystore.properties` sign the app. They are not in git.
Phones only accept an update signed with the same key, so keep a copy somewhere safe (e.g. a private
drive). Without them a new build can't be installed over the old app.
