using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.Research.Malmo;
using Newtonsoft.Json.Linq;
using RunMission;
using RunMission.Evolution;
using RunMission.Evolution.Enums;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;

class Program
{
    static NeatEvolutionAlgorithm<NeatGenome> _ea;

    const string CHAMPION_FILE = "minecraft_champion.xml";
    public static void Main()
    {
        //Test();
        Run();
        
    }



    //********************* Run Evaluator ***********************************

    private static void Run()
    {
        MalmoClientPool clientPool = new MalmoClientPool(2);
        MinecraftBuilderExperiment experiment = new MinecraftBuilderExperiment(clientPool, "Novelty");
        XmlDocument xmlConfig = new XmlDocument();
        xmlConfig.Load("..\\..\\..\\minecraft.config.xml");
        experiment.Initialize("Minecraft", xmlConfig.DocumentElement);
        var algorithm = experiment.CreateEvolutionAlgorithm();
        algorithm.StartContinue();
        Console.ReadKey();
    }
    static void ea_UpdateEvent(object sender, EventArgs e)
    {
        Console.WriteLine(string.Format("gen={0:N0} bestFitness={1:N6}", _ea.CurrentGeneration, _ea.Statistics._maxFitness));


        Console.WriteLine("Genome count " + _ea.GenomeList.Count);

        // Save the best genome to file
        var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { _ea.CurrentChampGenome }, false);
        doc.Save(CHAMPION_FILE);
    }




    //********************* Test Contorller ***********************************

    private static void Test()
    {
        AgentHost agentHost = new AgentHost();
        AgentHelper agentHelper = new AgentHelper(agentHost);
        try
        {
            var something = Environment.GetCommandLineArgs();
            Console.WriteLine(something);
            agentHost.parse(new StringVector(something));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: {0}", ex.Message);
            Console.Error.WriteLine(agentHost.getUsage());
            Environment.Exit(1);
        }
        if (agentHost.receivedArgument("help"))
        {
            Console.Error.WriteLine(agentHost.getUsage());
            Environment.Exit(0);
        }
        float startX = -230.5f, startY = 227.5f, startZ = -555.5f;
        /*MissionSpec mission = new MissionSpec();
        mission.timeLimitInSeconds(10000);
        //mission.requestVideo( 320, 240 );
        //mission.rewardForReachingPosition(19.5f,0.0f,19.5f,100.0f,1.1f);
        mission.startAt(startX,startY,startZ);
        */
        MissionRecordSpec missionRecord = new MissionRecordSpec("./saved_data.tgz");

        missionRecord.recordCommands();
        // missionRecord.recordMP4(20, 400000);
        //missionRecord.recordRewards();
        missionRecord.recordObservations();

        //https://microsoft.github.io/malmo/0.14.0/Schemas/Mission.html#element_AgentStart
        //https://microsoft.github.io/malmo/0.14.0/Schemas/Mission.html#element_InventoryItem

        string xml;
        if (System.Environment.UserName == "lema")
            xml = System.IO.File.ReadAllText(@"C:\Users\lema\Documents\GitHub\malmoTestAgentInterface\myworld.xml");
        else
            xml = System.IO.File.ReadAllText(@"C:\Users\Pierre\Documents\malmoTestAgentInterface\myworld.xml");

        MissionSpec mission = new MissionSpec(xml, false);
        mission.setModeToCreative();
        int attempts = 0;
        bool connected = false;
        while (!connected)
        {
            try
            {
                attempts += 1;
                agentHost.startMission(mission, missionRecord);
                connected = true;
            }
            catch (MissionException ex)
            {
                // Using catch(Exception ex) would also work, but specifying MissionException allows
                // us to access the error code:
                Console.Error.WriteLine("Error starting mission: {0}", ex.Message);
                Console.Error.WriteLine("Error code: {0}", ex.getMissionErrorCode());
                // We can do more specific error handling using this code, eg:
                if (ex.getMissionErrorCode() == MissionException.MissionErrorCode.MISSION_INSUFFICIENT_CLIENTS_AVAILABLE)
                    Console.Error.WriteLine("Have you started a Minecraft client?");
                if (attempts >= 3)   // Give up after three goes.
                    Environment.Exit(1);
                Thread.Sleep(1000); // Wait a second and try again.
            }
        }
        WorldState worldState;

        Console.WriteLine("Waiting for the mission to start");
        do
        {
            Console.Write(".");
            Thread.Sleep(100);
            worldState = agentHost.getWorldState();

            foreach (TimestampedString error in worldState.errors) Console.Error.WriteLine("Error: {0}", error.text);
        }
        while (!worldState.has_mission_begun);

        Console.WriteLine();


        Thread.Sleep(1000);

        bool runonce = false;

        do
        {
            try
            {
                if (!runonce)
                {

                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.PlaceBlockAbsolute(Direction.Front);

                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.Teleport(Direction.Left);

                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.Teleport(Direction.Left);

                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.Teleport(Direction.Left);

                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.Teleport(Direction.Left);

                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.PlaceBlockAbsolute(Direction.Under);
                    resetConstantPosition(agentHost, agentHelper);
                    agentHelper.DestroyBlockAbsolute(Direction.Under);
                    runonce = true;
                }

            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.WriteLine("Error reading observations in RunMission");
            }
            foreach (TimestampedReward reward in worldState.rewards) Console.Error.WriteLine("Summed reward: {0}", reward.getValue());
            foreach (TimestampedString error in worldState.errors) Console.Error.WriteLine("Error: {0}", error.text);
        }
        while (worldState.is_mission_running);
        //Console.ReadKey();
        Console.WriteLine("Mission has stopped.");
        Console.ReadKey();
    }


    private static void resetConstantPosition(AgentHost agentHost, AgentHelper agentHelper)
    {
        WorldState worldState = agentHost.getWorldState();
        agentHelper.ConstantObservations = worldState.observations;
    }
}
