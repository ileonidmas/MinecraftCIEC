using RunMission.Evolution.RunMission.Evolution;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RunMission.Evolution
{
    class MinecraftBuilderExperiment : SimpleNeatExperiment
    {
        public MalmoClientPool malmoClientPool;
        private string evaluatorType;

        public MinecraftBuilderExperiment(MalmoClientPool clientPool, string evaluator)
        {
            malmoClientPool = clientPool;
            evaluatorType = evaluator;
        }

        /// <summary>
        /// Gets the MinecraftBuilder evaluator that scores individuals.
        /// </summary>
        public override IPhenomeEvaluator<IBlackBox> PhenomeEvaluator
        {
            get {
                if (evaluatorType == "Fitness")
                {
                    MinecraftFitnessEvaluator evaluator = new MinecraftFitnessEvaluator();
                    evaluator.ClientPool = malmoClientPool;
                    return evaluator;
                }

                if (evaluatorType == "Novelty")
                {
                    MinecraftNoveltyEvaluator evaluator = new MinecraftNoveltyEvaluator();
                    evaluator.createFolders();
                    evaluator.ClientPool = malmoClientPool;
                    return evaluator;
                }

                return new MinecraftFitnessEvaluator();
            }
        }
        /// <summary>
        /// Defines the number of input nodes in the neural network.
        /// The network has one input for each block of the observation
        /// </summary>
        public override int InputCount
        {
            get { return 16; }
        }

        /// <summary>
        /// Defines the number of output nodes in the neural network.
        /// Direction and what type of action
        /// </summary>
        public override int OutputCount
        {
            get { return 16; }
        }

        /// <summary>
        /// Defines whether all networks should be evaluated every
        /// generation, or only new (child) networks. For Tic-Tac-Toe,
        /// we're evolving against a random player, so it's a good
        /// idea to evaluate individuals again every generation,
        /// to make sure it wasn't just luck.
        /// </summary>
        public override bool EvaluateParents
        {
            get { return false; }
        }
    }
}
