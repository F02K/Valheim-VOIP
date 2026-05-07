using System.Collections.Generic;
using UnityEngine;

namespace ValheimVoip
{
    internal sealed class VoicePlayback : MonoBehaviour
    {
        private readonly Dictionary<long, AudioSource> _sources = new Dictionary<long, AudioSource>();
        private readonly Dictionary<long, OpusVoiceCodec> _codecs = new Dictionary<long, OpusVoiceCodec>();

        public void Play(VoicePacket packet)
        {
            if (packet.OpusPayload == null || packet.OpusPayload.Length == 0 || packet.Samples <= 0)
            {
                return;
            }

            AudioSource source = GetSource(packet.SpeakerId);
            source.transform.position = packet.SpeakerPosition;
            source.maxDistance = VoiceRuntimeSettings.ProximityMeters;
            source.minDistance = VoiceRuntimeSettings.FullVolumeMeters;
            source.volume = VoiceSettings.PlaybackVolume.Value;

            AudioClip clip = AudioClip.Create(
                "voip-" + packet.SpeakerId,
                packet.Samples,
                1,
                packet.SampleRate,
                false);

            clip.SetData(GetCodec(packet.SpeakerId).Decode(packet.OpusPayload, packet.Samples, packet.SampleRate), 0);
            source.PlayOneShot(clip, VoiceSettings.PlaybackVolume.Value);
        }

        private AudioSource GetSource(long speakerId)
        {
            AudioSource source;
            if (_sources.TryGetValue(speakerId, out source) && source != null)
            {
                return source;
            }

            GameObject sourceObject = new GameObject("VOIP Speaker " + speakerId);
            DontDestroyOnLoad(sourceObject);
            source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            _sources[speakerId] = source;
            return source;
        }

        private OpusVoiceCodec GetCodec(long speakerId)
        {
            OpusVoiceCodec codec;
            if (_codecs.TryGetValue(speakerId, out codec) && codec != null)
            {
                return codec;
            }

            codec = new OpusVoiceCodec();
            _codecs[speakerId] = codec;
            return codec;
        }

        private void OnDestroy()
        {
            foreach (AudioSource source in _sources.Values)
            {
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }

            _sources.Clear();
            _codecs.Clear();
        }
    }
}
