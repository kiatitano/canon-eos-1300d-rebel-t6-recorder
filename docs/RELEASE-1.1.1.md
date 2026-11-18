# EOS 1300D Recorder Studio 1.1.1 — temperature capability reporting

Adds a visible Thermal status field and conditional protection for camera-reported temperature warnings.

**Important result:** the physically tested EOS 1300D with the installed Canon SDK does **not** expose a usable temperature warning status. It correctly shows **Unavailable**. This release cannot give a reliable heat-triggered alarm on that combination. Watch the temperature warning on the camera itself.

Where supported, a reported warning triggers a stop/save attempt, ends USB preview, suppresses keepalive and locks Start, clip restart, reconnect, focus and transfer until the app is restarted after cooling. A persistent banner and repeated custom critical beep ask the user to turn the camera OFF. It never sends an undocumented power-off command; USB session closure is not physical shutdown. Thermal protection cannot guarantee prevention of damage.

Tests pass for temperature-state handling, warning latching, unsupported-property fallback, locked UI controls, bilingual UI, custom beep synthesis and file verification. The rebuilt engine was also connected to the actual camera and correctly reported unavailable thermal telemetry. Real overheating was not induced; a real thermal stop/save remains unverified.

Download **EOS1300DRecorder-1.1.1-win-x86.zip** and extract it. Requires Windows with .NET Framework 4.8 and separately installed compatible 32-bit Canon EOS Utility/EDSDK. English is default; Persian is available. No Canon binaries or personal media are included. SHA256SUMS.txt accompanies the archive.

[Full temperature behavior and limits](https://github.com/kiatitano/canon-eos-1300d-rebel-t6-recorder/blob/main/docs/TEMPERATURE.md)
