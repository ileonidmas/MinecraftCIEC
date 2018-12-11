using System.Data.Entity;

namespace MinecraftCIAC.Models
{
    public class Evolution
    {
        public int ID { get; set; }

        public int BranchID { get; set; } // -1 is default

        public string DirectoryPath { get; set; }

        public string ParentVideoPath { get; set; }

        public string Username { get; set; }

        public string Sequence { get; set; } // 0 - small, 1 - big, 2 - novelty



    }


    public class EvolutionDBContext : DbContext
    {
        public DbSet<Evolution> Evolutions { get; set; }
    }
}