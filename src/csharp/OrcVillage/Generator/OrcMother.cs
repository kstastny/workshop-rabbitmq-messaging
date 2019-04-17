using System;
using OrcVillage.Database;

namespace OrcVillage.Generator
{
    public class OrcMother
    {
        //random google for Orc names came up with this https://en.uesp.net/wiki/Lore:Orc_Names
        private static string[] names =
        {
            "Moghakh", "Atulg", "Azuk", "Bagamul", "Bashag", "Bologra", "Borug", "Lorzub", "Lugrub", "Olumba", "Orakh"
        };

        private readonly Random rnd = new Random();

        private T GetRandomItem<T>(T[] arr)
        {
            return arr[rnd.Next(arr.Length)];
        }

        public Orc GiveBirth()
        {
            return new Orc
            {
                Id = Guid.NewGuid(),
                Born = DateTime.Now,
                Name = GetRandomItem(names),
                //All orcs are warriors
                Profession = "Warrior"
            };
        }
    }
}