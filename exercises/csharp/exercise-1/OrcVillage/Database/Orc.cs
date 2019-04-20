using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrcVillage.Database
{
    [Table("Orcs")]
    public class Orc
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Id")]
        public Guid Id { get; set; }


        [Column("Name")]
        public string Name { get; set; }

        [Column("Profession")]
        public string Profession { get; set; }

        [Column("Born")]
        public DateTime Born { get; set; }
    }
}