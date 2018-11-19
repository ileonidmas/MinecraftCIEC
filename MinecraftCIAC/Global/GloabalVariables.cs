using RunMission.Evolution;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MinecraftCIAC.Global
{
    public static class GloabalVariables
    {
        private static MalmoClientPool malmoClientPool;
        public static MalmoClientPool MalmoClientPool
        {
            get
            {
                if (malmoClientPool == null)
                {
                    malmoClientPool = new MalmoClientPool(2);
                }
                return malmoClientPool;
            }
        }

        private static NeatEvolutionAlgorithm<NeatGenome> evolutionAlgorithm;
        public static NeatEvolutionAlgorithm<NeatGenome> EvolutionAlgorithm
        {
            get
            {
                return evolutionAlgorithm;
            }

            set
            {
                evolutionAlgorithm = value;
            }
        }
    }
}