# Architecture

Valheim VOIP is split by runtime responsibility rather than by feature name.

## Client

Client code runs on game clients and should not make authoritative decisions.

- `VoiceCapture`: reads Unity microphone data, applies push-to-talk or voice activation, encodes frames.
- `VoiceClient`: sends encoded frames to the current Valheim server peer and accepts settings only from that server.
- `VoicePlayback`: decodes received frames and plays spatial audio at the speaker position.

## Server

Server code runs on hosts and dedicated servers.

- `VoiceServer`: relays voice frames to nearby peers only, and periodically broadcasts authoritative voice settings.

The server never trusts a client to decide who should hear a packet.

## Shared

Shared code is safe to use from both sides.

- `VoiceNetwork`: registers routed RPCs and dispatches packets to client/server components.
- `VoicePacket`: wire format for encoded voice frames.
- `VoiceRuntimeSettings`: effective session settings and server sync serialization.
- `VoiceSettings`: BepInEx config bindings.
- `OpusVoiceCodec`: Concentus-backed encoder/decoder wrapper.
- `AudioMath`: small audio helpers.
- `ValheimVoipPlugin`: BepInEx entry point and component wiring.

## Packet Flow

```text
Client microphone
  -> VoiceCapture
  -> OpusVoiceCodec.Encode
  -> VoiceClient.Send
  -> Valheim routed RPC
  -> VoiceServer.Relay
  -> nearby client routed RPC
  -> VoiceNetwork
  -> VoicePlayback
  -> OpusVoiceCodec.Decode
  -> Unity AudioSource
```

## Settings Flow

```text
Server BepInEx config
  -> VoiceRuntimeSettings.CreateServerPackage
  -> VoiceServer broadcast
  -> VoiceClient.ApplyServerSettings
  -> client runtime settings
```

Server settings override client session values during multiplayer. Personal client settings such as local push-to-talk and playback volume remain local.
