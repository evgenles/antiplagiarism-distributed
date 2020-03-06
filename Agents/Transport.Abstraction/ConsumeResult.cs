using System.Collections.Generic;

namespace Transport.Abstraction
{
    public class ConsumeResult<T>
    {
        public T Result { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}