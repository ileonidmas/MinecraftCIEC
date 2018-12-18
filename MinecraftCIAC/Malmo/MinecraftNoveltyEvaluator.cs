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
        private readonly int NOVELTY_THRESHOLD = 1;
        private readonly int NOVELTY_KNEARSEST = 3;
        private readonly int POPULATION_SIZE = 5;
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

            if (novelBehaviourArchive.Count != 0)
            {
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
            } else
            {
                // Novelty archive is empty, add first one by returning a value higher than the threshold
                return NOVELTY_THRESHOLD + 1;
            }
                
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
        /// Saves the novel structure in the novel archive folder associated to this novelty evolution
        /// </summary>
        /// <param name="structureGrid">The minecraft structure</param>
        private void saveNovelStructure(bool[] structureGrid, int index)
        {
            //Path to the file to save the novel structure in
            String novelFilePath = Path.Combine(noveltyArchivePath, index.ToString());

            //Create the file and save all values to the file
            using (StreamWriter sw = new StreamWriter(novelFilePath))
            {
                // Run through the structure grid and save all values to the file
                for (int j = 0; j < structureGrid.Length; j++)
                {
                    sw.WriteLine(structureGrid[j]);
                }
            }
        }

        /// <summary>
        /// Returns the score for a game. Scoring is 10 for a win, 1 for a draw
        /// and 0 for a loss. Note that scores cannot be smaller than 0 because
        /// NEAT requires the fitness score to be positive.
        /// </summary>
        private int getScore()
        {
            return 0;
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

        //Method for getting the 20x20x20 grid of the confined area according to agent position, from the 41x41x41 grid
        //private bool[] getStructureGrid(bool[] fitnessGrid, AgentPosition agentPosition, int gridWLH)
        //{
        //    //The area in which the agent may place blocks are at most 20x20x20
        //    bool[] flattenedFitnessGrid = new bool[20 * 20 * 20];

        //    //The agent starts at Y position 227. Find where the layers within the area starts and ends, that are also above ground level
        //    int layerStartY = (int)(Math.Abs(agentPosition.initialY - (agentPosition.currentY - ((gridWLH - 1) / 2))));

        //    //If agent gets out of bound 20 blocks below ground level or 20 blocks above ground level, return a grid that would result in a fitness of 0
        //    if (agentPosition.currentY < 227 - (gridWLH / 2) || agentPosition.currentY > 227 + (gridWLH / 2))
        //    {
        //        for (int i = 0; i < flattenedFitnessGrid.Length; i++)
        //        {
        //            flattenedFitnessGrid[i] = false;
        //        }

        //        return flattenedFitnessGrid;
        //    }

        //    //The agent starts at X position 0. Find where the layers within the area starts
        //    int layerStartX = (int)(Math.Abs(agentPosition.currentX - 20)) + 1;

        //    //The agent starts at Z position 0. Find where the layers within the area starts
        //    int layerStartZ = (int)(Math.Abs(agentPosition.currentZ - 20)) + 1;

        //    // Start at the layer above ground level and run through 20 layers from that layer in the flattened fitness grid
        //    int currentBlockInGrid = (layerStartY * (gridWLH * gridWLH)) + (layerStartZ * gridWLH) + layerStartX;

        //    int flattenedGridCounter = 0;

        //    for (int y = 0; y < 20; y++)
        //    {
        //        //move one layer up in the y position
        //        currentBlockInGrid = ((layerStartY + y) * (gridWLH * gridWLH)) + (layerStartZ * gridWLH) + layerStartX;

        //        for (int z = 0; z < 20; z++)
        //        {
        //            for (int x = 0; x < 20; x++)
        //            {
        //                if (fitnessGrid[currentBlockInGrid].ToString() == "gold_ore")
        //                {
        //                    flattenedFitnessGrid[flattenedGridCounter] = true;
        //                }
        //                else
        //                {
        //                    flattenedFitnessGrid[flattenedGridCounter] = false;
        //                }

        //                currentBlockInGrid += 1;
        //                flattenedGridCounter++;
        //            }

        //            //move to next row in the z position
        //            currentBlockInGrid += gridWLH - 20;
        //        }
        //    }

        //    return flattenedFitnessGrid;
        //}

        #endregion
    }
}
