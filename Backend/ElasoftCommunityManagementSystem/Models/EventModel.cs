using System.ComponentModel.DataAnnotations;

namespace ElasoftCommunityManagementSystem.Models
{
    public class EventModel
    {
        [Key]
        public int EventId { get; set; }
        
        [Required]
        public int ClubId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }

        // ⏰ Başlangıç ve bitiş tarihleri
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public int MaxParticipants { get; set; } // 🧍 Katılımcı limiti
        
        public string? ImageUrl { get; set; } // 🖼️ Afiş görseli
        
        public byte[]? Image { get; set; } // Resim verisi (base64 için)

        public ClubModel Club { get; set; }
        public ICollection<EventParticipantModel> EventParticipants { get; set; }
        public int ParticipantCount { get; set; } = 0;
        public string Status { get; set; } = "pending"; // default olarak "pending"
        public DateTime? UpdatedAt { get; set; }


    }
}
