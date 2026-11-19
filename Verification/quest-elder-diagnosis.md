# Quest Elder House exit investigation — 2026-11-19

## Confirmed on the connected Quest 3S

`quest-elder-crash-before.txt` records crashes at 11:50:03 and 11:51:08 (UTC+8), in `com.Unity.ModernRoom`, on `Loading.Preload`, with SIGTRAP / SI_TKILL. Android reports `Killed-By-AM: No`. These are native Unity process crashes, not an ordinary return to the village or a managed NullReferenceException.

The installed player's Unity version is 6000.0.80f1. Its `libunity.so` build ID is `9515966d488c188f`. The matching local `Library/Bee/artifacts/Android/libunity/arm64-v8a/libunity.sym.so` has the same build ID. `addr2line -Cf` resolves the relevant program counters to:

```
0x6810b8 CachedReader::OutOfBoundsError
0x681340 CachedReader::UpdateReadCache
0x53400c TransferPPtr<StreamedBinaryRead>
0x78bb80 TransferField<..., Converter_UnityEngineObject>
0x782f50 Transfer_UnityEngineObject
0x78c678 ExecuteSerializationCommands
0x7ab9a0 TransferScriptingObject
0x7c4358 SerializedFile::ReadObject
0x7bf10c PersistentManager::LoadFileCompletelyThreaded
0x673954 LoadSceneOperation::Perform
0x6752a8 PreloadManager::ProcessSingleOperation
```

This establishes an out-of-bounds read while deserializing a scripted object's Unity object reference during scene loading. The stack does **not** identify the specific component, prefab, or serialized field. The user's Elder House entry is the reported trigger; this capture alone does not label the scene being loaded.

## Other messages and limits

The subsequent launch capture (`quest-elder-current.txt`) includes `OnTriggerEnter2D` parameter `[UNREGISTERED]` warnings and an OpenXR session-not-ready warning. That launch paused without gaining focus. These messages have not been shown to cause the recorded serialization crash. No successful headset gameplay test is claimed.

## Change and next verification

Added `Forgotten Village > Build Quest (clean scene data)` in `QuestBuildAutomation.cs`. This builds all enabled scenes for Android using `BuildOptions.CleanBuildCache`, generating `Builds/ForgottenVillage-Quest.apk` and a build result under `Verification/quest-build-result.txt`.

Validation: the clean Android IL2CPP build completed successfully with **0 build errors** on 2026-11-19 at approximately 12:05 UTC+8 (APK approximately 60 MB).

This is a controlled rebuild to test stale/inconsistent build data as a possible cause, **not proof that stale data caused the crash**. It does not alter the authored houses or gameplay. The connected headset was disconnected by the user before post-build installation/testing.

After reconnecting, install the rebuilt APK with `adb install -r` (retain app data), capture the full Unity log while reproducing Elder House entry, then test repeated entry/exit. If the same crash remains, capture the pre-crash serialized-file error and isolate scene dependencies; do not treat an Editor-only lifecycle pass as Android validation.
