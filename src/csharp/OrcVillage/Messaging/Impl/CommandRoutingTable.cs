using System;
using OrcVillage.Messaging.Commands;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging.Impl
{
    public class CommandRoutingTable : IRoutingTable<CommandBase>
    {
        public RoutingInfo GetRoutingInfo(CommandBase request)
        {
            switch (request)
            {
                case Quest _:
                    return new RoutingInfo(MessagingConstants.EXCHANGE_COMMANDS,
                        MessagingConstants.ROUTINGKEY_CHIEFTAIN_QUESTS);
                case PreparationTask _:
                    return new RoutingInfo(MessagingConstants.EXCHANGE_COMMANDS,
                        MessagingConstants.ROUTINGKEY_CHIEFTAIN_PREPARATION);
            }

            throw new NotSupportedException("Unknown command type: " + request.GetType());
        }
    }
}