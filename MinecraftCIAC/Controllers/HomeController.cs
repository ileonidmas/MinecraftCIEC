﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace MinecraftCIAC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        bool mjau = false;

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            Thread t1 = new Thread(new ThreadStart(CatMethod));
            t1.Start();
            while (!mjau)
            {
                Thread.Sleep(100);
            }

            return View();
        }

        private void CatMethod()
        {
            Thread.Sleep(10000);
            mjau = true;
        }
    }
}