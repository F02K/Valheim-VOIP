namespace ValheimVoip
{
    internal sealed class VoiceClient
    {
        public void Send(VoicePacket packet)
        {
            if (ZRoutedRpc.instance == null || ZNet.instance == null)
            {
                return;
            }

            if (ZNet.instance.IsServer())
            {
                VoiceServer relay = VoiceServer.Instance;
                if (relay != null)
                {
                    relay.Relay(ZNet.GetUID(), packet);
                }

                return;
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(serverPeer.m_uid, VoiceNetwork.VoiceFrameRpcName, packet.ToPackage());
        }

        public void ApplyServerSettings(long senderPeerId, ZPackage package)
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

        public void OnRpcUnavailable()
        {
            VoiceRuntimeSettings.ClearServerSettings();
        }
    }
}
