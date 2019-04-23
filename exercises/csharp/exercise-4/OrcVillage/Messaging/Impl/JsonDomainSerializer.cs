using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace OrcVillage.Messaging.Impl
{
    /// <summary>
    /// Serializes directly from domain to byte array and back. Uses intermediate DTOs
    /// </summary>
    public class JsonDomainSerializer : ISerializer
    {
        private readonly IServiceProvider serviceProvider;

        public string ContentType => "application/json";

        public JsonDomainSerializer(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public byte[] Serialize<T>(T obj)
        {
            var converter = serviceProvider.GetRequiredService<IDomainConverter<T>>();
            return ToJsonBytes(converter.FromDomain(obj));
        }

        public T Deserialize<T>(byte[] obj)
        {
            var converter = serviceProvider.GetRequiredService<IDomainConverter<T>>();
            return converter.ToDomain(FromJsonBytes(obj, converter.TargetDtoType));
        }

        private byte[] ToJsonBytes<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        private IDto FromJsonBytes(byte[] obj, Type targetType)
        {
            var json = Encoding.UTF8.GetString(obj);

            return (IDto) JsonConvert.DeserializeObject(json, targetType);
        }
    }
}