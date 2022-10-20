namespace LoathsomePadSwapper
{

    public class BaskMessage
    {
        public class Hello
        {
            public string MsgType { get => "hello"; }
            public string Name { get => "LoathsomePadSwapper"; }
        }

        public class Subscribe
        {
            public string MsgType { get => "subscribe"; }
            public string Topic { get => "nextPlayer"; }
        }
        public class Event
        {
            public string MsgType { get; init; }

            public Event(string msgType)
            {
                MsgType = msgType;
            }
        }

    }
}
