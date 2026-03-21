using System.Collections.Generic;
using TutorPlatform.Models;

namespace TutorPlatform.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<FeatureModule> FeatureModules { get; set; } = new List<FeatureModule>();
        public List<ExamTrack> ExamTracks { get; set; } = new List<ExamTrack>();
    }
}
