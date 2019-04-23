using System.Text;
using Newtonsoft.Json;

namespace OrcVillage.Messaging.Impl
{
    public class JsonSerializer : ISerializer
    {
        public string ContentType => "application/json";

        public byte[] Serialize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] obj)
        {
            var json = Encoding.UTF8.GetString(obj);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}