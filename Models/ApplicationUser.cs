using Microsoft.AspNetCore.Identity;

namespace TutorPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string PlatformRole { get; set; } = PlatformRoles.Student;
        public string GradeLabel { get; set; }
    }
}
