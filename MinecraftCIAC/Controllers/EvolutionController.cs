using MinecraftCIAC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;


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

        public ActionResult Evolve()
        {
            TempData["msg"] = "<script>alert('Happy thoughts');</script>";
            return View();
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
