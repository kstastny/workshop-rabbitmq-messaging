using System;

namespace OrcVillage.Messaging.Commands
{
    public class Quest : CommandBase
    {
        public Guid QuestId { get; set; }
        
        public string QuestGoal { get; set; }

        public override string ToString()
        {
            return $"Quest [{QuestId}]: {QuestGoal}";
        }
    }
}