using System;
using OrcVillage.Messaging.Commands;

namespace OrcVillage.Messaging.Impl
{
    public class CommandDomainConverter : IDomainConverter<CommandBase>
    {
        public class CommandDto : IDto
        {
            public string Type { get; set; }

            public Guid Id { get; set; }

            public string Goal { get; set; }
        }

        public Type TargetDtoType => typeof(CommandDto);

        public CommandBase ToDomain(IDto dto)
        {
            switch (dto)
            {
                case CommandDto e:
                    switch (e.Type)
                    {
                        case "quest":
                            return new Quest
                            {
                                QuestId = e.Id,
                                QuestGoal = e.Goal
                            };
                        case "preparation":
                            return new PreparationTask
                            {
                                TaskId = e.Id,
                                TaskGoal = e.Goal
                            };
                    }

                    break;
            }

            throw new ArgumentException("Unexpected DTO type: " + dto.GetType().FullName);
        }

        public IDto FromDomain(CommandBase domain)
        {
            switch (domain)
            {
                case Quest q:
                    return new CommandDto
                    {
                        Id = q.QuestId,
                        Goal = q.QuestGoal,
                        Type = "quest"
                    };
                case PreparationTask t:
                    return new CommandDto
                    {
                        Id = t.TaskId,
                        Goal = t.TaskGoal,
                        Type = "preparation"
                    };
            }

            throw new NotSupportedException("Unsupported command: " + domain.GetType());
        }
    }
}