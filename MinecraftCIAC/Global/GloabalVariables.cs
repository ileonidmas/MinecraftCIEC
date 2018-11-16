using RunMission.Evolution;
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
                    malmoClientPool = new MalmoClientPool(3);
                }
                return malmoClientPool;
            }
        }

    }
}