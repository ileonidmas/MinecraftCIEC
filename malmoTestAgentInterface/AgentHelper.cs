using Microsoft.Research.Malmo;
using Newtonsoft.Json.Linq;
using RunMission.Evolution;
using RunMission.Evolution.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace RunMission
{
    public class AgentHelper
    {
        #region Private members
        private AgentHost agentHost;
        private TimestampedStringVector constanteObservations;
        private bool[] fitnessGrid;
        private AgentPosition agentPosition;
        #endregion

        #region Public members
        public AgentHost AgentHost
        {
            get => agentHost;
            set => agentHost = value;
        }
        public TimestampedStringVector ConstantObservations
        {
            get => constanteObservations;
            set => constanteObservations = value;
        }

        public bool[] FitnessGrid
        {
            get => fitnessGrid;
            set => fitnessGrid = value;
        }

        public AgentPosition AgentPosition
        {
            get => agentPosition;
            set => agentPosition = value;
        }
        #endregion

        #region Constructor
        public AgentHelper(AgentHost agentHost)
        {
            fitnessGrid = new bool[20 * 20 * 20];
            this.agentHost = agentHost;
        }
        #endregion

        #region Private methods
        // going to the right ( x pos becomes negative )
        // going to the left ( x pos becomes positive) 
        // going forward ( z pos becomes positive)
        // moving backward ( z begomes negative ) 
        public void setGridPosition(Direction direction, bool value)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var x = (int)((double)observations.GetValue("XPos") + 0.5);
            var y = (int)observations.GetValue("YPos");
            var z = (int)((double)observations.GetValue("ZPos") + 0.5);

            switch (direction)
            {
                case Direction.Back:
                    z--;
                    break;
                case Direction.Front:
                    z++;
                    break;
                case Direction.Left:
                    x++;
                    break;
                case Direction.Right:
                    x--;
                    break;
                case Direction.BackTop:
                    z--;
                    y++;
                    break;
                case Direction.FrontTop:
                    z++;
                    y++;
                    break;
                case Direction.LeftTop:
                    x++;
                    y++;
                    break;
                case Direction.RightTop:
                    x--;
                    y++;
                    break;
                case Direction.BackUnder:
                    z--;
                    y--;
                    break;
                case Direction.FrontUnder:
                    z++;
                    y--;
                    break;
                case Direction.LeftUnder:
                    x++;
                    y--;
                    break;
                case Direction.RightUnder:
                    x--;
                    y--;
                    break;
                case Direction.Under:
                    if (!value)
                        y--;
                    break;
            }

            y -= 227;

            if((x < 0 || y < 0 || z < 0) || (x > 20 || y > 20 || z > 20)) {
                return;
            }

            var position = x + (z * 20) + (y * 20 * 20);
            fitnessGrid[position] = value;
        }

        private void UpdateDirection(double desiredYaw, double desiredPitch, double precision = 1)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var currentYaw = (double)observations.GetValue("Yaw");
            var currentPitch = (double)observations.GetValue("Pitch");
            var deltaYaw = 0d;
            var deltaPitch = 0d;
            do
            {
                try
                {
                    Thread.Sleep(100);
                    observations = JObject.Parse(agentHost.getWorldState().observations[0].text);

                    currentYaw = (double)observations.GetValue("Yaw");
                    currentPitch = (double)observations.GetValue("Pitch");



                    //https://stackoverflow.com/questions/38407584/continuous-aim-to-target-in-malmo
                    deltaYaw = desiredYaw - currentYaw;
                    if (deltaYaw > 180)
                        deltaYaw -= 360;
                    if (deltaYaw < -180)
                        deltaYaw += 360;

                    deltaPitch = desiredPitch - currentPitch;
                    if (deltaPitch > 180)
                        deltaPitch -= 360;
                    if (deltaPitch < -180)
                        deltaPitch += 360;

                    Look(currentYaw, deltaYaw, currentPitch, deltaPitch);

                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.WriteLine("error reading observations in Agent helper");
                }
            } while (Math.Abs(deltaYaw) > precision || Math.Abs(deltaPitch) > precision);
            agentHost.sendCommand("turn " + 0);
            agentHost.sendCommand("pitch " + 0);
        }


        private void Look(double currentYaw, double deltaYaw, double currentPitch, double deltaPitch, int precision = 1)
        {
            var perTickYaw = 18;
            var perTickPitch = 18;
            
            //Yaw control
            if (Math.Abs(deltaYaw) > precision)
            {
                if (Math.Abs(deltaYaw) - perTickYaw > 0)
                {
                    if (deltaYaw > 0)                 
                        agentHost.sendCommand(String.Format("turn {0}", 1));
                    else
                        agentHost.sendCommand(String.Format("turn {0}", -1));                  
                }
                else
                {
                    deltaYaw /= 180.0;
                    agentHost.sendCommand("turn " + FormatValue(deltaYaw));
                }
            }

            //Pitch control
            if (Math.Abs(deltaPitch) > precision)
            {                
                if (Math.Abs(deltaPitch) - perTickPitch > 0)
                {
                    if (deltaPitch > 0)
                        agentHost.sendCommand(String.Format("pitch {0}", 1));
                    else
                        agentHost.sendCommand(String.Format("pitch {0}", -1));
                }
                else
                {
                    deltaPitch /= 180.0;
                    agentHost.sendCommand("pitch " + FormatValue(deltaPitch));
                }
            }
        }

        // going to the right ( x pos becomes negative )
        // going to the left ( x pos becomes positive) 
        // going forward ( z pos becomes positive)
        // moving backward ( z begomes negative ) 
        private void MoveTo(double deltaXPos, double deltaZPos, double yaw, double precision)
        {
            var perTickMove = 0.432;
            var overStep = 0.2582;

            if (yaw > 180)
                yaw -= 360;
            if (yaw < -180)
                yaw += 360;

            if (deltaXPos > 1)
            {
                deltaXPos = 1;
            }
            else if (deltaXPos < -1)
            {
                deltaXPos = -1;
            }

            deltaXPos /= 5;

            if (Math.Abs(deltaXPos) > precision)
            {
                if ((yaw < -175 && yaw > -185) || (yaw < 185 && yaw > 175))
                {
                    if (deltaXPos > 0)
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(Math.Abs(deltaXPos))));
                    else
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(-1.0 * Math.Abs(deltaXPos))));
                }
                else if (yaw < 5 && yaw > -5)
                {
                    if (deltaXPos > 0)
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(-1.0 * Math.Abs(deltaXPos))));
                    else
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(Math.Abs(deltaXPos))));
                }
                else if (yaw < -85 && yaw > -95)
                {
                    if (deltaXPos > 0)
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(Math.Abs(deltaXPos))));
                    else
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(-1.0 * Math.Abs(deltaXPos))));
                }
                else if (yaw < 95 && yaw > 85)
                {
                    if (deltaXPos > 0)
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(-1.0 * Math.Abs(deltaXPos))));
                    else
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(Math.Abs(deltaXPos))));
                }
            }


            if(deltaZPos > 1)
            {
                deltaZPos = 1;
            } else if (deltaZPos < -1)
            {
                deltaZPos = -1;
            }

            deltaZPos /= 5;

            if (Math.Abs(deltaZPos) > precision)
            {
                if ((yaw < -175 && yaw > -185) || (yaw < 185 && yaw > 175))
                {
                    if (deltaZPos > 0)
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(-1.0 * Math.Abs(deltaZPos))));
                    else
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(Math.Abs(deltaZPos))));
                }
                else if (yaw < 5 && yaw > -5)
                {
                    if (deltaZPos > 0)
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(Math.Abs(deltaZPos))));
                    else
                        agentHost.sendCommand(String.Format("move {0}", FormatValue(-1.0 * Math.Abs(deltaZPos))));
                }
                else if (yaw < -85 && yaw > -95)
                {
                    if (deltaZPos > 0)
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(Math.Abs(deltaZPos))));
                    else
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(-1.0 * Math.Abs(deltaZPos))));
                }
                else if (yaw < 95 && yaw > 85)
                {
                    if (deltaZPos > 0)
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(-1.0 * Math.Abs(deltaZPos))));
                    else
                        agentHost.sendCommand(String.Format("strafe {0}", FormatValue(Math.Abs(deltaZPos))));
                }
            }
        }

        private string FormatValue(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Public methods

        // ****************************************************** CONTROLLER 1 ***********************************************
        public void PlaceBlock(Direction where)
        {
            switch (where)
            {
                case Direction.Front:
                    UpdateDirection(0,60);
                    break;
                case Direction.Right:
                    UpdateDirection(90, 60);
                    break;
                case Direction.Back:
                    UpdateDirection(180, 60);
                    break;
                case Direction.Left:
                    UpdateDirection(270, 60);
                    break;
                case Direction.FrontUnder:
                    UpdateDirection(0, 70);
                    break;
                case Direction.RightUnder:
                    UpdateDirection(90, 70);
                    break;
                case Direction.BackUnder:
                    UpdateDirection(180, 70);
                    break;
                case Direction.LeftUnder:
                    UpdateDirection(270, 70);
                    break;
                case Direction.FrontTop:
                    UpdateDirection(0, 45);
                    break;
                case Direction.RightTop:
                    UpdateDirection(90, 45);
                    break;
                case Direction.BackTop:
                    UpdateDirection(180, 45);
                    break;
                case Direction.LeftTop:
                    UpdateDirection(270, 45);
                    break;
                case Direction.Under:
                    var observations = JObject.Parse(constanteObservations[0].text);
                    var currentYaw = (double)observations.GetValue("Yaw");
                    UpdateDirection(currentYaw, 90);
                    agentHost.sendCommand("jump 1");
                    break;
            }

            agentHost.sendCommand("use 1");
            if(where == Direction.Under)
                Thread.Sleep(300);
            agentHost.sendCommand("use 0");
            Thread.Sleep(100);
            agentHost.sendCommand("use 0");
            agentHost.sendCommand("use 0");
            agentHost.sendCommand("jump 0");
        }

        public void DestroyBlock(Direction where)
        {
            switch (where)
            {
                case Direction.Front:
                    UpdateDirection(0, 60);
                    break;
                case Direction.Right:
                    UpdateDirection(90, 60);
                    break;
                case Direction.Back:
                    UpdateDirection(180, 60);
                    break;
                case Direction.Left:
                    UpdateDirection(270, 60);
                    break;
                case Direction.FrontUnder:
                    UpdateDirection(0, 70);
                    break;
                case Direction.RightUnder:
                    UpdateDirection(90, 70);
                    break;
                case Direction.BackUnder:
                    UpdateDirection(180, 70);
                    break;
                case Direction.LeftUnder:
                    UpdateDirection(270, 70);
                    break;
                case Direction.FrontTop:
                    UpdateDirection(0, 45);
                    break;
                case Direction.RightTop:
                    UpdateDirection(90, 45);
                    break;
                case Direction.BackTop:
                    UpdateDirection(180, 45);
                    break;
                case Direction.LeftTop:
                    UpdateDirection(270, 45);
                    break;
                case Direction.Under:
                    var observations = JObject.Parse(constanteObservations[0].text);
                    var currentYaw = (double)observations.GetValue("Yaw");
                    UpdateDirection(currentYaw, 90);
                    agentHost.sendCommand("jump 1");
                    break;
            }
            
            agentHost.sendCommand("attack 1");
            Thread.Sleep(50);
            agentHost.sendCommand("attack 0");
            agentHost.sendCommand("attack 0");
            agentHost.sendCommand("attack 0");
        }

        

        // going to the right ( x pos becomes negative )
        // going to the left ( x pos becomes positive) 
        // going forward ( z pos becomes positive)
        // moving backward ( z begomes negative ) 
        public void Move(Direction direction, bool jump = false)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var precision = 0.08d;
            var currentYaw = (double)observations.GetValue("Yaw");
            var currentXPos = (double)observations.GetValue("XPos");
            var currentYPos = (double)observations.GetValue("YPos");
            var currentZPos = (double)observations.GetValue("ZPos");
            var desiredXPos = 0d;
            var desiredYPos = currentYPos + 1;
            var desiredZPos = 0d;
            var deltaXPos = 1d;
            var deltaYPos = 0d;
            var deltaZPos = 1d;

            switch (direction)
            {
                case Direction.Left:
                    desiredXPos = currentXPos + 1;
                    desiredZPos = currentZPos;
                    break;
                case Direction.Front:
                    desiredXPos = currentXPos;
                    desiredZPos = currentZPos + 1;
                    break;
                case Direction.Right:
                    desiredXPos = currentXPos - 1;
                    desiredZPos = currentZPos;
                    break;
                case Direction.Back:
                    desiredXPos = currentXPos;
                    desiredZPos = currentZPos - 1;
                    break;
            }



            if (jump == true) { 
                agentHost.sendCommand("jump 1");
            }

            observations = JObject.Parse(constanteObservations[0].text);
            currentXPos = (double)observations.GetValue("XPos");
            currentYPos = (double)observations.GetValue("YPos");
            currentZPos = (double)observations.GetValue("ZPos");
            deltaXPos = desiredXPos - currentXPos;
            deltaYPos = desiredYPos - currentYPos;
            deltaZPos = desiredZPos - currentZPos;

            do
            {
                Thread.Sleep(100);                
                MoveTo(deltaXPos, deltaZPos, currentYaw, precision);
                observations = JObject.Parse(agentHost.getWorldState().observations[0].text);
                currentXPos = (double)observations.GetValue("XPos");
                currentYPos = (double)observations.GetValue("YPos");
                currentZPos = (double)observations.GetValue("ZPos");
                deltaXPos = desiredXPos - currentXPos;
                deltaYPos = desiredYPos - currentYPos;
                deltaZPos = desiredZPos - currentZPos;

                if (deltaYPos > 0.5 && jump)
                    agentHost.sendCommand("jump 1");
                else
                    agentHost.sendCommand("jump 0");
            } while ((deltaXPos > precision || deltaXPos < -precision) ^ (Math.Abs(deltaZPos) > precision));

            agentHost.sendCommand("strafe 0");
            agentHost.sendCommand("move 0");
            agentHost.sendCommand("jump 0");
            Thread.Sleep(100);
            agentHost.sendCommand("strafe 0");
            agentHost.sendCommand("move 0");
            agentHost.sendCommand("jump 0");
        }


        public string[] CheckSurroundings()
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var allBlocks = observations.GetValue("floor3x3x3");
            var something = allBlocks[0];
           
            return new string[13]{
                allBlocks[4].ToString(), allBlocks[1].ToString(), allBlocks[5].ToString(), allBlocks[7].ToString(), allBlocks[3].ToString(),
                allBlocks[10].ToString(), allBlocks[14].ToString(), allBlocks[16].ToString(), allBlocks[12].ToString(),
                allBlocks[19].ToString(), allBlocks[23].ToString(), allBlocks[25].ToString(), allBlocks[21].ToString() };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>

        public bool IsThereABlock(Direction direction)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var allBlocks = observations.GetValue("floor3x3x3");
            var something = allBlocks[0];
            switch (direction)
            {
                case Direction.Under:
                    if (allBlocks[4].ToString() == "air")
                        return false;
                    return true;
                case Direction.BackUnder:
                    if (allBlocks[1].ToString() == "air")
                        return false;
                    return true;
                case Direction.LeftUnder:
                    if (allBlocks[5].ToString() == "air")
                        return false;
                    return true;
                case Direction.FrontUnder:
                    if (allBlocks[7].ToString() == "air")
                        return false;
                    return true;
                case Direction.RightUnder:
                    if (allBlocks[3].ToString() == "air")
                        return false;
                    return true;
                case Direction.Back:
                    if (allBlocks[10].ToString() == "air")
                        return false;
                    return true;
                case Direction.Left:
                    if (allBlocks[14].ToString() == "air")
                        return false;
                    return true;
                case Direction.Front:
                    if (allBlocks[16].ToString() == "air")
                        return false;
                    return true;
                case Direction.Right:
                    if (allBlocks[12].ToString() == "air")
                        return false;
                    return true;
                case Direction.BackTop:
                    if (allBlocks[19].ToString() == "air")
                        return false;
                    return true;
                case Direction.LeftTop:
                    if (allBlocks[23].ToString() == "air")
                        return false;
                    return true;
                case Direction.FrontTop:
                    if (allBlocks[25].ToString() == "air")
                        return false;
                    return true;
                case Direction.RightTop:
                    if (allBlocks[21].ToString() == "air")
                        return false;
                    return true;
            }

            return false;
        }

        public bool CanMoveThisDirection(Direction direction)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var allBlocks = observations.GetValue("floor3x3x3");
            switch (direction)
            {
                
                case Direction.Back:
                    if (allBlocks[19].ToString() == "air")
                        return true;
                    return false;
                case Direction.Left:
                    if (allBlocks[23].ToString() == "air")
                        return true;
                    return false;
                case Direction.Front:
                    if (allBlocks[25].ToString() == "air")
                        return true;
                    return false;
                case Direction.Right:
                    if (allBlocks[21].ToString() == "air")
                        return true;
                    return false;
            }
            return false;
        }

        public bool CanTeleportThisDirection(Direction direction)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var allBlocks = observations.GetValue("floor3x3x3");
            switch (direction)
            {

                case Direction.Back:
                    if (allBlocks[19].ToString() == "air")
                        return true;
                    return false;
                case Direction.Left:
                    if (allBlocks[23].ToString() == "air")
                        return true;
                    return false;
                case Direction.Front:
                    if (allBlocks[25].ToString() == "air")
                        return true;
                    return false;
                case Direction.Right:
                    if (allBlocks[21].ToString() == "air")
                        return true;
                    return false;
            }
            return false;
        }

        public bool ShouldJumpDirection(Direction direction)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var allBlocks = observations.GetValue("floor3x3x3");
            var something = allBlocks[0];
            switch (direction)
            {

                case Direction.Back:
                    if (allBlocks[10].ToString() == "air")
                        return false;
                    return true;
                case Direction.Left:
                    if (allBlocks[14].ToString() == "air")
                        return false;
                    return true;
                case Direction.Front:
                    if (allBlocks[16].ToString() == "air")
                        return false;
                    return true;
                case Direction.Right:
                    if (allBlocks[12].ToString() == "air")
                        return false;
                    return true;
            }
            return false;
        }

        // ****************************************************** CONTROLLER 2 ***********************************************
        public void SendCommand(string command, double value)
        {
            agentHost.sendCommand(String.Format("{0} {1}", command, value));            
        }

        // ****************************************************** CONTROLLER 3 ***********************************************
        public void SendAbsoluteCommand(string command, double value)
        {
            agentHost.sendCommand(String.Format("{0} {1}", command, FormatValue(value)));
            Thread.Sleep(200);
        }

        public void Teleport(Direction direction)
        {
            var observations = JObject.Parse(constanteObservations[0].text);
            var currentXPos = (double)observations.GetValue("XPos");
            var currentYPos = (double)observations.GetValue("YPos");
            var currentZPos = (double)observations.GetValue("ZPos");

            if (ShouldJumpDirection(direction))
            {
                SendAbsoluteCommand("tpy", currentYPos + 1);
            }

            switch (direction)
            {
                case Direction.Left:
                    SendAbsoluteCommand("tpx", currentXPos + 1);
                    break;
                case Direction.Front:
                    SendAbsoluteCommand("tpz", currentZPos + 1);
                    break;
                case Direction.Right:
                    SendAbsoluteCommand("tpx", currentXPos - 1);
                    break;
                case Direction.Back:
                    SendAbsoluteCommand("tpz", currentZPos - 1);
                    break;
            }



        }

        public void PlaceBlockAbsolute(Direction where)
        {
            switch (where)
            {
                case Direction.Front:
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.Right:
                    SendAbsoluteCommand("setYaw", 90);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.Back:
                    SendAbsoluteCommand("setYaw", 180);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.Left:
                    SendAbsoluteCommand("setYaw", 270);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.FrontUnder:
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.RightUnder:
                    SendAbsoluteCommand("setYaw", 90);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.BackUnder:
                    SendAbsoluteCommand("setYaw", 180);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.LeftUnder:
                    SendAbsoluteCommand("setYaw", 270);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.FrontTop:
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.RightTop:
                    SendAbsoluteCommand("setYaw", 90);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.BackTop:
                    SendAbsoluteCommand("setYaw", 180);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.LeftTop:
                    SendAbsoluteCommand("setYaw", 270);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.Under:
                    var observations = JObject.Parse(constanteObservations[0].text);
                    var currentYPos = (double)observations.GetValue("YPos");
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 90);
                    SendAbsoluteCommand("tpy", currentYPos + 2);
                    break;
            }
            SendAbsoluteCommand("use", 1);
        }

        public void DestroyBlockAbsolute(Direction where)
        {
            switch (where)
            {
                case Direction.Front:
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.Right:
                    SendAbsoluteCommand("setYaw", 90);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.Back:
                    SendAbsoluteCommand("setYaw", 180);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.Left:
                    SendAbsoluteCommand("setYaw", 270);
                    SendAbsoluteCommand("setPitch", 60);
                    break;
                case Direction.FrontUnder:
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.RightUnder:
                    SendAbsoluteCommand("setYaw", 90);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.BackUnder:
                    SendAbsoluteCommand("setYaw", 180);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.LeftUnder:
                    SendAbsoluteCommand("setYaw", 270);
                    SendAbsoluteCommand("setPitch", 70);
                    break;
                case Direction.FrontTop:
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.RightTop:
                    SendAbsoluteCommand("setYaw", 90);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.BackTop:
                    SendAbsoluteCommand("setYaw", 180);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.LeftTop:
                    SendAbsoluteCommand("setYaw", 270);
                    SendAbsoluteCommand("setPitch", 45);
                    break;
                case Direction.Under:
                    var observations = JObject.Parse(constanteObservations[0].text);
                    var currentYPos = (double)observations.GetValue("YPos");
                    SendAbsoluteCommand("setYaw", 0);
                    SendAbsoluteCommand("setPitch", 90);
                    break;
            }

            agentHost.sendCommand("attack 1");
        }

        //Ends the mission by sending a command to the agent host
        public void endMission()
        {
            agentHost.sendCommand("quit");
        }

        #endregion
    }
}
