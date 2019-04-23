using System;
using OrcVillage.Database;
using OrcVillage.Messaging.Commands;

namespace OrcVillage.Generator
{
    public class OrcChieftain
    {
        private static readonly string[] Quests =
        {
            "Pillage a village",
            "Raze a town",
            "Sack a city",
            "Look for gold",
            "Find legendary axe",
            "Harass goblins",
            "Destroy paladins",
            "Kill! Kill! Kill!",
        };

        private static readonly string[] Preparations =
        {
            "Forge weapons",
            "Build catapults",
            "Make armor",
            "Chop trees",
            "Build ships",
            "Take a nap",
            "Rest a bit",
        };

        private readonly Random rnd = new Random();

        private T GetRandomItem<T>(T[] arr)
        {
            return arr[rnd.Next(arr.Length)];
        }

        public Quest Quest()
        {
            return new Quest
            {
                QuestId = Guid.NewGuid(),
                QuestGoal = GetRandomItem(Quests)
            };
        }

        public PreparationTask Preparation()
        {
            return new PreparationTask
            {
                TaskId = Guid.NewGuid(),
                TaskGoal = GetRandomItem(Preparations)
            };
        }
    }
}