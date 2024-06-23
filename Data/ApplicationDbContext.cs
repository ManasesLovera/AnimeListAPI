using AnimeListAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeListAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Anime> Animes { get; set; }
        public DbSet<UserAnimeList> UserAnimeLists { get; set; }
    }
}
