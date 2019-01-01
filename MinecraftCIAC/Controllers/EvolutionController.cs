using MinecraftCIAC.Models;
using MinecraftCIAC.Malmo;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.IO;
using System.Xml;
using MinecraftCIAC.Global;
using Microsoft.Research.Malmo;
using RunMission.Evolution.Enums;
using System.Diagnostics;

namespace MinecraftCIAC.Controllers
{
    public class EvolutionController : Controller
    {

        private EvolutionDBContext db = new EvolutionDBContext();
        private readonly object userIDLock = new object();

        // GET: Evolution
        public ActionResult Index()
        {
            //id part
            int userId = 0;
            lock(userIDLock)
            {
                userId = FileUtility.GetUserId();
            }

            HttpContext.Session.Add("userId", userId);
            HttpContext.Session.Add("sequence", "");
            HttpContext.Session.Add("branchId", "");

            var list = db.Evolutions.ToList();
            foreach (var evolution in list)
            {
                string result = Path.GetFileName(evolution.DirectoryPath);
                string path = FileUtility.GetUserDBVideoPath(evolution.Username, result);
                evolution.ParentVideoPath = path;
            }
            return View(list);           
        }
        
        
        /// <summary>
        /// Method for performing evolutions
        /// </summary>
        /// <param name="id">Indicates if its a new evolution or not. Id of chosen individual by CIEC</param>
        /// <param name="fitness">Fitness given by user input for the chosen individual</param>
        /// <param name="novelty">Indicates whether or not to perform novelty search</param>
        public ActionResult Evolve(int id = -1, int fitness = 0)
        {
            // Get username for this evolution
            string username = HttpContext.Session["userId"].ToString().ToString();//Request.UserHostAddress;

            // Prepares next evolution view
            List<Evolution> evolutions = new List<Evolution>();

            // Get client pool used for running the evaluations
            MalmoClientPool clientPool = Global.GloabalVariables.MalmoClientPool;

            // Initialize the experiment and the evaluator object (MinecraftSimpleEvaluator)
            MinecraftBuilderExperiment experiment;
            if (fitness < 0)
            {
                experiment = new MinecraftBuilderExperiment(clientPool, "Novelty", username);
            } else
            {
                experiment = new MinecraftBuilderExperiment(clientPool, "Simple", username);
            }
            XmlDocument xmlConfig = new XmlDocument();

            xmlConfig.Load(System.AppDomain.CurrentDomain.BaseDirectory + "minecraft.config.xml");
            experiment.Initialize("Minecraft", xmlConfig.DocumentElement);

            // The evolutionary algorithm object
            NeatEvolutionAlgorithm<NeatGenome> algorithm;

            if (id != -1)
            {
                if (fitness < 0)
                {
                    //Novelty

                    //Initialize experiment
                    experiment.Initialize("Novelty", xmlConfig.DocumentElement);

                    //load population, choose selected as parent and create offsprings
                    var reader = XmlReader.Create(FileUtility.GetUserResultPath(username) + "Population.xml");
                    var list = experiment.LoadPopulation(reader);
                    var parent = list[id];
                    List<NeatGenome> offSprings = new List<NeatGenome>();
                    offSprings.Add(parent);
                    while (offSprings.Count != GloabalVariables.POPULATION_SIZE)
                        offSprings.Add(parent.CreateOffspring(parent.BirthGeneration));

                    reader.Close();

                    // add novel structure to archive
                    FileUtility.SaveNovelStructure(username, id.ToString());

                    //save chosen to parent
                    FileUtility.CopyCanditateToParentFolder(username, id.ToString());
                    
                    // Initialize algorithm object using the current generation
                    algorithm = experiment.CreateEvolutionAlgorithm(offSprings[0].GenomeFactory, offSprings);

                    
                    // Continue evolution after first generation if stop condition hasnt been met
                    if (!algorithm.StopConditionSatisfied)
                    {
                        algorithm.StartContinue();                        
                    }

                    while (algorithm.RunState != RunState.Paused && algorithm.RunState != RunState.Ready)
                    {
                        Thread.Sleep(100);
                    }


                    // save separate genomes to population.xml
                    var tempList = new List<NeatGenome>();
                    for (int i = 0; i < algorithm.GenomeList.Count; i++)
                    {
                        reader = XmlReader.Create(FileUtility.GetUserResultPath(username) + i.ToString()+ "/" +"genome.xml");
                        list = experiment.LoadPopulation(reader);
                        tempList.Add(list[0]);
                        reader.Close();
                    }

                    var doc = NeatGenomeXmlIO.SaveComplete(tempList, false);
                    doc.Save(FileUtility.GetUserResultPath(username) + "Population.xml");

                    // Save population after evaluating the generation
                    string action = "2";
                    string sequence = HttpContext.Session["sequence"].ToString();
                    HttpContext.Session.Add("sequence", sequence + action);
                    for (int i = 0; i < algorithm.GenomeList.Count; i++)
                    {
                        string folderName = i.ToString();
                        string videoPath = "";
                        if (i == 0)
                            videoPath = FileUtility.GetVideoPathWithoutDecoding(username, folderName);
                        else
                            videoPath = FileUtility.DecodeArchiveAndGetVideoPath(username, folderName);
                        Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i, Username = HttpContext.Session["userId"].ToString() };
                        evolutions.Add(evolution);
                    }


                }
                else
                {
                    //IEC

                    if(fitness == 1)
                    {
                        experiment.Initialize("Small mutation", xmlConfig.DocumentElement);
                    } else if(fitness == 2)
                    {
                        experiment.Initialize("Big mutation", xmlConfig.DocumentElement);
                    }

                    // read current population and set fitness of the chosen genome
                    var reader = XmlReader.Create(FileUtility.GetUserResultPath(username) + "Population.xml");
                    var list = experiment.LoadPopulation(reader);
                    foreach (var genome in list)
                    {
                        genome.EvaluationInfo.SetFitness(0);
                    }
                    list[id].EvaluationInfo.SetFitness(fitness);
                    reader.Close();

                    // add novel structure to archive
                    FileUtility.SaveNovelStructure(username, id.ToString());

                    // Initialize algorithm object using the current generation
                    algorithm = experiment.CreateEvolutionAlgorithm(list[0].GenomeFactory, list);

                    // Copy video files of the generation champion into the parent folder and delete the other 
                    // folders to allow for new candidate videos
                    int indexOfChamp = 0;
                    foreach (var genome in list)
                    {                        
                        if (genome.Id == algorithm.CurrentChampGenome.Id && algorithm.CurrentChampGenome.Id != 0)
                        {
                            FileUtility.CopyCanditateToParentFolder(username, indexOfChamp.ToString());
                        }
                        indexOfChamp++;
                    }

                    // Perform evaluation of a generation. Pause shortly after to ensure that the algorithm only
                    // evaluates one generation
                    algorithm.StartContinue();
                    Thread.Sleep(5000);
                    algorithm.RequestPause();

                    // Wait for the evaluation of the generation to be done
                    while (algorithm.RunState != RunState.Paused)
                    {
                        Thread.Sleep(100);
                    }

                    // Save population after evaluating the generation
                    var doc = NeatGenomeXmlIO.SaveComplete(algorithm.GenomeList, false);
                    doc.Save(FileUtility.GetUserResultPath(username) + "Population.xml");

                    string action = "";
                    if (fitness == 1)
                        action = "0";
                    else
                        action = "1";
                    string sequence = HttpContext.Session["sequence"].ToString();
                    HttpContext.Session.Add("sequence", sequence + action);

                    for (int i = 0; i < algorithm.GenomeList.Count; i++)
                    {
                        string folderName = i.ToString();
                        string videoPath = "";
                        if(i == 0)
                            videoPath = FileUtility.GetVideoPathWithoutDecoding(username, folderName);
                        else
                            videoPath = FileUtility.DecodeArchiveAndGetVideoPath(username, folderName);
                        Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i, Username = HttpContext.Session["userId"].ToString() };
                        evolutions.Add(evolution);
                        FileUtility.SaveCurrentGenome(username, i.ToString(), algorithm.GenomeList[i]);
                    }

                }
            } else
            {
                // Perform new evolution

                experiment.Initialize("Minecraft", xmlConfig.DocumentElement);

                // Create folders for the user
                FileUtility.CreateUserFolder(username);

                // Create a new evolution algorithm object with an initial generation
                algorithm = experiment.CreateEvolutionAlgorithm();

                // Save population after evaluating the generation
                var doc = NeatGenomeXmlIO.SaveComplete(algorithm.GenomeList, false);
                doc.Save(FileUtility.GetUserResultPath(username) + "Population.xml");

                for (int i = 0; i < algorithm.GenomeList.Count; i++)
                {
                    string folderName = i.ToString();
                    string videoPath = FileUtility.DecodeArchiveAndGetVideoPath(username, folderName);                    
                    Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = -1, Sequence = ""};
                    evolutions.Add(evolution);
                    FileUtility.SaveCurrentGenome(username, i.ToString(), algorithm.GenomeList[i]);
                }

                HttpContext.Session.Add("branchId", "-1");
                return View("FirstEvolution", evolutions);
            }
            return View(evolutions);
        }

       

        /// <summary>
        /// Method to continue on another users saved progress of their evolution
        /// </summary>
        /// <param name="filesLocation">Path to the files of the evolution a user wants to branch from</param>
        public ActionResult Continue(string filesLocation,string oldUsername, string sequence)
        {
            // get username
            string username = HttpContext.Session["userId"].ToString();
            // set sequence if it existed
            if(sequence == null)
                HttpContext.Session.Add("sequence", "" );
            // set previous username for branching

            HttpContext.Session.Add("branchId", oldUsername);

            // create clean user folder in Results
            FileUtility.CreateUserFolder(username);

            // move files to username folder in Results
            FileUtility.CopyFilesToUserFolder(filesLocation, username, oldUsername);

            // Get client pool used for running the evaluations
            MalmoClientPool clientPool = Global.GloabalVariables.MalmoClientPool;

            //recreate experiment and load poppulation
            MinecraftBuilderExperiment experiment = new MinecraftBuilderExperiment(clientPool, "Simple", username);
            XmlDocument xmlConfig = new XmlDocument();

            xmlConfig.Load(System.AppDomain.CurrentDomain.BaseDirectory + "minecraft.config.xml");
            experiment.Initialize("Minecraft", xmlConfig.DocumentElement);

            // read current population
            var reader = XmlReader.Create(FileUtility.GetUserResultPath(username) + "Population.xml");
            var list = experiment.LoadPopulation(reader);
            reader.Close();

            //load video files
            List<Evolution> evolutions = new List<Evolution>();
            for (int i = 0; i < list.Count; i++)
            {
                string folderName = i.ToString();
                string videoPath = "";
                videoPath = FileUtility.GetVideoPathWithoutDecoding(username, i.ToString());
                
                //TODO: Change branch ID to 0
                Evolution evolution = new Evolution() { ID = i , DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i, Username = HttpContext.Session["userId"].ToString(), Sequence = sequence};
                evolutions.Add(evolution);
            }

            if (sequence== null)
                return View("FirstEvolution", evolutions);
            return View("Evolve", evolutions);
        }
        
        public ActionResult Return()
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Method for publishing an evolution
        /// </summary>
        public ActionResult Publish()
        {
            // save to candidate path
            string evolutionPath = FileUtility.SaveCurrentProgressAndReturnPath(HttpContext.Session["userId"].ToString());
            string videoPath = FileUtility.GetVideoPathFromEvolutionPath(evolutionPath);

            // add stuff to database
            int count = db.Evolutions.Count() + 1;

            //TODO: Change BranchID to ID of the chosen evolution, if this is a continution of another users progress
            db.Evolutions.Add(new Evolution() { ID = count, BranchID = int.Parse(HttpContext.Session["branchId"].ToString()), DirectoryPath = evolutionPath, ParentVideoPath = videoPath, Username = HttpContext.Session["userId"].ToString(), Sequence = HttpContext.Session["sequence"].ToString()});

            db.SaveChanges();
            return RedirectToAction("Index");
        }
       

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}
