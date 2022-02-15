using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Models;
using Service.Data;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Service.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _appEnvironment;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _appEnvironment = appEnvironment;
        }

        public IActionResult Index()
        {
            return LocalRedirect("~/Home/document?entity=todo");
        }
        public IActionResult Document(string entity, int id)
        {
            if (System.IO.File.Exists(_appEnvironment.ContentRootPath + "\\wwwroot\\patterns\\documents\\" + entity + ".html"))
            {
                ViewBag.Entity = entity;
            } else
            {
                ViewBag.Entity = "generic";
            }
            ViewBag.PatternType = "documents";
            ViewBag.Id = id;
            return View();
        }


        public IActionResult List(string entity)
        {
            if (System.IO.File.Exists(_appEnvironment.ContentRootPath + "\\wwwroot\\patterns\\lists\\" + entity + ".html"))
            {
                ViewBag.Entity = entity;
            }
            else
            {
                ViewBag.Entity = "generic";
            }
            ViewBag.PatternType = "lists";
            ViewBag.Id = 0;
            return View();
        }

        [Route("Download")]
        public FileResult Download(string filename)
        {
            string path = _appEnvironment.ContentRootPath + "\\Files\\" + filename;
            string type = "aplication/" + Path.GetExtension(filename);
            FileStream fs = new FileStream(path, FileMode.Open);
            return File(fs, type, filename);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
