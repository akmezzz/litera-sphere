using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorPlatform.Models;
using TutorPlatform.ViewModels;

namespace TutorPlatform.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var viewModel = new HomeIndexViewModel
            {
                FeatureModules = PlatformCatalog.FeatureModules.ToList(),
                ExamTracks = PlatformCatalog.ExamTracks.ToList()
            };

            return View(viewModel);
        }

        [Authorize]
        public IActionResult RedirectToDashboard(string tab = null)
        {
            if (User.IsInRole(PlatformRoles.Tutor))
            {
                return RedirectToAction("Index", "Tutor", new { tab });
            }

            return RedirectToAction("Index", "Student", new { tab });
        }
    }
}
