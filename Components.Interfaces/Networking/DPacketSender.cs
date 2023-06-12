using Components.Library;

namespace Components.Interfaces.Networking
{
    public abstract class DPacketSender<EP> : ComponentBase<CoreBase>
    {
        public DPacketSender(CoreBase owner) : base(owner) { }

        protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands() => new List<(string opCode, Type opType)>() {
                (nameof(Connect), typeof(void)),
                (nameof(Disconnect), typeof(void)),
                (nameof(Send), typeof(void)),
                (nameof(ReceiveAsync), typeof(void))
            };

        public abstract void Connect(EP ep);
        public abstract void Disconnect(EP ep);
        public abstract void Send(byte[] data);
        public abstract void ReceiveAsync(Action<byte[]> callback);
    }
}