namespace TutorPlatform.Models
{
    public class DailyMeme
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Caption { get; set; }
        public string ImageUrl { get; set; }
        public string Theme { get; set; }
        public int DisplayOrder { get; set; }
    }
}
