# Battery use and idle power saving

The app previously forced camera Live View output to both PC and LCD whenever it connected or began a clip. The Preview switch only stopped downloading images. Together with periodic keepalive, that could keep the camera active even when the app showed no preview and no recording.

## Default behavior

**Settings > App preferences > Save battery while idle** is enabled by default. It does not change resolution, frame rate, encoding, exposure, Picture Style, audio, preview dimensions or preview frame rate.

- When both recording and preview are off, the app writes Live View output 0 and verifies the camera accepted it.
- During recording, live movie mode remains enabled even if preview is off. Removing that mode can interrupt a clip; this app does not do that to save battery.
- During preview, full-resolution USB preview continues as before.
- After 60 seconds with no recording, preview, transfer, focus or recording recovery, the SDK session is closed. Keepalive and automatic reconnect are suppressed during this intentional rest.
- Start, Preview, Wake camera, camera controls or transferring pending takes reopen the connection. If the camera has physically switched off or USB is unavailable, it may need to be turned on manually.
- Pending take metadata remains in memory during rest. No original is deleted by resting. Battery and temperature polling pause until the camera reconnects; the UI does not show a stale reading as current.
- Explicitly unsupported temperature properties are probed once per connection instead of every two seconds. Transient/unknown read errors are not cached as unsupported.

You can turn the option off to retain an always-awake camera session. Active recording, transfer and preview are never suspended by the idle timer. Cooling lock takes precedence over any wake request.

## Hardware observations

On the tested EOS 1300D, idle output 0 was accepted and the recording-state property returned 0. Preview output 3 produced normal frames. A PC-only output request (2) was coerced by the camera to 0x80000003, retaining the LCD flag; independent LCD shutdown was therefore not verified. Writing EVF Mode 0 was rejected with 0x81. No undocumented LCD or power-off commands are used.

The battery reports only coarse levels. There is no measured percentage or minutes-of-runtime improvement claim. Most savings are expected during idle time; the sensor and encoder still have to operate while recording at full quality. An aging battery and temperature can also affect runtime. For long unattended sessions, use an appropriate power solution supported by the camera rather than expecting software to eliminate recording power consumption.

## Completed hardware verification

The actual 60-second idle timeout released the session. It remained resting past the normal automatic-reconnect interval. Start reopened the session and began recording. Preview on/off retained recording state 4, and Stop returned output and recording state to 0. A disposable clip was copied for stream inspection without deleting its card original. Its AVC video remained 1920 x 1080 at 30000/1001 fps with the same PCM audio codec as an earlier original MOV. No re-encoding or quality-setting writes were introduced. These short tests do not measure battery runtime or repeat the earlier full 4 GB-boundary test.

## Additional savings in 1.2.0

**Pause hidden preview when minimized** is enabled by default. The app stops requesting unseen USB preview frames while minimized and resumes the same preview resolution/rate when restored. During active recording, required live movie mode is kept and the movie continues unchanged. When otherwise idle, a minimized preview releases Live View and can enter idle rest; restoring a window with preview enabled reopens the session.

With idle battery saving enabled, Live View is also paused before moving completed files and restored after completion/cancellation if preview was requested. Transfers keep the SDK session and do not trigger the idle timer. This does not transcode or alter the original file.

Physical tests verified paused hidden frames, idle output 0, restore-to-preview, recording continuity while minimized, and transfer output 0 followed by preview restoration after cancellation. Runtime savings remain unmeasured.

For an additional body-side adjustment, lower **LCD brightness** in the camera menu to a comfortable level. This is display brightness, not movie exposure. Canon lists frequent LCD use and frequent autofocus among factors that consume more battery; the app already performs face AF only on request. It does not force continuous AF, disable lens stabilization or reduce movie resolution to claim a battery saving. Independent LCD-off through this SDK was not accepted by the tested body.

Sources: [Canon battery-use guidance](https://www.canon.com.au/support/sims-content?cid=E51D6D6A30174A31A50DF4AEEF4F357D&pid=1f51a2dcb60d46559562084a4f1b5ea8), [Canon Live View and LCD brightness guidance](https://in.canon/en/support/8202836400).
