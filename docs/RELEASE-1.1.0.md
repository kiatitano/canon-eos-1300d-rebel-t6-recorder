# EOS 1300D Recorder Studio 1.1.0

The first public Windows release of the USB recording companion for Canon EOS 1300D / Rebel T6 users.

## Download and setup

Download **EOS1300DRecorder-1.1.0-win-x86.zip**, extract it, install compatible **32-bit Canon EOS Utility / EDSDK** separately, close EOS Utility, and run `EOS1300DRecorder.exe`. Windows with .NET Framework 4.8 is required. The app requests no administrator privileges. Canon SDK binaries are not bundled.

## Included

- English by default, with Persian available through live language switching.
- Dark studio interface with optional USB Live View and mirrored preview.
- USB Start/Stop and automatic restart after a verified clip size/time limit.
- Pre-recording Face Detection Live AF with an adjustable wait.
- Supported ISO, shutter, aperture, white balance and Picture Style controls.
- Approximate battery level, low/critical banners and repeated custom beep alerts.
- Configurable destination folder, SDK location, preview startup and reconnect preferences.
- Post-recording transfer prompt, progress/cancellation and SHA-256 verification before deleting a verified card original.
- MIT-licensed C# source, build scripts, offline verification tests and English/Persian guides.

## Read before recording

Movies are recorded to the **SD card first**. Original files are moved to the PC only after recording and confirmation. USB preview is lower resolution than the movie. Automatic restart leaves a short gap between separate clips and is **not seamless unlimited recording**. Battery values are coarse, not precise one-percent readings. Resolution, frame rate and movie audio remain camera-menu settings. No firmware modifications are made.

Physical development tests used an EOS 1300D, firmware 1.1.0. Rebel T6 and Kiss X80 regional names are recognized but have not been separately hardware-tested. Other models, including T6i/T6s, are unsupported. Offline release tests pass for custom sound, transfer integrity/cancellation and bilingual UI. See [testing details](docs/TESTING.md) and [troubleshooting](docs/TROUBLESHOOTING.md).

The binary is not code-signed. `SHA256SUMS.txt` accompanies this release so you can check that your ZIP matches the published archive. This project is independent of Canon.
