namespace STTech.BytesIO.Core
{
    public class SendArgs
    {
        public SendArgs(byte[] data, SendOptions options)
        {
            Data = data;
            Options = options;
        }

        public byte[] Data { get; }
        public SendOptions Options { get; }
    }
}
