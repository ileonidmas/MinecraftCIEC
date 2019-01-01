using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftCIAC.Malmo
{
    class MinecraftSimpleEvaluator : IPhenomeEvaluator<IBlackBox, NeatGenome>
    {
        private ulong _evalCount;
        private bool _stopConditionSatisfied;
        private MalmoClientPool clientPool;
        public string username { get; set; }


        public MalmoClientPool ClientPool
        {
            get => clientPool;
            set => clientPool = value;
        }

        private List<double> fitnessList = new List<double>(10);
        private int generation = 1;

        /// <summary>
        /// Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount
        {
            get { return _evalCount; }
        }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the the evolutionary algorithm/search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _stopConditionSatisfied; }
        }

        private static object myLock = new object();

        /// <summary>
        /// Evaluate the provided IBlackBox against the random tic-tac-toe player and return its fitness score.
        /// Each network plays 10 games against the random player and two games against the expert player.
        /// Half of the games are played as circle and half are played as x.
        /// 
        /// A win is worth 10 points, a draw is worth 1 point, and a loss is worth 0 points.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox brain, NeatGenome genome)
        {
            int evalCount;
            lock (myLock) {
                evalCount =(int) _evalCount;
                _evalCount++;
            };

            string foldername = evalCount.ToString();
            var isFirstIteration = FileUtility.IsFirstIteration(username, foldername);

            if (!isFirstIteration)
            {
                evalCount++;
                foldername = evalCount.ToString();
                // remove old
                FileUtility.RemoveOldFolder(username, foldername);
            }

            

            bool[] clientInfo = ClientPool.RunAvailableClientWithUserName(brain, username, foldername);

            // save structure

            FileUtility.SaveCurrentStructure(username, foldername, clientInfo);
                       
            // Return the fitness score
            return new FitnessInfo(0, 0);
        }


        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
        }
    }
}
