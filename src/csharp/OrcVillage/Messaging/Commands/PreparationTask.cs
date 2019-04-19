using System;

namespace OrcVillage.Messaging.Commands
{
    public class PreparationTask : CommandBase
    {
        public Guid TaskId { get; set; }
        
        public string TaskGoal { get; set; }
    }
}