using Microsoft.Research.Malmo;
using Newtonsoft.Json.Linq;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RunMission.Evolution
{
    public class MalmoClient
    {
        public NeatAgentController neatPlayer { get; set; }
        public AgentHost agentHost { get; set; }
        private MissionSpec mission;
        private WorldState worldState;
        private ClientPool availableClients;

        private bool isWorldCreated = false;

        public MalmoClient(ClientPool clientPool)
        {
            isWorldCreated = false;

            availableClients = clientPool;
        }
        
        public void RunMalmo(IBlackBox brain)
        {
            agentHost = new AgentHost();
            neatPlayer = new NeatAgentController(brain, agentHost);

            InitializeMission();

            CreateWorld();
            TryStartMission();
                        
            ConsoleOutputWhileMissionLoads();

            //set agent stuck bool
            neatPlayer.AgentNotStuck = true;

            Console.WriteLine("Mission has started!");
            
            var agentPosition = new AgentPosition();
            bool gotStartPosition = false;

            Thread.Sleep(500);

            while (worldState.is_mission_running)
            {
                //Early termination of mission in case no action was performed (agent is stuck)
                if (!neatPlayer.AgentNotStuck)
                {
                    //Ensures that the video is at least 2 seconds long, showing the users that no action actually was performed
                    Thread.Sleep(2000);

                    neatPlayer.AgentHelper.endMission();
                    break;
                }

                //Give observations to agent and let agent perform actions according to these
                worldState = agentHost.getWorldState();
                if (worldState.observations.Count == 0)
                {
                     continue;
                }
                neatPlayer.AgentHelper.ConstantObservations = worldState.observations;
                //neatPlayer.UpdateFitness();
                neatPlayer.PerformAction();

                //Get end position and fitness grid after every performed action by the agent
                if (worldState.observations != null)
                {
                    var observations = JObject.Parse(worldState.observations[0].text);

                    if (!gotStartPosition)
                    {
                        agentPosition.initialX = (double)observations.GetValue("XPos");
                        agentPosition.initialY = (double)observations.GetValue("YPos");
                        agentPosition.initialZ = (double)observations.GetValue("ZPos");

                        gotStartPosition = true;
                    }

                    agentPosition.currentX = (double)observations.GetValue("XPos");
                    agentPosition.currentY = (double)observations.GetValue("YPos");
                    agentPosition.currentZ = (double)observations.GetValue("ZPos");
                }
            }

            neatPlayer.AgentHelper.AgentPosition = agentPosition;

            Thread.Sleep(2000);
            agentHost.Dispose();

            Console.WriteLine("Mission has ended!");
        }

        public bool[] GetFitnessGrid()
        {
            return neatPlayer.AgentHelper.FitnessGrid;
        }

        public AgentPosition GetAgentPosition()
        {
            return neatPlayer.AgentHelper.AgentPosition;
        }

        private void InitializeMission()
        {
            // string xml;
            string missionXMLpath = "";
            if (System.Environment.UserName == "lema")
                missionXMLpath = System.IO.File.ReadAllText(@"C:\Users\lema\Documents\GitHub\malmoTestAgentInterface\myworld.xml");
            else
                missionXMLpath = System.IO.File.ReadAllText(@"C:\Users\Pierre\Documents\malmoTestAgentInterface\myworld.xml");

            mission = new MissionSpec(missionXMLpath, false);
            AddBlocks(mission);
            mission.setModeToCreative();
        }

        private void AddBlocks(MissionSpec mission)
        {
            int x1 = -1;
            for (int z1 = -1; z1 < 20; z1++)
            {
                for (int y = 227; y <= 227 + 10; y++)
                {
                    mission.drawBlock(x1, y, z1, "cobblestone");
                }
            }

            int z2 = -1;
            for (int x2 = -1; x2 < 20; x2++)
            {
                for (int y = 227; y <= 227 + 10; y++)
                {
                    mission.drawBlock(x2, y, z2, "cobblestone");
                }
            }

            int x3 = 20;
            for (int z3 = 0; z3 < 20; z3++)
            {
                for (int y = 227; y < 227 + 10; y++)
                {
                    mission.drawBlock(x3, y, z3, "cobblestone");
                }
            }

            int z4 = 20;
            for (int x4 = 0; x4 < 20; x4++)
            {
                for (int y = 227; y < 227 + 10; y++)
                {
                    mission.drawBlock(x4, y, z4, "cobblestone");
                }
            }

        }
        private void TryStartMission()
        {
            try
            {
                agentHost.startMission(mission, availableClients, new MissionRecordSpec(), 0, "Test Builder");
            }
            catch (Exception ex)
            {
            string errorLine = "Fatal error when starting a mission in ProgramMalmo: " + ex.Message;
            Console.WriteLine("Error starting mission: {0}", ex.Message);
            Environment.Exit(1);
            } 
        }

        private string SaferRead(string path)
        {
            string newFile = null;
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    newFile = reader.ReadToEnd();
                }
            }
            return newFile;
        }

        private void CreateWorld()
        {
            if (!isWorldCreated)
            {
                mission.forceWorldReset();
                //AddBlocks(mission);
                isWorldCreated = true;
            }
        }
        private void ConsoleOutputWhileMissionLoads()
        {
            worldState = agentHost.getWorldState();
            while (!worldState.has_mission_begun)
            {
                //Console.Write(".");
                Thread.Sleep(100);
                worldState = agentHost.getWorldState();
            }
        }
    }
}
