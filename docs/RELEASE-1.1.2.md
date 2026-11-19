# EOS 1300D Recorder Studio 1.1.2 — idle battery saving

Fixes unnecessary camera activity when both preview and recording are off. The previous Preview switch stopped frame downloads while leaving camera Live View active.

Save battery while idle is on by default and can be disabled in App preferences. Idle preview-off now ends Live View. After 60 inactive seconds the USB SDK session is released and automatic reconnect is suppressed. Start, Preview, Wake camera or a camera action reconnects. Recording, preview, transfer, focus and recording recovery are excluded from timed rest. Pending take metadata remains available after reconnect.

An active recording retains live movie mode even when preview is off. Resolution, frame rate, encoding, exposure, audio and preview quality are unchanged. Explicitly unsupported temperature properties are checked once per connection instead of repeatedly; supported temperature monitoring and battery alerts continue during active use.

Physical tests verified idle output 0, real 60-second rest, wake-and-record, preview toggles without stopping recording and return to idle after Stop. A copied original MOV retained 1920x1080 AVC at 30000/1001 fps and PCM audio. No originals were deleted. Offline tests pass for activity exclusions, wake UI, thermal lock, translations, sound and transfer verification.

No battery-runtime percentage improvement was measured. The tested camera did not independently disable its LCD on a PC-only output request. Most expected benefit is between recordings. The camera still consumes power to record full-quality movies. Temperature telemetry remains unsupported on the tested EOS 1300D/SDK; full USB power-off is not provided.
