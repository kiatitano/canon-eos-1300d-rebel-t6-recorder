# Testing and practical limits

## Offline tests

Run `scripts\Test.ps1` on Windows with .NET Framework 4.8. No camera is accessed by the offline test suite.

- PCM waveform structure, expected nine/fifteen beep bursts and bounded amplitude.
- Zero-volume behavior.
- Identical original-file copies accepted.
- Same-length bit corruption and truncated copies rejected.
- The final verification uses the exact hash established by the comparison, not a later untrusted hash.
- Cancellation before camera access cannot reach source deletion.
- English/Persian text, live direction changes and filename preservation.
- Startup UI renders without camera footage.

## Physical-camera observations

Development used a Canon EOS 1300D with firmware 1.1.0 and an EF-S 18–55mm f/3.5–5.6 III lens.

- USB Start/Stop created original MOV clips on the card.
- Preview at 1056 × 704 was decoded. Preview off/on while recording did not stop the movie. Mirroring only affected display.
- A real internal clip stopped at about 4.28 billion bytes after approximately 12 minutes. The next recording was confirmed about 2.5 seconds after the detected stop in that test.
- Verified original-file transfers completed; approved test originals were removed after validation and unrelated older takes remained on the card.
- Cancelling before transfer preserved pending originals.
- ISO, shutter, aperture, white balance, Picture Style, contrast, sharpness, saturation and color tone were changed, read back and restored.
- The camera accepted the keepalive command. A deliberately closed SDK session was automatically reconnected while USB remained plugged in.
- Face AF was exercised before recording and preview sharpness checked visually. Its earlier fixed 2.2-second interruption was replaced with live-frame feedback and a bounded configurable wait. The camera did not always report a focus-lock flag.

These observations were made during development and are not proof of all future sessions. Physical cable removal, every battery/temperature condition, all Canon SDK versions, branded T6/Kiss units and many-hour runs have not all been verified. Battery exhaustion was not deliberately forced to validate an alert.

## Manual release checklist

1. Build and run offline tests.
2. Open a fresh install in English; switch to Persian and back.
3. Check the main window and Settings at a smaller supported size.
4. Confirm no personal media, local logs or Canon DLLs enter the source/release archive.
5. With the supported camera available, connect, preview, focus, record a short test and stop. Declining transfer must preserve it.
6. Test transfer only with disposable recordings made for that test.

The normal release executable contains no hidden command-line path that automatically records test footage or approves deletion.

## Temperature capability update (1.1.1)

Both temperature-property paths were read on the physical EOS 1300D while idle and with Live View enabled; neither was supported by the installed SDK/body combination. The rebuilt engine connected successfully, reported `TemperatureAvailable=false`, and remained usable. Offline policy tests verify latched cooling behavior and unavailable telemetry; UI tests verify disabled Start/reconnect/focus/preview during a simulated warning. No camera was deliberately overheated. See [temperature details](TEMPERATURE.md).

## Idle battery saving (1.1.2)

Policy tests exclude recording, preview, transfer, focus and recording recovery from timed rest. UI tests verify wake and pending-transfer actions remain available. Physical tests verified real 60-second rest, no unwanted automatic reconnect, Start waking the camera and recording, preview toggles preserving recording, and Stop ending Live View. The copied disposable MOV retained 1920x1080 AVC at 30000/1001 fps and PCM audio. Originals were preserved. Battery-runtime savings and another full 4 GB restart run were not measured in this test. See [power saving](POWER-SAVING.md).

## Stop sounds and additional power savings (1.2.0)

Offline tests validate the two/three-note PCM patterns, short durations, quiet amplitude, mute and exactly-once stop tracking. Physical tests verified notifications for manual stop, a camera-side stop and simulated SDK/session loss. A short real recording with the engine's elapsed-time threshold advanced for testing exercised the ClipLimit notification, automatic restart and the following manual-stop notification. This was not another full 4 GB-boundary test.

Minimizing paused frame downloads; idle Live View ended, recording continued while minimized, and restoring resumed frames. Advancing the idle timer while minimized exercised rest and restore-to-preview. A cancelled transfer of a newly created disposable clip had native Live View output 0, resumed preview afterward and retained the original on the card. These tests do not measure watts or battery runtime. No personal footage, diagnostic executables or QA clips are included in the source/release.
