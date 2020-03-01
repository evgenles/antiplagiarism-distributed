using System.Text.Json;

namespace Agent.Abstract
{
    public static class ObjectExtensions
    {
        public static T ToObject<T>(this object obj) where T : class
        {
            if (typeof(T) != typeof(JsonElement) && obj is JsonElement jElement)
            {
                var json = jElement.GetRawText();
                return JsonSerializer.Deserialize<T>(json);
            }
            return obj as T;
        }
    }
}