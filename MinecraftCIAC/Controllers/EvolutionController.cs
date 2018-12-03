using MinecraftCIAC.Models;
using RunMission.Evolution;
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
using RunMission.Evolution.RunMission.Evolution;
using MinecraftCIAC.Global;

namespace MinecraftCIAC.Controllers
{
    public class EvolutionController : Controller
    {

        private EvolutionDBContext db = new EvolutionDBContext();

        // GET: Evolution
        public ActionResult Index()
        {
           // var ip = getIPAddress(HttpContext.Request);
            
            var list = db.Evolutions.ToList();
            foreach (var evolution in list)
            {
                string result = Path.GetFileName(evolution.DirectoryPath);
                string path = FileUtility.GetUserDBVideoPath("Leo", result);
                evolution.ParentVideoPath = path;
            }
            //list.ElementAt(0).DirectoryPath = "video.mp4";

            return View(list);            
            //return View();
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
            string username = "Leo";//Request.UserHostAddress;

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
            //xmlConfig.Load(@"C:\Users\christopher\Documents\GitHub\MinecraftCIEC\malmoTestAgentInterface\minecraft.config.xml");
            if (System.Environment.UserName == "lema")
                xmlConfig.Load("C:\\Users\\lema\\Documents\\Github\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
            else
                xmlConfig.Load("C:\\Users\\Pierre\\Documents\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");

            experiment.Initialize("Minecraft", xmlConfig.DocumentElement);

            // The evolutionary algorithm object
            NeatEvolutionAlgorithm<NeatGenome> algorithm;

            if (id != -1)
            {
                if (fitness < 0)
                {
                    //Transition CIEC -> Novelty

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

                    while (algorithm.RunState != RunState.Paused)
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

                    for (int i = 0; i < algorithm.GenomeList.Count; i++)
                    {
                        string folderName = i.ToString();
                        string videoPath = "";
                        if (i == 0)
                            videoPath = FileUtility.GetVideoPathWithoutDecoding(username, folderName);
                        else
                            videoPath = FileUtility.DecodeArchiveAndGetVideoPath(username, folderName);
                        Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i };
                        evolutions.Add(evolution);
                    }


                }
                else
                {
                    //Transition CIEC -> CIEC

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

                    for (int i = 0; i < algorithm.GenomeList.Count; i++)
                    {
                        string folderName = i.ToString();
                        string videoPath = "";
                        if(i == 0)
                            videoPath = FileUtility.GetVideoPathWithoutDecoding(username, folderName);
                        else
                            videoPath = FileUtility.DecodeArchiveAndGetVideoPath(username, folderName);
                        Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i };
                        evolutions.Add(evolution);
                        FileUtility.SaveCurrentGenome(username, i.ToString(), algorithm.GenomeList[i]);
                    }

                }
            } else
            {
                // Perform new evolution

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
                    Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i };
                    evolutions.Add(evolution);
                    FileUtility.SaveCurrentGenome(username, i.ToString(), algorithm.GenomeList[i]);
                }

                return View("FirstEvolution", evolutions);
            }


            // do loading screen here

            


            TempData["msg"] = "<script>alert('Happy thoughts');</script>";

           

            
            return View(evolutions);
        }

       

        /// <summary>
        /// Method to continue on another users saved progress of their evolution
        /// </summary>
        /// <param name="filesLocation">Path to the files of the evolution a user wants to branch from</param>
        public ActionResult Continue(string filesLocation)
        {
            // get username
            string username = "Leo";

            // create clean user folder in Results
            FileUtility.CreateUserFolder(username);

            // move files to username folder in Results
            FileUtility.CopyFilesToUserFolder(filesLocation, username);

            // Get client pool used for running the evaluations
            MalmoClientPool clientPool = Global.GloabalVariables.MalmoClientPool;

            //recreate experiment and load poppulation
            MinecraftBuilderExperiment experiment = new MinecraftBuilderExperiment(clientPool, "Simple", username);
            XmlDocument xmlConfig = new XmlDocument();
            if (System.Environment.UserName == "lema")
                xmlConfig.Load("C:\\Users\\lema\\Documents\\Github\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
            else
                xmlConfig.Load("C:\\Users\\Pierre\\Documents\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
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
                Evolution evolution = new Evolution() { ID = i , DirectoryPath = FileUtility.GetUserResultVideoPath(username, folderName), BranchID = i };
                evolutions.Add(evolution);
            }
            return View("Evolve", evolutions);

            return View(evolutions);
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
            string evolutionPath = FileUtility.SaveCurrentProgressAndReturnPath("Leo");
            string videoPath = FileUtility.GetVideoPathFromEvolutionPath(evolutionPath);

            // add stuff to database
            int count = db.Evolutions.Count() + 1;

            //TODO: Change BranchID to ID of the chosen evolution, if this is a continution of another users progress
            db.Evolutions.Add(new Evolution() { ID = count, BranchID = 2, DirectoryPath = evolutionPath, ParentVideoPath = videoPath});

            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Evolution evo = db.Evolutions.Find(id);
            if (evo == null)
            {
                return HttpNotFound();
            }

            db.Evolutions.Remove(evo);
            db.SaveChanges();
            FileUtility.DeleteDirectory(evo.DirectoryPath);
            return RedirectToAction("Index");
        }

        /*
       public static string getIPAddress(HttpRequestBase request)
       {
           string szRemoteAddr = request.UserHostAddress;
           string szXForwardedFor = request.ServerVariables["X_FORWARDED_FOR"];
           string szIP = "";

           if (szXForwardedFor == null)
           {
               szIP = szRemoteAddr;
           }
           else
           {
               szIP = szXForwardedFor;
               if (szIP.IndexOf(",") > 0)
               {
                   string[] arIPs = szIP.Split(',');

                   foreach (string item in arIPs)
                   {

                           if (item!= null)
                           return item;

                   }
               }
           }
           return szIP;
       }
       */

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
