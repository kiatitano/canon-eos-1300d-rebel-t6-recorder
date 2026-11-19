# EOS 1300D Recorder Studio 1.2.0 — stop sounds and better battery saving

A Windows USB video-recording companion for Canon EOS 1300D / Rebel T6, with English default and Persian support.

## New

- Quiet custom stop sounds: two short notes for manual stops and clip limits; three for unexpected stops, loss of recording monitoring or a thermal stop. Independent volume, mute and test buttons. Notifications are deduplicated per confirmed clip and work while minimized.
- Pause hidden USB preview requests while minimized; resume the same quality when restored. Active camera recording continues.
- Pause camera Live View during original-file transfers when idle battery saving is enabled, then restore requested preview after completion or cancellation.
- Includes the earlier idle saver: ends Live View when recording/preview are off, releases the USB session after 60 idle seconds, and reconnects on Start/Preview/Wake. Unsupported temperature properties are not repeatedly polled.
- Portable ZIP now includes the public guides and app-only screenshots alongside the executable.

## Verified

Offline tests pass for short PCM sounds, exactly-once stop tracking, idle policy, cooling lock, bilingual UI and transfer integrity. Physical EOS 1300D tests verified manual/camera-side/session-loss stop notifications, minimize/restore without interrupting recording, and paused Live View during a cancelled transfer with the source retained. A short recording with the time threshold advanced for testing exercised rollover notification and restart; another full 4 GB test was not run for this release. Prior file inspection confirmed 1920x1080 AVC at 30000/1001 fps with PCM audio. These changes do not write movie quality, frame-rate, exposure or audio settings and do not re-encode movies.

## Important limits

A disconnect sound reports loss of monitoring; it does not prove the camera stopped saving. Audio requires a running app and usable PC sound output. Speakers may be picked up by the camera microphone; headphones avoid acoustic pickup.

Battery runtime improvement has not been measured. Independent LCD shutdown via SDK was not accepted by the tested body. The tested EOS 1300D/SDK does not expose temperature warning telemetry; it shows Unavailable. Where a warning is supported, the app attempts stop/save and latches a cooling lock. Full power-off requires the physical OFF switch.

## Download and setup

Download **EOS1300DRecorder-1.2.0-win-x86.zip**, extract it and run `EOS1300DRecorder.exe`. Requires Windows, .NET Framework 4.8 and separately installed compatible **32-bit Canon EOS Utility/EDSDK**. Close EOS Utility before connecting. No Canon DLLs, personal media or diagnostic tools are included. The binary is unsigned. SHA256SUMS.txt accompanies the ZIP.

[Full guide](https://github.com/kiatitano/canon-eos-1300d-rebel-t6-recorder#readme) · [Stop sounds](https://github.com/kiatitano/canon-eos-1300d-rebel-t6-recorder/blob/main/docs/STOP-SOUNDS.md) · [Power saving](https://github.com/kiatitano/canon-eos-1300d-rebel-t6-recorder/blob/main/docs/POWER-SAVING.md)
