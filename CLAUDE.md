# Project context (read me first)

## Goal

Get **Factorio (Nintendo Switch version) running well in MeloNX on an iPad Pro
12.9" 6th gen (M2, iPadOS 26.5.2)**, then tune the touch/virtual-controller
layout for the big screen. The user owns the game/console; keys, firmware, and
game dumps come from their own Switch.

## What this repo is

MeloNX — a Nintendo Switch emulator for iOS (Ryujinx C# core + Swift/SwiftUI
frontend). Upstream lives on a Forgejo instance at
<https://git.ryujinx.app/projects/MeloNX> (NOT GitHub — GitHub hits are
unofficial mirrors). This working copy pushes to the user's fork:
<https://github.com/nurtrino/MeloNX> (remote name `github`, branch `master`).

Architecture: `src/Ryujinx.Library` is NativeAOT-compiled to
`Ryujinx.Library.dylib` by `distribution/ios/build.sh` (invoked by a
PBXLegacyTarget named "Ryujinx" inside the Xcode project). The Swift app at
`src/MeloNX/MeloNX.xcodeproj` (scheme `MeloNX`) links that dylib plus prebuilt
xcframeworks committed under `src/MeloNX/MeloNX/Dependencies/`.

## State as of 2026-08-01 (session on the user's Windows PC)

Done:

- Added `.github/workflows/build-ipa.yml`: builds an **unsigned IPA** on a
  `macos-26` runner (the Release config targets the iOS 26.2 SDK, so Xcode 26
  is required) and uploads it as artifact `MeloNX-unsigned-ipa`. Runs on every
  push to master.
- **Xcode 26.6 compiler bug workaround**: the archive step passes
  `SWIFT_OPTIMIZATION_LEVEL=-Onone` because the Swift 6.3.3 optimizer
  (EarlyPerfInliner pass) segfaults compiling the `Setting` class deinit in
  `src/MeloNX/MeloNX/UI/Main/Settings/NativeSettingsManager.swift`. Only the
  Swift UI is affected; the C# core is fully optimized. If building locally
  with a different Xcode, `-O` may or may not crash the compiler — try it.
- **Fixed real crashes exposed by -Onone** (debug preconditions are active in
  such builds; upstream's optimized builds silently read out of
  bounds/misaligned). Commit `cde6f1b0f`:
  - `UI/MeloNXApp.swift` — `lastAppversion` is empty `Data()` on first
    launch; loading a `Float` from it crashed on the first SwiftUI frame
    (this was diagnosed from a real device .ips crash log).
  - `Core/Models/CallbackData.swift`, `Common/StatisticsHandler.swift`
    (fifo at byte offset 17 — never 8-aligned), `Core/Models/RumbleData.swift`
    — all switched to `loadUnaligned`.
- **CI core caching** (commit `f66b048cb`): `actions/cache` keys the compiled
  core on a hash of all non-Swift sources; on hit, a `.ci-core-cache-hit`
  marker file makes `build.sh` skip the ~20-min NativeAOT publish, so
  Swift-only pushes build in a few minutes.
- Published unsigned IPA to
  <https://github.com/nurtrino/IOS-Games/releases/tag/melonx-v2.5>. That asset
  predates the crash fixes — **replace it with a fixed build** (crash-fix run
  30731129827 on nurtrino/MeloNX was in progress when this session ended).

Device/install status:

- User installs via **Signulous** (paid signing service). First install
  crashed on launch → that was the `lastAppversion` bug above, fixed but not
  yet shipped to the device.
- When re-uploading to Signulous: enable the **increased memory limit**
  entitlement (required by MeloNX; the app's only required entitlement —
  extended-virtual-addressing is NOT needed on this iPad). If there's a
  **get-task-allow** option, enable it for JIT.
- **JIT plan**: StikDebug from the App Store as the JIT provider (select it in
  MeloNX settings). MeloNX 2.5 has iOS-26/TXM handling built in
  (`Common/JIT/EnableJIT.swift`, `Common/JIT26Breakpoint.swift`). TrollStore
  is not possible on iOS 26.

## Building on this Mac

1. Requirements: Xcode 26.x, .NET 10 SDK (`global.json` wants 10.0.100+;
   `brew install dotnet-sdk` or the official installer). `build.sh` probes
   `/usr/local/bin` and `/opt/homebrew/bin` for `dotnet`.
2. Open `src/MeloNX/MeloNX.xcodeproj`, scheme `MeloNX`, and build — the
   legacy target compiles the C# core automatically (slow the first time,
   incremental afterwards).
3. With an Apple ID signed into Xcode, you can sign and run directly on the
   iPad over USB/Wi-Fi — much faster iteration than CI + Signulous. Debug
   builds are `-Onone` by default, which is fine (and matches what the crash
   fixes were hardened for).
4. Delete the local `.ci-core-cache-hit` file if it ever exists locally —
   it's a CI-only marker that makes `build.sh` skip the core build.

## Next steps

1. Ship a crash-fixed build to the device (local Xcode build, or grab the CI
   artifact / updated release) and confirm MeloNX launches.
2. First-run setup in app: import `prod.keys`, firmware, then the user's
   Factorio dump. Set JIT provider to StikDebug.
3. Get Factorio booting; tune graphics/memory settings for the M2 iPad.
4. Then the fun part: adjust the virtual controller overlay for the 12.9"
   screen (`Core/Controllers/VirtualController.swift`,
   `Core/Models/OverlayPosition.swift`, per-game settings). Factorio's Switch
   port supports the touchscreen natively (tap moves the cursor, touch
   inventory management) and MeloNX passes iPad touches through, so overlay
   placement should stay out of the way of touch play.
