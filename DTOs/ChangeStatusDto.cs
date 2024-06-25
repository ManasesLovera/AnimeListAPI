using System.ComponentModel.DataAnnotations;

namespace AnimeListAPI.DTOs
{
    public class ChangeStatusDto
    {
        [Required]
        public int AnimeId { get; set; }
        [Required]
        public string Status {  get; set; } = string.Empty;
    }
}
