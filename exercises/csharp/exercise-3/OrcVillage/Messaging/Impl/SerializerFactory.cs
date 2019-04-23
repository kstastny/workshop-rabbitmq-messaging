using System;
using Microsoft.Extensions.DependencyInjection;

namespace OrcVillage.Messaging.Impl
{
    public class SerializerFactory : ISerializerFactory
    {
        private readonly IServiceProvider serviceProvider;

        public SerializerFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        
        public ISerializer CreateSerializer(string contentType)
        {
            //NOTE: here we would decide what serializer to create based on contentType.
            //could be JSON, XML, ProtoBuf, MsgPack,...
            return serviceProvider.GetRequiredService<ISerializer>();
        }
    }
}