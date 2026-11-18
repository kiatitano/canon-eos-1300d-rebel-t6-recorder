# Architecture

The app is a .NET Framework/WPF x86 desktop executable. Loose XAML is embedded as a resource and loaded by the STA entry point. There is no web backend, account system, package restoration or media transcoder.

## Camera ownership

`CameraEngine` owns a dedicated STA worker thread and a command queue. All EDSDK calls and event pumping stay on that worker. The WPF UI receives snapshots and bounded frame updates through the Dispatcher; sound playback uses another background worker.

The engine distinguishes app-owned recording, idle, transfer and focus operations. It does not start new recording merely because a camera reconnects. A surviving app-owned recording can resume monitoring after a session loss.

## Safe transfer boundary

Pending take metadata contains name, size and camera timestamp. Transfer resolves a unique matching source, downloads to temporary files, validates both copies and a finalized local file against the anchored SHA-256, then deletes that verified source. The finalized destination is held against writers through deletion. The SDK implicitly releases a successfully deleted directory item; code avoids releasing it twice.

Cancellation is checked before source access/deletion and between download chunks. Earlier completed moves can coexist with later pending sources. Pending state is intentionally session-local for now.

## UI and localization

Main-window and Settings controllers are separate partial-class files. User confirmations are behind `IUserDialogs`, allowing tests to decline destructive operations without bypassing the production prompt path.

Source strings are English. The Persian catalog is an embedded UTF-8 JSON dictionary. UI boundary helpers preserve English source text for live switching, adjust designated text-flow regions and translate known boundary fragments without replacing words inside filenames or native SDK identifiers. SDK diagnostics can remain in English. Add new user-visible source strings to the Persian catalog when changing UI.

## Preferences and portability

Preferences are stored locally as XML and written through a temporary-file replacement. SDK discovery verifies 32-bit PE architecture. The transfer directory is configurable and defaults to the user's Videos folder; the old private prototype's destination can migrate on that machine.

`Interop` holds only interface declarations, not vendor binaries. The build script generates the original icon, embeds XAML/catalog resources and compiles with the local .NET Framework compiler.
