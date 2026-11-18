# Setup and troubleshooting

## Canon SDK installation and architecture

The executable is a 32-bit process. It can only load a compatible 32-bit `EDSDK.dll` and the native dependencies expected by that installation. The app examines the DLL's PE header when selecting a folder; a 64-bit library is rejected.

Install Canon's compatible EOS Utility distribution separately. Detection checks the configured SDK folder and common Canon EOS Utility installation folders. You can select the correct folder in App preferences. Merely renaming a 64-bit DLL or copying it beside the app will not make it compatible.

If the DLL exists but cannot be loaded, its dependencies may be missing or incompatible. Use a complete vendor installation rather than a random DLL download. The repository and release intentionally contain no vendor DLLs.

## Camera busy or not found

Close EOS Utility, another recorder, webcam software or a second app instance that holds the camera. Use a USB data cable and check that Windows recognizes the camera. Set the camera to Movie mode. Camera Wi-Fi settings can prevent USB operation in some configurations; check the camera menu/manual.

Automatic reconnect retries a failed connection when enabled. It cannot power on a camera with an empty battery. After battery replacement, turn the camera on and wait for Windows to recognize it. If the connection stays unavailable, reconnect manually and inspect the log.

## Preview and focus

The USB preview is a separate lower-resolution image, not the original MOV data. A preview can look softer or noisier than a saved movie. Inspect an original movie before diagnosing the final recording quality from preview alone.

For software-controlled focus, switch the physical lens to AF. Stand/sit in the intended position and add enough face light. The face-focus action keeps downloading live frames while the camera operates and waits for reported confirmation, up to the configured timeout. Some camera states do not report a usable confirmation; the app tells you to check the image instead of guaranteeing focus.

If face selection is unreliable, try the supported single-point AF method or small nearer/farther adjustments. A lens's minimum focus distance, movement, low contrast and a distracting background can all matter. This app does not add modern eye tracking or uninterrupted movie servo AF.

## Long recordings

The camera still records each clip to its card. The app only restarts a verified limit; manual early stops and arbitrary failures are not treated as permission to keep recording. Expect a short gap. A full card, heat, low battery or a disconnected cable can end the session.

Windows sleep prevention is active while this app owns recording or transfer work. Manual sleep/shutdown and power loss still interrupt it. The camera's SDK keepalive does not defeat thermal protection or make a depleted battery usable.

## Transfers and available space

Choose a writable local destination. The required free space includes all pending originals plus a temporary second copy of the largest file and a margin. Reading every file twice is deliberate: it supports verification before card deletion, at the cost of extra time and space.

If a transfer fails, do not remove the only good card copy. The app leaves unverified sources on the card. A cancellation can happen after earlier files completed, so inspect both the destination and pending count. Temporary `.partial` and `.verify` files are not declared final originals.

The pending list is not a general card browser and is not persisted across app restarts. For recordings from earlier app runs, use EOS Utility or a card reader.

## Battery alerts

The camera's normal/half/low/empty indications are coarse. The displayed number is not an accurate remaining-time estimate. Choose the 50% early warning if the low level gives insufficient time in your setup.

Use Test beep alert, check the app's volume and confirm the Windows output device is not muted. The custom waveform is played through the selected Windows audio output; the app does not override system volume.

## Reporting a problem

Include camera model/firmware, lens, Windows version, SDK architecture, steps to reproduce and the relevant log section. State whether recording, preview or transfer was active. Remove personal paths/data. Do not attach private footage or vendor DLLs unless a separate appropriate process is agreed.
