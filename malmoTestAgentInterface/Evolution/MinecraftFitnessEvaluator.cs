using Newtonsoft.Json.Linq;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RunMission
        #region IPhenomeEvaluator<IBlackBox> Members
.Evolution
{
    public class MinecraftFitnessEvaluator: IPhenomeEvaluator<IBlackBox>
    {
        private ulong _evalCount;
        private bool _stopConditionSatisfied;
        private MalmoClientPool clientPool;
        public MalmoClientPool ClientPool {
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

        /// <summary>
        /// Evaluate the provided IBlackBox against the random tic-tac-toe player and return its fitness score.
        /// Each network plays 10 games against the random player and two games against the expert player.
        /// Half of the games are played as circle and half are played as x.
        /// 
        /// A win is worth 10 points, a draw is worth 1 point, and a loss is worth 0 points.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox brain)
        {
            bool[] clientInfo = ClientPool.RunAvailableClient(brain);

            double fitness = calculateFitnessWall(clientInfo);

            // Update the fitness score of the network
            //fitness += 1;

            // Update the evaluation counter.
            _evalCount++;

            Console.WriteLine("EvalCount: " + _evalCount);
            Console.WriteLine("Fitness: " + fitness);

            // If the networks reaches a fitness of 30, stop evaluation
            if (fitness >= 30)
                _stopConditionSatisfied = true;

            //writeToFile(fitness);

            // Return the fitness score
            return new FitnessInfo(fitness, fitness);
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

        private void writeToFile(int fitness)
        {
            // add fitness to the list before new evaluation
            if (fitnessList.Count < 10)
                fitnessList.Add(fitness);

            // if whole population got evaluated, write generation and max fitness to result file
            String path = "";
            if (System.Environment.UserName == "lema")
                path = @"C:\Users\lema\Documents\GitHub\malmoTestAgentInterface\Evolution\Results\results.csv";
            else
                path = @"C:\Users\Pierre\Documents\malmoTestAgentInterface\Evolution\Results\results.csv";

            if (fitnessList.Count == 10)
            {
                var maxFitness = fitnessList.Max();

                using (StreamWriter outputFile = new StreamWriter(path, true))
                {
                    if (generation == 1)
                    {
                        outputFile.WriteLine("Generation, Fitness");
                    }

                    outputFile.WriteLine(String.Format("{0}, {1}", generation, maxFitness));
                }

                //Console.WriteLine("Maximum fitness = " + maxFitness + " in population number " + populationNumber);
                fitnessList.Clear();
                generation++;
            }
        }

        #region fitnessFuncs


        //Fitness function for calculating fitness of building a wall (old version)
        private double calculateFitnessWall(bool[] fitnessGrid)
        {
            int fitness = 0;

            for(int i = 0; i < fitnessGrid.Length; i++)
            {
                if(fitnessGrid[i] == true)
                {
                    Console.WriteLine(i);
                    fitness += 1 + (i / (20 * 20));

                    // check if something is right only if its not right side
                    if ( (i + 1) % 20 != 0)
                        // check if something is on the right
                        if (fitnessGrid[i + 1] == true)
                            fitness++;

                    // check if something is left only if its not left side
                    if ( i % 20 != 0)
                        // check if something is on the left
                        if (fitnessGrid[i - 1] == true)
                            fitness++;

                    //// check if something is front only if its not front side
                    //if (i % 400 >= 380)
                    //    // check if something is front
                    //    if (fitnessGrid[i + 20] == true)
                    //    fitness++;
                    
                    //// check if something is back only if its not back side
                    //if (i % 400 <= 19)
                    //    // check if something is back
                    //    if (fitnessGrid[i - 20] == true)
                    //        fitness++;

                    // check if something is top and its not top side
                    if(i + 400 <= 20 * 20 * 20)
                        // check if something is top
                        if (fitnessGrid[i + 400] == true)
                            fitness++;

                    // check if something is top and its not top side
                    if (i - 400 >= 0)
                        // check if something is top
                        if (fitnessGrid[i - 400] == true)
                            fitness++;
                }
            }

            return fitness;

            ////check if there is a block to the right of current block
            //if (i - 1 >= 0)
            //{
            //    if (fitnessgrid[i - 1].tostring() == "gold_ore" && blockonindex)
            //    {
            //        fitness += 1.0;
            //    }
            //}

            ////check if there is a block to the left of current block
            //if (i + 1 <= fitnessgrid.count() - 1)
            //{
            //    if (fitnessgrid[i + 1].tostring() == "gold_ore" && blockonindex)
            //    {
            //        fitness += 1.0;
            //    }
            //}

            ////check if there is a block below current block
            //if (i - (gridwlh * gridwlh) >= 0)
            //{
            //    if (fitnessgrid[i - (gridwlh * gridwlh)].tostring() == "gold_ore" && blockonindex)
            //    {
            //        fitness += 1.0;
            //    }
            //}

            ////check if there is a block on top of current block
            //if (i + (gridwlh * gridwlh) <= fitnessgrid.count() - 1)
            //{
            //    if (fitnessgrid[i + (gridwlh * gridwlh)].tostring() == "gold_ore" && blockonindex)
            //    {
            //        fitness += 1.0;
            //    }
            //}

            ////increase current layer count and reset current block in layer position
            ////if last block in layer has been checked
            //if (currentblockinlayer == blocksperlayer - 1)
            //{
            //    currentlayer++;
            //    currentblockinlayer = 0;
            //}
            //else
            //{
            //    currentblockinlayer++;
            //}

            //blockonindex = false;

            //return fitness;
        }

        //Fitness function for calculating fitness of connected structures
        //private double calculateFitnessConStruct(JToken fitnessGrid, AgentPosition agentPosition)
        //{

        //    bool blockOnIndex = false;

        //    double fitness = 0.0;
        //    int gridWLH = 41;

        //    //The agents current Y position
        //    double agentYPos = agentPosition.currentY;

        //    //The agent starts at Y position 227.
        //    int layersBelowGroundLevel = (int)(227 - (agentYPos - ((gridWLH - 1) / 2)));

        //    //Disregard blocks below ground level by turning them to air.
        //    int disregardBlocks = 0;
        //    if (layersBelowGroundLevel > 0)
        //    {
        //        if (layersBelowGroundLevel > gridWLH)
        //            layersBelowGroundLevel = gridWLH;
        //        disregardBlocks = (layersBelowGroundLevel * (gridWLH * gridWLH));
        //        for (int i = 0; i < disregardBlocks; i++)
        //        {
        //            fitnessGrid[i].Replace("air");
        //        }
        //    }

        //    //Disregard blocks below groundlevel
        //    for (int i = disregardBlocks; i < fitnessGrid.Count(); i++)
        //    {
        //        //Check if current block is a gold ore and increase fitness if so
        //        if (fitnessGrid[i].ToString() == "gold_ore")
        //        {
        //            fitness += 1.0;

        //            blockOnIndex = true;
        //        }

        //        //Check if there is a block to the right of current block
        //        if (i - 1 >= 0)
        //        {
        //            if (fitnessGrid[i - 1].ToString() == "gold_ore" && blockOnIndex)
        //            {
        //                fitness += 1.0;
        //            }
        //        }

        //        //Check if there is a block to the left of current block
        //        if (i + 1 <= fitnessGrid.Count() - 1)
        //        {
        //            if (fitnessGrid[i + 1].ToString() == "gold_ore" && blockOnIndex)
        //            {
        //                fitness += 1.0;
        //            }
        //        }

        //        //Check if there is a block in back of current block
        //        if (i - gridWLH >= 0)
        //        {
        //            if (fitnessGrid[i - gridWLH].ToString() == "gold_ore" && blockOnIndex)
        //            {
        //                fitness += 1.0;
        //            }
        //        }

        //        //Check if there is a block in front of current block
        //        if (i + gridWLH <= fitnessGrid.Count() - 1)
        //        {
        //            if (fitnessGrid[i + gridWLH].ToString() == "gold_ore" && blockOnIndex)
        //            {
        //                fitness += 1.0;
        //            }
        //        }

        //        //Check if there is a block below current block
        //        if (i - (gridWLH * gridWLH) >= 0)
        //        {
        //            if (fitnessGrid[i - (gridWLH * gridWLH)].ToString() == "gold_ore" && blockOnIndex)
        //            {
        //                fitness += 2.0;
        //            }
        //        }

        //        //Check if there is a block on top of current block
        //        if (i + (gridWLH * gridWLH) <= fitnessGrid.Count() - 1)
        //        {
        //            if (fitnessGrid[i + (gridWLH * gridWLH)].ToString() == "gold_ore" && blockOnIndex)
        //            {
        //                fitness += 2.0;
        //            }
        //        }

        //        blockOnIndex = false;
        //    }

        //    return fitness;
        //}
        #endregion

        //Method for getting the 20x20x20 grid of the confined area according to agent position, from the 41x41x41 grid
        private bool[] getStructureGrid(JToken fitnessGrid, AgentPosition agentPosition, int gridWLH)
        {
            //The area in which the agent may place blocks are at most 20x20x20
            bool[] flattenedFitnessGrid = new bool[20 * 20 * 20];

            //The agent starts at Y position 227. Find where the layers within the area starts and ends, that are also above ground level
            int layerStartY = (int)(Math.Abs(agentPosition.initialY - (agentPosition.currentY - ((gridWLH - 1) / 2))));

            //If agent gets out of bound 20 blocks below ground level or 20 blocks above ground level, return a grid that would result in a fitness of 0
            if (agentPosition.currentY < 227 - (gridWLH / 2) || agentPosition.currentY > 227 + (gridWLH / 2))
            {
                for (int i = 0; i < flattenedFitnessGrid.Length; i++)
                {
                    flattenedFitnessGrid[i] = false;
                }

                return flattenedFitnessGrid;
            }

            //The agent starts at X position 0. Find where the layers within the area starts
            int layerStartX = (int)(Math.Abs(agentPosition.currentX - 20)) + 1;

            //The agent starts at Z position 0. Find where the layers within the area starts
            int layerStartZ = (int)(Math.Abs(agentPosition.currentZ - 20)) + 1;

            // Start at the layer above ground level and run through 20 layers from that layer in the flattened fitness grid
            int currentBlockInGrid = (layerStartY * (gridWLH * gridWLH)) + (layerStartZ * gridWLH) + layerStartX;

            int flattenedGridCounter = 0;

            for (int y = 0; y < 20; y++)
            {
                //move one layer up in the y position
                currentBlockInGrid = ((layerStartY + y) * (gridWLH * gridWLH)) + (layerStartZ * gridWLH) + layerStartX;

                for (int z = 0; z < 20; z++)
                {
                    for (int x = 0; x < 20; x++)
                    {
                        if (fitnessGrid[currentBlockInGrid].ToString() == "gold_ore")
                        {
                            flattenedFitnessGrid[flattenedGridCounter] = true;
                        }
                        else
                        {
                            flattenedFitnessGrid[flattenedGridCounter] = false;
                        }

                        currentBlockInGrid += 1;
                        flattenedGridCounter++;
                    }

                    //move to next row in the z position
                    currentBlockInGrid += gridWLH - 20;
                }
            }

            return flattenedFitnessGrid;
        }

        #endregion
    }
}
