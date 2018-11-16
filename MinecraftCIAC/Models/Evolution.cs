using System.Data.Entity;

namespace MinecraftCIAC.Models
{
    public class Evolution
    {
        public int ID { get; set; }

        public int BranchID { get; set; }

        public string DirectoryPath { get; set; }


    }


    public class EvolutionDBContext : DbContext
    {
        public DbSet<Evolution> Evolutions { get; set; }
    }
}