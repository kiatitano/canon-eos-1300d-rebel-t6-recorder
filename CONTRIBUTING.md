# Contributing

Please open an issue with a concrete problem or proposed improvement before a large change. Useful contributions include camera-state handling, clearly scoped hardware tests, accessibility, translations and reproducible fixes.

1. Work on Windows with .NET Framework 4.8.
2. Run `scripts\Test.ps1` before proposing a change.
3. Keep EDSDK calls on the camera worker thread. Do not run parallel native calls from UI callbacks.
4. Preserve English defaults, Persian support and unsupported-control explanations.
5. Never weaken the copy/verification-before-delete boundary. A successful download alone is not permission to delete an original.
6. Keep hardware-destructive tests separate from offline tests and use only recordings created for the test.
7. Do not commit camera footage, personal screenshots, logs, credentials, Canon SDK binaries or machine-specific preferences.

The model guard is intentional. Additional models need implementation review and actual tests; changing the name check alone does not establish compatibility. Describe what was tested and what remains unknown.

Source is MIT-licensed. Canon's separately installed software remains under its own terms; do not add vendor DLLs to a contribution or release.
