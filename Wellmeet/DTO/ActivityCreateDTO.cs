using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Wellmeet.Core.Enums;

namespace Wellmeet.DTO
{
    public record ActivityCreateDTO
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "City must not exceed 100 characters.")]
        public string? City { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "Location must not exceed 100 characters.")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [EnumDataType(typeof(ActivityCategory), ErrorMessage = "Invalid category.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActivityCategory Category { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public DateTime EndDateTime { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [Range(1, 100, ErrorMessage = "Maximum participants must be between 1 and 100.")]
        public int MaxParticipants { get; set; } = 10;
    }
}
