# Valheim VOIP

Experimental BepInEx plugin for proximity voice chat in Valheim.

## Current MVP

- Captures the first Unity microphone device.
- Encodes 16 kHz mono microphone frames with Opus through Concentus.
- Sends compact Opus packets through Valheim routed RPC.
- Relays packets from the server only to peers within the configured proximity radius.
- Clients automatically send voice to the current Valheim server peer; no separate VOIP address or port is configured.
- The server periodically syncs session voice settings to connected clients.
- Plays received speech as spatial Unity audio at the speaker position.
- Defaults to push-to-talk on `V`; voice activation can be enabled in the BepInEx config.

## Client and server behavior

Install `ValheimVoip.dll` on both the dedicated server and every client.

Players do not configure a voice server per world. When a client joins a Valheim server, the mod uses the existing Valheim network session:

- client captures and encodes local voice,
- client sends voice frames to `ZNet.GetServerPeer()`,
- server listens on the same routed RPC,
- server relays only to nearby peers,
- server syncs authoritative session settings such as radius, sample rate, Opus bitrate, and frame size.

Local client config is still useful for personal input and playback choices, such as push-to-talk key, voice activation, and playback volume.

## Build

This project targets `net462` and references the assemblies from the adjacent dedicated server install.

On this machine, the working local build path is:

```powershell
.\build.ps1
```

That compiles with the .NET Framework compiler against Unity's own Mono assemblies and deploys `ValheimVoip.dll` into `..\..\BepInEx\plugins\ValheimVoip`.

If you install a modern .NET SDK, this project can also be built with:

```powershell
dotnet build .\ValheimVoip.csproj
```

Debug SDK builds are copied to:

```text
..\..\BepInEx\plugins\ValheimVoip
```

The legacy Framework MSBuild included with Windows cannot read SDK-style projects.

## Important next steps

The voice path uses Concentus source compiled directly into `ValheimVoip.dll`. No native Opus library or separate Concentus DLL is required.

Recommended follow-up work:

- add input device selection,
- add a tiny HUD indicator for transmit/receive state,
- add jitter buffering instead of one `AudioClip` per packet,
- add mute/deafen controls,
- add server config sync so distance and bitrate cannot diverge per client.
