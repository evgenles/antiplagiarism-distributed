using System;

namespace Transport.Abstraction
{
    public interface ITransportConsumer
    {
        void Subscribe(string id, params string[] queueTopic);

        ConsumeResult<string> Consume();
        ConsumeResult<string> Consume(TimeSpan timeout);
        ConsumeResult<T> Consume<T>();
        ConsumeResult<T> Consume<T>(TimeSpan timeout);
        void Commit();
    }
}