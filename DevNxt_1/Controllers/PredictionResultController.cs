using DevNxt_1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DevNxt_1.Controllers
{
    public class PredictionResultController : Controller
    {
        // GET: PredictionResult
        public ActionResult Index()
        {
            IList<Result> predictionModel = (IList<Result>)Session["prediction"];
            TempData["prediction"] = predictionModel;
            return View();
        }
    }
}