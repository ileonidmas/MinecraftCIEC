using MinecraftCIAC.Malmo;
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
        #region 

        public static readonly int NUMBER_OF_AGENTS = 5;
        public static readonly int POPULATION_SIZE = 5;

        #endregion


        private static MalmoClientPool malmoClientPool;
        public static MalmoClientPool MalmoClientPool
        {
            get
            {
                if (malmoClientPool == null)
                {
                    malmoClientPool = new MalmoClientPool(NUMBER_OF_AGENTS);
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