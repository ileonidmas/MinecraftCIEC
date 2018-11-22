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
                
        public ActionResult Evolve(int id = -1, int fitness = -1)
        {
            MalmoClientPool clientPool = Global.GloabalVariables.MalmoClientPool;
            string username = "Leo";//Request.UserHostAddress;
            MinecraftBuilderExperiment experiment = new MinecraftBuilderExperiment(clientPool, "Simple",username);
            XmlDocument xmlConfig = new XmlDocument();
            if (System.Environment.UserName == "lema")
                xmlConfig.Load("C:\\Users\\lema\\Documents\\Github\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
            else
                xmlConfig.Load("C:\\Users\\Pierre\\Documents\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");


            experiment.Initialize("Minecraft", xmlConfig.DocumentElement);
            NeatEvolutionAlgorithm<NeatGenome> algorithm;

            if (id != -1)
            {            
                // read current poppulation
                var reader = XmlReader.Create(FileUtility.GetUserResultPath(username) + "Population.xml");
                var list = experiment.LoadPopulation(reader);
                foreach (var genome in list)
                {
                    genome.EvaluationInfo.SetFitness(0);
                }
                list[id].EvaluationInfo.SetFitness(fitness);
                reader.Close();

                ////copy files to 0 folder and if 0 do nothing
                //if (id != 0)
                //{
                //    string folderName = id.ToString();
                //    FileUtility.CopyCanditateToParentFolder(username, folderName);
                //}


                algorithm = experiment.CreateEvolutionAlgorithm(list[0].GenomeFactory,list);

                int indexOfChamp = 0;
                foreach (var genome in list)
                {
                    if (genome.Id == algorithm.CurrentChampGenome.Id && algorithm.CurrentChampGenome.Id != 0)
                    {
                        FileUtility.CopyCanditateToParentFolder(username, indexOfChamp.ToString());
                    }
                    indexOfChamp++;
                }

                algorithm = experiment.CreateEvolutionAlgorithm(list[0].GenomeFactory,list);                
                algorithm.StartContinue();
                Thread.Sleep(1000);
                algorithm.RequestPause();
                while (algorithm.RunState != RunState.Paused)
                {
                    Thread.Sleep(100);
                }
            } else
            {

                RunMission.Evolution.FileUtility.CreateUserFolder(username);
                algorithm = experiment.CreateEvolutionAlgorithm();
            }


            // do loading screen here


            var doc = NeatGenomeXmlIO.SaveComplete(algorithm.GenomeList, false);
            doc.Save(FileUtility.GetUserResultPath(username) + "Population.xml");
            TempData["msg"] = "<script>alert('Happy thoughts');</script>";
            List<Evolution> evolutions = new List<Evolution>();



            for(int i = 0;i< algorithm.GenomeList.Count; i++)
            {
                string folderName = i.ToString();
                string videoPath = "";
                if (id != -1 && i == 0)
                {
                    videoPath = FileUtility.GetVideoPathWithoutDecoding(username, "0");
                }
                else
                {
                    videoPath = FileUtility.DecodeArchiveAndGetVideoPath(username, folderName);
                }
                Evolution evolution = new Evolution() { ID = i, DirectoryPath = FileUtility.GetUserResultVideoPath(username,folderName), BranchID = i };
                evolutions.Add(evolution);
            }
            
            return View(evolutions);
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


        public ActionResult Continue(string filesLocation)
        {
            //get username
            string username = "Leo";

            //create clean user folder in results
            RunMission.Evolution.FileUtility.CreateUserFolder(username);

            // move files to username folder in Results
            FileUtility.CopyFilesToUserFolder(filesLocation, username);
            
            //recreate experiment to load poppulation
            MalmoClientPool clientPool = Global.GloabalVariables.MalmoClientPool;
            MinecraftBuilderExperiment experiment = new MinecraftBuilderExperiment(clientPool, "Simple", username);
            XmlDocument xmlConfig = new XmlDocument();
            if (System.Environment.UserName == "lema")
                xmlConfig.Load("C:\\Users\\lema\\Documents\\Github\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
            else
                xmlConfig.Load("C:\\Users\\Pierre\\Documents\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
            experiment.Initialize("Minecraft", xmlConfig.DocumentElement);

            // read current poppulation
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

        public ActionResult Publish()
        {

            // save to candidate path
            string evolutionPath = FileUtility.SaveCurrentProgressAndReturnPath("Leo");
            string videoPath = FileUtility.GetVideoPathFromEvolutionPath(evolutionPath);
            // add stuff to database

            int count = db.Evolutions.Count() + 1;
            db.Evolutions.Add(new Evolution() { ID = count, BranchID = 2, DirectoryPath = evolutionPath, ParentVideoPath = videoPath});

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
