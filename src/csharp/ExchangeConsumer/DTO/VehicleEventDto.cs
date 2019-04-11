using System;

namespace ExchangeConsumer.DTO
{
    public class VehicleEventDto
    {
        public string Type { get; set; }
        public Guid VehicleId { get; set; }
        public string Name { get; set; }
        public string RegistrationPlate { get; set; }
    }
}