# Recording stop sounds

A custom, quiet sound is enabled by default. It is generated locally as PCM audio and does not use the Windows default notification sound.

- **Two descending notes (about 0.22 seconds):** a manual stop or a known clip-size/time boundary.
- **Three descending notes (about 0.33 seconds):** an unexpected camera-side stop, lost recording monitoring/SDK session, or thermal protection.

Settings > App preferences > Recording stop sound provides enable/mute, independent volume and two test buttons. The default stop volume is 35%. Muting the battery reminder does not mute stop sounds. Stop sounds continue while the window is minimized.

The engine arms one notification after a recording is confirmed. Stopping, then encountering another error for the same clip, does not generate a duplicate. A restarted or recovered recording arms the next notification. Idle camera rest, preview switching and pressing Stop while already idle do not generate a stop sound. Closing the app during an active recording uses the existing stop/save confirmation.

Clip-limit sounds do not delay automatic restart. Stop events are dispatched independently of the UI's recording-state snapshots, so a short rollover gap is still reported. Audio runs in a bounded background queue shared with battery/thermal beeps; a stop cue can wait for an alarm that is already playing rather than overwriting it or blocking camera commands.

A connection-loss sound means the app lost recording monitoring. It does **not** prove the physical camera stopped or that a movie was finalized. In a physical test, closing the SDK session stopped the camera; other faults may leave it running. The app does not silently start a new recording when reconnecting to a stopped camera.

No sound is inserted into the MOV file. Speakers can still be picked up by the camera microphone; headphones avoid that acoustic pickup. Sounds require a running app, working PC audio and an unmuted output. They cannot notify after the computer loses power or the app crashes.
