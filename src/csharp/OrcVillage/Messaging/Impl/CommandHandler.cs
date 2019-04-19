using System;
using OrcVillage.Messaging.Commands;

namespace OrcVillage.Messaging.Impl
{
    public class CommandHandler : IMessageHandler<CommandBase>
    {
        public void Handle(CommandBase message)
        {
            Console.WriteLine("COMMAND RECEIVED " + message.GetType().FullName);
        }
    }
}