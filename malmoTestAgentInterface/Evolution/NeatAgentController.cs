using Microsoft.Research.Malmo;
using RunMission.Evolution.Enums;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RunMission.Evolution
{
    public class NeatAgentController
    {
        /// <summary>
        /// The neural network that this player uses to make its decision.
        /// </summary>
        public IBlackBox Brain { get; set; }

        private bool agentNotStuck = true;
        public bool AgentNotStuck
        {
            get => agentNotStuck;
            set => agentNotStuck = value;
        }
        private AgentHelper agentHelper;
        public AgentHelper AgentHelper
        {
            get => agentHelper;
            set => agentHelper = value;
        }

        /// <summary>
        /// Creates a new NEAT player with the specified brain.
        /// </summary>
        public NeatAgentController(IBlackBox brain, AgentHost agentHost)
        {
            Brain = brain;
            agentHelper = new AgentHelper(agentHost);
        }

        bool runOnce = true;
        public void PerformAction()
        {
            // Clear the network
            Brain.ResetState();

            // Get observations
            var observations = agentHelper.CheckSurroundings();
            var agentPosition = agentHelper.AgentPosition;

            // Convert the world observations into an input array for the network
            setInputSignalArray(Brain.InputSignalArray, observations, agentPosition);
            // Activate the network
            Brain.Activate();
            // Convert the action and perform the command
            //outputToCommands();

            //if (runOnce)
            //{
            //    agentHelper.PlaceBlockAbsolute(Direction.Front);
            //    agentHelper.setGridPosition(Direction.Front, true);

            //    agentHelper.PlaceBlockAbsolute(Direction.FrontTop);
            //    agentHelper.setGridPosition(Direction.FrontTop, true);

            //    agentHelper.Teleport(Direction.Right);
            //    var worldState = AgentHelper.AgentHost.getWorldState();
            //    AgentHelper.ConstantObservations = worldState.observations;

            //    agentHelper.Teleport(Direction.Front);
            //    worldState = AgentHelper.AgentHost.getWorldState();
            //    AgentHelper.ConstantObservations = worldState.observations;

            //    agentHelper.PlaceBlockAbsolute(Direction.Under);
            //    agentHelper.setGridPosition(Direction.Under, true);

            //    worldState = AgentHelper.AgentHost.getWorldState();
            //    AgentHelper.ConstantObservations = worldState.observations;

            //    agentHelper.PlaceBlockAbsolute(Direction.LeftTop);
            //    agentHelper.setGridPosition(Direction.LeftTop, true);

            //    runOnce = false;
            //}

            agentNotStuck = outputToCommandsAbs();
        }

        // Method for passing observations as inputs for the ANN
        private void setInputSignalArray(ISignalArray inputArr, string[] board, AgentPosition agentPosition)
        {
            inputArr[0] = blockToInt(board[0]);
            inputArr[1] = blockToInt(board[1]);
            inputArr[2] = blockToInt(board[2]);
            inputArr[3] = blockToInt(board[3]);
            inputArr[4] = blockToInt(board[4]);
            inputArr[5] = blockToInt(board[5]);
            inputArr[6] = blockToInt(board[6]);
            inputArr[7] = blockToInt(board[7]);
            inputArr[8] = blockToInt(board[8]);
            inputArr[9] = blockToInt(board[9]);
            inputArr[10] = blockToInt(board[10]);
            inputArr[11] = blockToInt(board[11]);
            inputArr[12] = blockToInt(board[12]);

            inputArr[13] = agentPosition.currentX - agentPosition.initialX; // Difference of current x position and initial x position
            inputArr[14] = agentPosition.currentY - agentPosition.initialY; // Difference of current y position and initial y position
            inputArr[15] = agentPosition.currentZ - agentPosition.initialZ; // Difference of current z position and initial z position
        }

        private int blockToInt(string block)
        {
            if (block == "air")
                return 0;
            return 1;

        }

        // Method for passing outputs of the neural network to the client
        //***************************************************** FIRST CONTROLLER ***********************************************
        private void outputToCommands()
        {
            bool actionIsPerformed = false;

            double move = Brain.OutputSignalArray[0];
            double placeBlock = Brain.OutputSignalArray[1];
            double destroyBlock = Brain.OutputSignalArray[2];

            Direction direction = Direction.Under;
            var highestDirection = 15;
            var highestValue = 0d;
            // find direction
            for (int i = 3; i < 16; i++)
            {
                if (Brain.OutputSignalArray[i] > highestValue)
                {
                    highestValue = Brain.OutputSignalArray[i];
                    highestDirection = i;
                }
            }
            switch (highestDirection)
            {
                case 3:
                    direction = Direction.LeftUnder;
                    break;
                case 4:
                    direction = Direction.FrontUnder;
                    break;
                case 5:
                    direction = Direction.RightUnder;
                    break;
                case 6:
                    direction = Direction.BackUnder;
                    break;
                case 7:
                    direction = Direction.Left;
                    break;
                case 8:
                    direction = Direction.Front;
                    break;
                case 9:
                    direction = Direction.Right;
                    break;
                case 10:
                    direction = Direction.Back;
                    break;
                case 11:
                    direction = Direction.LeftTop;
                    break;
                case 12:
                    direction = Direction.FrontTop;
                    break;
                case 13:
                    direction = Direction.RightTop;
                    break;
                case 14:
                    direction = Direction.BackTop;
                    break;
                case 15:
                    direction = Direction.Under;
                    break;
            }

            /*
            if(directionRaw < (1d / 13d))
            {
                direction = Direction.Under;
            } else if (directionRaw >= (1d / 13d) && directionRaw < (2d / 13d))
            {
                direction = Direction.BackUnder;
            } else if (directionRaw >= (2d / 13d) && directionRaw < (3d / 13d))
            {
                direction = Direction.LeftUnder;
            } else if (directionRaw >= (3d / 13d) && directionRaw < (4d / 13d))
            {
                direction = Direction.FrontUnder;
            } else if (directionRaw >= (4d / 13d) && directionRaw < (5d / 13d))
            {
                direction = Direction.RightUnder;
            } else if (directionRaw >= (5d / 13d) && directionRaw < (6d / 13d))
            {
                direction = Direction.Back;
            } else if (directionRaw >= (6d / 13d) && directionRaw < (7d / 13d))
            {
                direction = Direction.Left;
            } else if (directionRaw >= (7d / 13d) && directionRaw < (8d / 13d))
            {
                direction = Direction.Front;
            } else if (directionRaw >= (8d / 13d) && directionRaw < (9d / 13d))
            {
                direction = Direction.Right;
            } else if (directionRaw >= (9d / 13d) && directionRaw < (10d / 13d))
            {
                direction = Direction.BackTop;
            } else if (directionRaw >= (10d / 13d) && directionRaw < (11d / 13d))
            {
                direction = Direction.LeftTop;
            } else if (directionRaw >= (11d / 13d) && directionRaw < (12d / 13d))
            {
                direction = Direction.FrontTop;
            } else if (directionRaw >= (12d / 13d) && directionRaw < (13d / 13d))
            {
                direction = Direction.RightTop;
            }*/

            if (move > placeBlock && move > destroyBlock && direction != Direction.Under)
            {
                Console.WriteLine("Trying to move " + direction);
                if (direction == Direction.BackUnder || direction == Direction.BackTop)
                {
                    direction = Direction.Back;
                }
                else if (direction == Direction.RightUnder || direction == Direction.RightTop)
                {
                    direction = Direction.Right;
                }
                else if (direction == Direction.FrontUnder || direction == Direction.FrontTop)
                {
                    direction = Direction.Front;
                }
                else if (direction == Direction.LeftUnder || direction == Direction.LeftTop)
                {
                    direction = Direction.Left;
                }
                if (agentHelper.CanMoveThisDirection(direction))
                {
                    agentHelper.Move(direction, agentHelper.ShouldJumpDirection(direction));

                    actionIsPerformed = true;

                    Console.WriteLine(String.Format("Move action performed"));
                }

            }
            else if (placeBlock >= destroyBlock)
            {
                Console.WriteLine("Trying to place block  " + direction);
                if (!agentHelper.IsThereABlock(direction) || direction == Direction.Under)
                {
                    if (direction == Direction.BackTop && !agentHelper.IsThereABlock(Direction.Back))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.RightTop && !agentHelper.IsThereABlock(Direction.Right))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.FrontTop && !agentHelper.IsThereABlock(Direction.Front))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.LeftTop && !agentHelper.IsThereABlock(Direction.Left))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Back && !agentHelper.IsThereABlock(Direction.BackUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Right && !agentHelper.IsThereABlock(Direction.RightUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Front && !agentHelper.IsThereABlock(Direction.FrontUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Left && !agentHelper.IsThereABlock(Direction.LeftUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }

                    agentHelper.PlaceBlock(direction);
                    actionIsPerformed = true;
                    Console.WriteLine(String.Format("Place action performed"));

                }
            }
            else
            {
                Console.WriteLine("Trying to destroy block  " + direction);
                if (agentHelper.IsThereABlock(direction))
                {
                    agentHelper.DestroyBlock(direction);

                    Console.WriteLine(String.Format("Destroy action performed"));

                    actionIsPerformed = true;
                }
            }
            if (!actionIsPerformed)
            {
                Console.WriteLine(String.Format("No action"));
            }
        }

        // Method for passing outputs of the neural network to the client (second method)
        //***************************************************** SECOND CONTROLLER ***********************************************
        private void outputToCommandsCont()
        {
            double move = Brain.OutputSignalArray[0];// 0 to 1
            double strafe = Brain.OutputSignalArray[1];// 0 to 1
            double placeBlock = Brain.OutputSignalArray[2]; //  0 or 1
            double destroyBlock = Brain.OutputSignalArray[3]; //  0 or 1
            double pitch = Brain.OutputSignalArray[4];// 0 to 1
            double yaw = Brain.OutputSignalArray[5];// 0 to 1
            double jump = Brain.OutputSignalArray[6];// 0 or 1

            //Move backwards if less than 0.5, else forward
            if (move < 0.5)
            {
                if (move < 0.25)
                {
                    agentHelper.SendCommand("move", -1);
                }
                else
                {
                    agentHelper.SendCommand("move", 1);
                }
            }
            else
            {
                agentHelper.SendCommand("move", 0);
            }

            //Strafe left if less than 0.5, else strafe right
            if (strafe < 0.5)
            {
                if (strafe < 0.25)
                {
                    agentHelper.SendCommand("strafe", -1);
                }
                else
                {
                    agentHelper.SendCommand("strafe", 1);
                }
            }
            else
            {
                agentHelper.SendCommand("strafe", 0);
            }

            // place or destroy
            if (placeBlock >= destroyBlock)
            {
                //If round to 1 place a block, else dont place a block
                if (placeBlock > 0.5)
                {
                    agentHelper.SendCommand("use", 1);
                }
                else
                {
                    agentHelper.SendCommand("use", 0);
                }
            }
            else
            {
                //If round to 1 destroy a block, else dont destroy a block
                if (destroyBlock > 0.5)
                {
                    agentHelper.SendCommand("attack", 1);
                }
                else
                {
                    agentHelper.SendCommand("attack", 0);
                }
            }

            //Pitch left if less than 0.5, else Pitch right
            if (pitch < 0.6)
            {
                if (pitch < 0.3)
                {
                    agentHelper.SendCommand("pitch", -1);
                }
                else
                {
                    agentHelper.SendCommand("pitch", 1);
                }
            }
            else
            {
                agentHelper.SendCommand("pitch", 0);
            }

            //Yaw left if less than 0.5, else Yaw right
            if (yaw < 0.6)
            {
                if (yaw < 0.3)
                {
                    agentHelper.SendCommand("yaw", -1);
                }
                else
                {
                    agentHelper.SendCommand("yaw", 1);
                }
            }
            else
            {
                agentHelper.SendCommand("yaw", 0);
            }

            //If round to 1 jump, else dont jump
            if (jump < 0.5)
            {
                agentHelper.SendCommand("jump", 0);
            }
            else
            {
                agentHelper.SendCommand("jump", 1);
            }

            //Console.WriteLine(String.Format("Move:{0} Strafee:{1} Place:{2} Destroy:{3} Yaw:{4} Pitch:{5} Jump:{6}", move,strafe,placeBlock,destroyBlock,yaw,pitch,jump));
            //agentHelper.SendCommand("move " + move);
            //agentHelper.SendCommand("strafe " + strafe);
            //agentHelper.SendCommand("use " + Math.Round(placeBlock));

        }

        //public void UpdateFitness()
        //{
        //    Fitness = agentHelper.CalculateGrid();
        //}


        //***************************************************** THIRD CONTROLLER ***********************************************

        private bool outputToCommandsAbs()
        {
            bool actionIsPerformed = false;

            double move = Brain.OutputSignalArray[0];
            double placeBlock = Brain.OutputSignalArray[1];
            double destroyBlock = Brain.OutputSignalArray[2];

            Direction direction = Direction.Under;
            var highestDirection = 15;
            var highestValue = 0d;
            // find direction
            for (int i = 3; i < 16; i++)
            {
                if (Brain.OutputSignalArray[i] > highestValue)
                {
                    highestValue = Brain.OutputSignalArray[i];
                    highestDirection = i;
                }
            }
            switch (highestDirection)
            {
                case 3:
                    direction = Direction.LeftUnder;
                    break;
                case 4:
                    direction = Direction.FrontUnder;
                    break;
                case 5:
                    direction = Direction.RightUnder;
                    break;
                case 6:
                    direction = Direction.BackUnder;
                    break;
                case 7:
                    direction = Direction.Left;
                    break;
                case 8:
                    direction = Direction.Front;
                    break;
                case 9:
                    direction = Direction.Right;
                    break;
                case 10:
                    direction = Direction.Back;
                    break;
                case 11:
                    direction = Direction.LeftTop;
                    break;
                case 12:
                    direction = Direction.FrontTop;
                    break;
                case 13:
                    direction = Direction.RightTop;
                    break;
                case 14:
                    direction = Direction.BackTop;
                    break;
                case 15:
                    direction = Direction.Under;
                    break;
            }


            if (move > placeBlock && move > destroyBlock && direction != Direction.Under)
            {
                //Console.WriteLine("Trying to move " + direction);
                if (direction == Direction.BackUnder || direction == Direction.BackTop)
                {
                    direction = Direction.Back;
                }
                else if (direction == Direction.RightUnder || direction == Direction.RightTop)
                {
                    direction = Direction.Right;
                }
                else if (direction == Direction.FrontUnder || direction == Direction.FrontTop)
                {
                    direction = Direction.Front;
                }
                else if (direction == Direction.LeftUnder || direction == Direction.LeftTop)
                {
                    direction = Direction.Left;
                }
                if (agentHelper.CanMoveThisDirection(direction))
                {
                    agentHelper.Teleport(direction);
                    actionIsPerformed = true;
                    //Console.WriteLine(String.Format("Move action performed"));
                }
                else
                {
                    Console.WriteLine("Agent stuck");
                    return false;
                }
            }
            else if (placeBlock >= destroyBlock)
            {                
                if (direction == Direction.BackUnder || direction == Direction.BackTop)
                {
                    direction = Direction.Back;
                }
                else if (direction == Direction.RightUnder || direction == Direction.RightTop)
                {
                    direction = Direction.Right;
                }
                else if (direction == Direction.FrontUnder || direction == Direction.FrontTop)
                {
                    direction = Direction.Front;
                }
                else if (direction == Direction.LeftUnder || direction == Direction.LeftTop)
                {
                    direction = Direction.Left;
                }

                if (direction == Direction.Back)
                {
                    if (!agentHelper.IsThereABlock(Direction.BackUnder))
                    {
                        direction = Direction.BackUnder;
                    }
                    else
                    {
                        if (!agentHelper.IsThereABlock(Direction.Back))
                        {
                            direction = Direction.Back;
                        }
                        else
                        {
                            if (!agentHelper.IsThereABlock(Direction.BackTop))
                            {
                                direction = Direction.BackTop;
                            }
                            else
                            {
                                Console.WriteLine("Agent stuck");
                                return false;
                            }
                        }
                    }

                } else
                {
                    if(direction == Direction.Right)
                    {
                        if (!agentHelper.IsThereABlock(Direction.RightUnder))
                        {
                            direction = Direction.RightUnder;
                        }
                        else
                        {
                            if (!agentHelper.IsThereABlock(Direction.Right))
                            {
                                direction = Direction.Right;
                            }
                            else
                            {
                                if (!agentHelper.IsThereABlock(Direction.RightTop))
                                {
                                    direction = Direction.RightTop;
                                }
                                else
                                {
                                    Console.WriteLine("Agent stuck");
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        if(direction == Direction.Front)
                        {
                            if (!agentHelper.IsThereABlock(Direction.FrontUnder))
                            {
                                direction = Direction.FrontUnder;
                            }
                            else
                            {
                                if (!agentHelper.IsThereABlock(Direction.Front))
                                {
                                    direction = Direction.Front;
                                }
                                else
                                {
                                    if (!agentHelper.IsThereABlock(Direction.FrontTop))
                                    {
                                        direction = Direction.FrontTop;
                                    } else
                                    {
                                        Console.WriteLine("Agent stuck");
                                        return false;
                                    }
                                }
                            }
                        } else
                        {
                            if(direction == Direction.Left)
                            {
                                if (!agentHelper.IsThereABlock(Direction.LeftUnder))
                                {
                                    direction = Direction.LeftUnder;
                                }
                                else
                                {
                                    if (!agentHelper.IsThereABlock(Direction.Left))
                                    {
                                        direction = Direction.Left;
                                    }
                                    else
                                    {
                                        if (!agentHelper.IsThereABlock(Direction.LeftTop))
                                        {
                                            direction = Direction.LeftTop;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Agent stuck");
                                            return false;
                                        }
                                    }
                                }
                            } else
                            {
                                if (direction == Direction.Under)
                                {
                                    direction = Direction.Under;
                                } else
                                {
                                    Console.WriteLine("Agent stuck");
                                    return false;
                                }
                            }
                        }
                    }
                }

                agentHelper.PlaceBlockAbsolute(direction);
                actionIsPerformed = true;
                agentHelper.setGridPosition(direction, true);

                /*
                if (!agentHelper.IsThereABlock(direction) || direction == Direction.Under)
                {
                    if (direction == Direction.BackTop && !agentHelper.IsThereABlock(Direction.Back))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.RightTop && !agentHelper.IsThereABlock(Direction.Right))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.FrontTop && !agentHelper.IsThereABlock(Direction.Front))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.LeftTop && !agentHelper.IsThereABlock(Direction.Left))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Back && !agentHelper.IsThereABlock(Direction.BackUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Right && !agentHelper.IsThereABlock(Direction.RightUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Front && !agentHelper.IsThereABlock(Direction.FrontUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    else if (direction == Direction.Left && !agentHelper.IsThereABlock(Direction.LeftUnder))
                    {
                        //Console.WriteLine(String.Format("No action"));
                        return;
                    }
                    agentHelper.PlaceBlockAbsolute(direction);
                    actionIsPerformed = true;
                    //Console.WriteLine(String.Format("Place action performed"));
                    agentHelper.setGridPosition(direction, true);                    
                }
                */
            }
            else
            {
                //Console.WriteLine("Trying to destroy block  " + direction);
                if (agentHelper.IsThereABlock(direction))
                {
                    agentHelper.DestroyBlockAbsolute(direction);

                    //Console.WriteLine(String.Format("Destroy action performed"));

                    actionIsPerformed = true;

                    agentHelper.setGridPosition(direction, false);
                } else
                {
                    Console.WriteLine("Agent stuck");
                    return false;
                }
            }

            return actionIsPerformed;
        }
    }
}
