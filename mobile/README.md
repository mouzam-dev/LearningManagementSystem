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

For a new release, bump `versionCode` (+1) and `versionName` in `android/app/build.gradle`.

## Signing key - back it up

`android/daura-release.keystore` + `android/keystore.properties` sign the app. They are not in git.
Phones only accept an update signed with the same key, so keep a copy somewhere safe (e.g. a private
drive). Without them a new build can't be installed over the old app.
