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
using System.Xml;

namespace MinecraftCIAC.Controllers
{
    public class EvolutionController : Controller
    {

        private EvolutionDBContext db = new EvolutionDBContext();

        // GET: Evolution
        public ActionResult Index()
        {
            
            var list = db.Evolutions.ToList();
            return View(list);            
            //return View();
        }

        public ActionResult Evolve(int id = -1, int fitness = -1)
        {
            MalmoClientPool clientPool = Global.GloabalVariables.MalmoClientPool;
            MinecraftBuilderExperiment experiment = new MinecraftBuilderExperiment(clientPool, "Simple");
            XmlDocument xmlConfig = new XmlDocument();
            if (System.Environment.UserName == "lema")
                xmlConfig.Load("C:\\Users\\lema\\Documents\\Github\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");
            else
                xmlConfig.Load("C:\\Users\\Pierre\\Documents\\MinecraftCIEC\\malmoTestAgentInterface\\minecraft.config.xml");


            experiment.Initialize("Minecraft", xmlConfig.DocumentElement);
            NeatEvolutionAlgorithm<NeatGenome> algorithm;

            if (id != -1)
            {
                var reader = XmlReader.Create("C:\\Temp\\tempPopulation.xml");
                var list = experiment.LoadPopulation(reader);
                foreach (var genome in list)
                {
                    genome.EvaluationInfo.SetFitness(0);
                }
                list[id].EvaluationInfo.SetFitness(fitness);
                reader.Close();

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
                algorithm = experiment.CreateEvolutionAlgorithm();
            }

            

            
            
            // do loading screen here

            var doc = NeatGenomeXmlIO.SaveComplete(algorithm.GenomeList, false);
            doc.Save("C:\\Temp\\tempPopulation.xml");
            TempData["msg"] = "<script>alert('Happy thoughts');</script>";
            List<Evolution> evolutions = new List<Evolution>();
            for(int i = 0;i< algorithm.GenomeList.Count; i++)
            {
                Evolution evolution = new Evolution() { ID = i, DirectoryPath = "~/EvolutionDB/video.mp4", BranchID = i };
                evolutions.Add(evolution);
            }
            
            return View(evolutions);
        }
       


        public ActionResult Return()
        {
            return RedirectToAction("Index");
        }

        public ActionResult Publish()
        {

            //save to db




            return RedirectToAction("Index");
        }

        public ActionResult CreateFake()
        {
            int count = db.Evolutions.Count() + 1;
            db.Evolutions.Add(new Evolution() { ID = count, BranchID = 2, DirectoryPath = "" });
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult RemoveFake(int id)
        {
            
            int count = db.Evolutions.Count();
            if (count == 0)
                return RedirectToAction("Index");
            Evolution evolution = db.Evolutions.Find(id);
            db.Evolutions.Remove(evolution);
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
