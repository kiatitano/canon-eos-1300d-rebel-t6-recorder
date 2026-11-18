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
