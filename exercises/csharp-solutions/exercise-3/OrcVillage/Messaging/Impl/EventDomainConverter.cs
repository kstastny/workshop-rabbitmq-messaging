using System;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging.Impl
{
    public class EventDomainConverter : IDomainConverter<EventBase>
    {
        public class EventDto : IDto
        {
            public string Type { get; set; }

            public string EventType { get; set; }

            public Guid OrcId { get; set; }

            public string Name { get; set; }

            public string Profession { get; set; }
        }

        public Type TargetDtoType => typeof(EventDto);

        public EventBase ToDomain(IDto dto)
        {
            switch (dto)
            {
                case EventDto e:
                    switch (e.Type)
                    {
                        case MessagingConstants.EVENT_TYPE_ORCEVENT:
                            return new OrcEvent
                            {
                                Name = e.Name,
                                EventType = e.EventType,
                                Profession = e.Profession,
                                OrcId = e.OrcId
                            };
                    }

                    break;
            }

            throw new ArgumentException("Unexpected DTO type: " + dto.GetType().FullName);
        }

        public IDto FromDomain(EventBase domain)
        {
            switch (domain)
            {
                case OrcEvent e:
                    return new EventDto
                    {
                        Type = MessagingConstants.EVENT_TYPE_ORCEVENT,
                        EventType = e.EventType,
                        Name = e.Name,
                        Profession = e.Profession,
                        OrcId = e.OrcId
                    };
            }

            throw new NotSupportedException("Unsupported event: " + domain.GetType());
        }
    }
}