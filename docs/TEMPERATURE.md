# Temperature warnings and cooling lock

This feature reads a camera-reported warning state, not a Celsius measurement. It cannot guarantee prevention of damage or replace the camera's own protections.

## Actual EOS 1300D test

A connected EOS 1300D was probed while idle and with USB Live View enabled. The installed SDK returned 0x00000009 for the modern temperature property (0x01000415) and 0x00000050 for the legacy property (0x00000415). Neither supplied a usable warning state. This combination displays **Unavailable**. Watch the temperature icon on the camera itself; the app cannot provide a reliable temperature-triggered alarm when the camera does not expose that information.

## If a warning is exposed

A valid zero means only **No warning reported**. Any valid nonzero value conservatively latches the cooling lock. The app attempts to stop and save its recording before releasing Live View and the SDK session. It stops extending the idle shutdown timer and blocks Start, autofocus, preview, restart, transfer and reconnect for this app session. A later normal or missing sample cannot clear the lock. Card originals are retained; no transfer/deletion starts during cooling.

The UI shows a persistent warning and repeats the custom critical beep every 30 seconds using the app volume. This thermal alarm is independent of the battery reminder mute. If recording stop cannot be confirmed, it explicitly asks you to use the camera controls. Use the camera's OFF switch after recording has finished saving, and allow it to cool. Reopen the app only after cooling. The app does not send an undocumented power-off command, and closing an SDK session does not mean the camera is physically off. Live View stop requests are best effort and may be rejected by the body.

Polling takes place approximately every two seconds while the camera worker is available, and before starting each clip. Long SDK operations and USB failures can delay it. Unsupported, invalid or failed readings are shown as unavailable, never as a reassuring normal state.

## Verification

Offline tests cover unavailable/normal/warning states, latching across later normal/missing samples, unknown warning codes, and modern-to-legacy property fallback. Physical tests confirm unsupported telemetry on this unit. The camera was not deliberately heated; real overtemperature shutdown and thermal stop/save on this body are unverified.
