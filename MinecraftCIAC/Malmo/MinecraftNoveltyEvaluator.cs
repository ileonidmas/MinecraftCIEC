﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

    using Newtonsoft.Json.Linq;
    using SharpNeat.Core;
    using SharpNeat.Genomes.Neat;
    using SharpNeat.Phenomes;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

namespace MinecraftCIAC.Malmo
#region IPhenomeEvaluator<IBlackBox> Members
.Evolution
{
    public class MinecraftNoveltyEvaluator : IPhenomeEvaluator<IBlackBox, NeatGenome>
    {
        private readonly double NOVELTY_THRESHOLD = 1.5;
        private readonly int NOVELTY_KNEARSEST = 3;
        private readonly int POPULATION_SIZE = 4;
        private ulong _evalCount;
        private bool _stopConditionSatisfied;
        private MalmoClientPool clientPool;
        private int populationSize = 0;
        public string username { get; set; }

        private string noveltyArchivePath;
        private List<bool[]> novelBehaviourArchive = new List<bool[]>();
        private List<bool[]> newNovelBehaviourArchive = new List<bool[]>();
        private List<bool[]> currentGenerationArchive = new List<bool[]>();
        private Dictionary<ulong, int> distanceDictionary = new Dictionary<ulong, int>();
        private int distanceCount = 0;
        private int generation = 1;
        private int counter = 0;

        public MalmoClientPool ClientPool
        {
            get => clientPool;
            set => clientPool = value;
        }

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

        public static object myLock = new object();
        public static object myLock2 = new object();
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
            string foldername;
            lock (myLock)
            {
                evalCount = (int)_evalCount + 100;
                foldername = evalCount.ToString();
                _evalCount++;
                populationSize++;
            };

            // Evaluate and get structure. Add to current generation archive afterwards
            bool[] structureGrid = ClientPool.RunAvailableClientWithUserName(brain, username, foldername);
            currentGenerationArchive.Add(structureGrid);

            // Wait until all in the current generation has been evaluated
            while (currentGenerationArchive.Count != populationSize)
            {
                Thread.Sleep(1000);
            }

            // Get distance to K nearest neighbours. Lock ensures that two of the same structure
            // doesn't get added to the novel behaviour archive, due to race condition
            var noveltyDistance = 0.0;
            lock (myLock2)
            {

                if (counter < POPULATION_SIZE)
                {
                    noveltyDistance = getDistance(structureGrid);

                    if (noveltyDistance > NOVELTY_THRESHOLD)
                    {
                        counter++;
                        novelBehaviourArchive.Add(structureGrid);
                        newNovelBehaviourArchive.Add(structureGrid);
                        if (newNovelBehaviourArchive.Count == POPULATION_SIZE)
                            _stopConditionSatisfied = true;

                        FileUtility.SaveCurrentGenome(username, foldername, genome);
                        FileUtility.SaveCurrentStructure(username, foldername, structureGrid);
                        FileUtility.SaveCurrentFitness(username, foldername, getGridFitness(structureGrid));
                        FileUtility.SaveNovelStructure(username, foldername);
                        FileUtility.CopyCanditateToProperFolder(username, foldername, counter.ToString());
                        Console.WriteLine(noveltyDistance);
                    }
                }
                else
                {
                    _stopConditionSatisfied = true;
                }
                distanceCount++;
            }

            while (distanceCount != populationSize)
            {
                Thread.Sleep(1000);
            }
            
            Thread.Sleep(1500);
            distanceCount = 0;
            populationSize = 0;
            currentGenerationArchive.Clear();

            // run novelty until archive is full, then stop = true

            // Return the fitness score
            return new FitnessInfo(noveltyDistance, noveltyDistance);
        }

        /// <summary>
        /// Method for comparing a structure with both the current generation and the novelty archive
        /// </summary>
        /// <param name="structureGrid">The minecraft structure</param>
        /// <returns>Average novelty distance to k nearest neighbours</returns>
        private double getDistance(bool[] structureGrid)
        {
            //currentGenerationArchive.AddRange(novelBehaviourArchive);

            var knearest = 0;

            List<int> novelDistances = new List<int>();

            //Compare the individual with each of the other individuals, in both novel archive and current generation
            var distance = 0;
            for (int i = 0; i < novelBehaviourArchive.Count; i++)
            {
                //Compare each structure block by block
                for (int j = 0; j < 20 * 20 * 20; j++)
                {
                    if (structureGrid[j] != novelBehaviourArchive[i][j])
                        distance++;
                }

                novelDistances.Add(distance);
                distance = 0;
            }

            //Sort in ascending order
            novelDistances.Sort((a, b) => a.CompareTo(b));
            if (novelDistances.Count < NOVELTY_KNEARSEST)
                knearest = novelDistances.Count;
            else
                knearest = NOVELTY_KNEARSEST;

            //Find the summed up k-nearest novel distances and return the average of the sum
            double avgNovelty = 0;
            for (int i = 0; i < knearest; i++)
            {
                avgNovelty += novelDistances[i];
            }

            return avgNovelty / knearest;

        }

        /// <summary>
        /// Creates the folders for saving the novelty archive
        /// </summary>
        public void createFolders()
        {
            //Creates a folder for novelty archives if it doesn't exist
            string noveltyArchivesPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\")) + "noveltyResults";
            Directory.CreateDirectory(noveltyArchivesPath);

            //Creates a folder for one archive with a random archive name
            noveltyArchivePath = Path.Combine(noveltyArchivesPath, Path.GetRandomFileName());
            Directory.CreateDirectory(noveltyArchivePath);
        }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// Note. The TicTacToe problem domain has no internal state. This method does nothing.
        /// </summary>
        public void Reset()
        {
        }

        public void LoadStructures(string username)
        {

            List<bool[]> structures = FileUtility.LoadStructures(username);
            foreach (var structure in structures)
            {
                novelBehaviourArchive.Add(structure);
            }

        }
        #endregion

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
