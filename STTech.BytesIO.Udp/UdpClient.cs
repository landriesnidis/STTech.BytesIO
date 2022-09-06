using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Entity;
using System;

namespace STTech.BytesIO.Udp
{

    public class UdpClient : BytesClient, IUdpClient
    {
        public override void Connect()
        {
        }

        public override void Disconnect(DisconnectionReasonCode code = DisconnectionReasonCode.Active, Exception ex = null)
        {
        }

        protected override void ReceiveDataCompletedHandle()
        {
        }

        protected override void ReceiveDataHandle()
        {
        }

        protected override void SendHandler(byte[] data)
        {
        }
    }
}
