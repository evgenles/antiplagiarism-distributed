using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agent.Abstract.Models;

namespace AgentLoader
{
    public class TypedAgentConverter : JsonConverter<AgentMessage>
    {
        public override AgentMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, AgentMessage value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}