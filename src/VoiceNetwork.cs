using System;
using UnityEngine;

namespace ValheimVoip
{
    internal sealed class VoiceNetwork : MonoBehaviour
    {
        private const string RpcName = "ValheimVoip_VoiceFrame";
        private const string SettingsRpcName = "ValheimVoip_Settings";
        private const float SettingsBroadcastInterval = 10f;

        private VoicePlayback _playback;
        private bool _registered;
        private ZRoutedRpc _registeredRpc;
        private float _nextSettingsBroadcast;

        public void Initialize(VoicePlayback playback)
        {
            _playback = playback;
        }

        private void Update()
        {
            if (ZRoutedRpc.instance == null)
            {
                _registered = false;
                _registeredRpc = null;
                VoiceRuntimeSettings.ClearServerSettings();
                return;
            }

            if (!_registered || _registeredRpc != ZRoutedRpc.instance)
            {
                ZRoutedRpc.instance.Register<ZPackage>(RpcName, OnVoiceFrame);
                ZRoutedRpc.instance.Register<ZPackage>(SettingsRpcName, OnSettings);
                _registered = true;
                _registeredRpc = ZRoutedRpc.instance;
                _nextSettingsBroadcast = 0f;
                ValheimVoipPlugin.Log.LogInfo("Voice RPC registered");
            }

            if (ZNet.instance != null && ZNet.instance.IsServer() && Time.time >= _nextSettingsBroadcast)
            {
                BroadcastServerSettings();
                _nextSettingsBroadcast = Time.time + SettingsBroadcastInterval;
            }
        }

        public void Send(VoicePacket packet)
        {
            if (!_registered || _registeredRpc != ZRoutedRpc.instance || ZRoutedRpc.instance == null || ZNet.instance == null)
            {
                return;
            }

            if (ZNet.instance.IsServer())
            {
                RelayFromServer(ZNet.GetUID(), packet);
                return;
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(serverPeer.m_uid, RpcName, packet.ToPackage());
        }

        private void OnVoiceFrame(long senderPeerId, ZPackage package)
        {
            if (!VoiceRuntimeSettings.Enabled)
            {
                return;
            }

            VoicePacket packet;
            try
            {
                packet = VoicePacket.FromPackage(package);
            }
            catch (Exception ex)
            {
                ValheimVoipPlugin.Log.LogWarning("Dropped malformed voice packet: " + ex.Message);
                return;
            }

            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                RelayFromServer(senderPeerId, packet);
                if (Player.m_localPlayer != null && packet.SpeakerId != Player.m_localPlayer.GetPlayerID())
                {
                    _playback.Play(packet);
                }

                return;
            }

            if (Player.m_localPlayer != null && packet.SpeakerId == Player.m_localPlayer.GetPlayerID())
            {
                return;
            }

            _playback.Play(packet);
        }

        private static void RelayFromServer(long senderPeerId, VoicePacket packet)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                return;
            }

            float maxDistance = VoiceRuntimeSettings.ProximityMeters;
            float maxDistanceSquared = maxDistance * maxDistance;

            foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer == null || peer.m_uid == senderPeerId)
                {
                    continue;
                }

                if ((peer.GetRefPos() - packet.SpeakerPosition).sqrMagnitude > maxDistanceSquared)
                {
                    continue;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, RpcName, packet.ToPackage());
            }
        }

        private static void BroadcastServerSettings()
        {
            if (ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, SettingsRpcName, VoiceRuntimeSettings.CreateServerPackage());
        }

        private static void OnSettings(long senderPeerId, ZPackage package)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer())
            {
                return;
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer == null || serverPeer.m_uid != senderPeerId)
            {
                return;
            }

            VoiceRuntimeSettings.ApplyServerPackage(package);
        }
    }
}
