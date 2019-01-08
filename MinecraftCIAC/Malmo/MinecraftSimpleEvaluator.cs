using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.IO;
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
            FileUtility.SaveCurrentFitness(username, foldername, getGridFitness(clientInfo));

            // Return the fitness score
            return new FitnessInfo(0, 0);
        }


        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
        }

        private double getGridFitness(bool[] strutureGrid)
        {
            return calculateFitnessWall(strutureGrid);
        }

        //Fitness function for calculating fitness of building a wall
        private double calculateFitnessWall(bool[] fitnessGrid)
        {
            int fitness = 0;

            for (int i = 0; i < fitnessGrid.Length; i++)
            {
                if (fitnessGrid[i] == true)
                {
                    //Console.WriteLine(i);
                    //fitness += 1 + (i / (20 * 20));
                    fitness += 1;

                    // check if something is on top of block i
                    if (i + 400 % (20 * 20 * 20) != 0)
                        if (fitnessGrid[i + 400] == true)
                            fitness += 1;

                    // check if something is on the right side of the block i
                    if ((i + 1) % 20 != 0)
                        if (fitnessGrid[i + 1] == true)
                            fitness += 2;

                    // check if something is on the left side of the block i
                    if (i % 20 != 0)
                        // check if something is on the left
                        if (fitnessGrid[i - 1] == true)
                            fitness += 2;

                    // check if something is on top of block i + 1
                    if ((i + 400 + 1) % 20 != 0)
                        if (fitnessGrid[i + 400 + 1] == true)
                            fitness += 3;

                    // check if something is on top of block i - 1
                    if ((i + 400 - 1) % 20 != 0)
                        if (fitnessGrid[i + 400 - 1] == true)
                            fitness += 3;
                }
            }

            return fitness;
        }
    }
}
