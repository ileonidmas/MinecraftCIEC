﻿using Microsoft.Research.Malmo;
using RunMission.Evolution.Enums;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MinecraftCIAC.Malmo
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
                        
                        return;
                    }
                    else if (direction == Direction.RightTop && !agentHelper.IsThereABlock(Direction.Right))
                    {
                        
                        return;
                    }
                    else if (direction == Direction.FrontTop && !agentHelper.IsThereABlock(Direction.Front))
                    {
                        
                        return;
                    }
                    else if (direction == Direction.LeftTop && !agentHelper.IsThereABlock(Direction.Left))
                    {
                        
                        return;
                    }
                    else if (direction == Direction.Back && !agentHelper.IsThereABlock(Direction.BackUnder))
                    {
                        
                        return;
                    }
                    else if (direction == Direction.Right && !agentHelper.IsThereABlock(Direction.RightUnder))
                    {
                        
                        return;
                    }
                    else if (direction == Direction.Front && !agentHelper.IsThereABlock(Direction.FrontUnder))
                    {
                        
                        return;
                    }
                    else if (direction == Direction.Left && !agentHelper.IsThereABlock(Direction.LeftUnder))
                    {
                        
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
                        
        }


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
                    if (direction == Direction.Right)
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
                        if (direction == Direction.Front)
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
                            if (direction == Direction.Left)
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
            }
            else
            {
                if (agentHelper.IsThereABlock(direction))
                {
                    agentHelper.DestroyBlockAbsolute(direction);

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
        //***************************************************** THIRD CONTROLLER ***********************************************

        private bool outputToCommandsAbsAlt()
        {
            bool actionIsPerformed = false;


            double action = Brain.OutputSignalArray[0];
            double directionDouble = Brain.OutputSignalArray[1];
            Direction direction = getDirection(directionDouble);


            if (action<= 0.33d && direction != Direction.Under) //move
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
            } else
            {
                if (action<= 0.66d)//place block
                {
                    if (AgentHelper.IsThereABlock(direction))
                    {
                        return false;
                    }
                    else
                    {
                        if (direction == Direction.Left)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.LeftUnder))
                                return false;
                        }
                        else if (direction == Direction.Right)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.RightUnder))
                                return false;
                        }
                        else if (direction == Direction.Front)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.FrontUnder))
                                return false;
                        }
                        else if (direction == Direction.Back)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.BackUnder))
                                return false;
                        }
                        else if (direction == Direction.LeftTop)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.Left))
                                return false;
                        }
                        else if (direction == Direction.RightTop)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.Right))
                                return false;
                        }
                        else if (direction == Direction.FrontTop)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.Front))
                                return false;
                        }
                        else if (direction == Direction.BackTop)
                        {
                            if (!AgentHelper.IsThereABlock(Direction.Back))
                                return false;
                        }

                        agentHelper.PlaceBlockAbsolute(direction);
                        actionIsPerformed = true;
                        agentHelper.setGridPosition(direction, true);
                    }

                } else // destroy
                {
                    if (agentHelper.IsThereABlock(direction))
                    {
                        agentHelper.DestroyBlockAbsolute(direction);
                        actionIsPerformed = true;
                        agentHelper.setGridPosition(direction, false);
                    }
                    else
                    {
                        Console.WriteLine("Agent stuck");
                        return false;
                    }
                }
            }


            return actionIsPerformed;
        }


        private Direction getDirection(double direction)
        {
            if (direction <= 0.077d) return Direction.Back;
            else if (direction <= 0.154d) return Direction.Left;
            else if (direction <= 0.231d) return Direction.Front;
            else if (direction <= 0.308d) return Direction.Right;
            else if (direction <= 0.385d) return Direction.BackTop;
            else if (direction <= 0.462d) return Direction.LeftTop;
            else if (direction <= 0.539d) return Direction.FrontTop;
            else if (direction <= 0.616d) return Direction.RightTop;
            else if (direction <= 0.693d) return Direction.BackUnder;
            else if (direction <= 0.770d) return Direction.LeftUnder;
            else if (direction <= 0.847d) return Direction.FrontUnder;
            else if (direction <= 0.924d) return Direction.RightUnder;
            else return Direction.Under;

        }

    }
}
