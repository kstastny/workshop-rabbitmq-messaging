using System;
using OrcVillage.Messaging.Commands;

namespace OrcVillage.Messaging.Impl
{
    public class CommandHandler : IMessageHandler<CommandBase>
    {
        private readonly Random rnd = new Random();

        private readonly AppConfiguration appConfiguration;

        public CommandHandler(AppConfiguration appConfiguration)
        {
            this.appConfiguration = appConfiguration;
        }

        public void Handle(CommandBase message)
        {
            Console.WriteLine("Received new command: " + message);
            switch (message)
            {
                case PreparationTask t:
                    if (rnd.NextDouble() < appConfiguration.PreparationFailureRate)
                        throw new TransientFailure($"[{t.TaskId}] {t.TaskGoal} - failed!");
                    Console.WriteLine($"[{t.TaskId}] {t.TaskGoal} - success!");
                    //TODO exercise: send information about success
                    break;
                case Quest q:
                    if (rnd.NextDouble() < appConfiguration.QuestFailureRate)
                        throw new TransientFailure($"[{q.QuestId}] {q.QuestGoal} - failed!");
                    Console.WriteLine($"[{q.QuestId}] {q.QuestGoal} - success!");
                    //TODO exercise: send information about success
                    break;
                default:
                    throw new Exception("Unknown command: " + message);
            }
        }
    }
}