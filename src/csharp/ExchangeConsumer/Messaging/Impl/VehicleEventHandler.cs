using System;
using ExchangeConsumer.DTO;

namespace ExchangeConsumer.Messaging.Impl
{
    public class VehicleEventHandler : IMessageHandler<VehicleEventDto>
    {
        public void Handle(VehicleEventDto message)
        {
            Console.WriteLine("Received vehicle event '{0}' for vehicle '{1}'", message.Type, message.RegistrationPlate);
        }
    }
}