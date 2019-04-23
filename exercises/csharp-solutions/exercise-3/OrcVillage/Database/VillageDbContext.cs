using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace OrcVillage.Database
{
    public class VillageDbContext : DbContext
    {
        public VillageDbContext(DbContextOptions<VillageDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var doTypes = GetType().Assembly.GetTypes()
                .Where(s => s.GetCustomAttributes(typeof(TableAttribute), false).Any());

            foreach (var doType in doTypes)
            {
                modelBuilder.Entity(doType);
            }


            base.OnModelCreating(modelBuilder);
        }
    }
}