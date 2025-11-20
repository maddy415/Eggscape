# Android Build Guide

This project is configured for Unity 6000.0.42f1 and includes Android-specific settings so you can produce a functional Android build.

## Requirements
- **Unity 6000.0.42f1** with the **Android Build Support** modules (SDK/NDK & OpenJDK) installed through Unity Hub.
- Access to an Android device or emulator for testing.

## Build Steps
1. **Open the project** in Unity (version 6000.0.42f1).
2. **Switch the target platform**: Go to **File ▸ Build Settings**, select **Android**, and click **Switch Platform**.
3. **Verify Player Settings** (Edit ▸ Project Settings ▸ Player):
   - **Package Name**: `com.BarelyEnoughGames.Eggscape` under Android.
   - **Version**: `1.0` with **Bundle Version Code** set to `1` (update before store releases).
   - **Minimum API Level**: Android 8.0 (API 26) or higher.
   - **Target API Level**: Android 14 (API 34).
   - **Scripting Backend**: IL2CPP, **Target Architectures**: ARM64.
4. **Configure signing** (recommended for release builds): In **Player Settings ▸ Publishing Settings**, create or reference a keystore, set the **Key Alias**, and enter the passwords. For development-only builds you can leave the default debug keystore.
5. **Add scenes to the build**: In **File ▸ Build Settings**, ensure the required scenes are listed in **Scenes In Build** (the project already includes `Assets/Scenes/main_menu.unity`, `story.unity`, `tutorial.unity`, and levels 1–4).
6. **Build**: In **File ▸ Build Settings**, click **Build** (or **Build And Run**). Choose an output folder; Unity will generate an `.apk` or `.aab` depending on your selection.
7. **Test on device**: Install the build on an Android device (enable USB debugging) or upload the `.aab` to internal testing on the Play Console.

## Notes
- The project targets ARM64 only, matching current Play Store requirements.
- Update the **Bundle Version Code** and **Version** before distributing new releases.
- If you need logs while running on device, install **Android Logcat** via the Package Manager for convenient log streaming.
