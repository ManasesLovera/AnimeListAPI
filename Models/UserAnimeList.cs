namespace AnimeListAPI.Models
{
    public class UserAnimeList
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AnimeId { get; set; }
        public string Status { get; set; } = string.Empty;
        public User? User { get; set; }
        public Anime? Anime { get; set; }
    }
}
