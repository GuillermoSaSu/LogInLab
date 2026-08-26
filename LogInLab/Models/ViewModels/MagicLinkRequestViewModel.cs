using System.ComponentModel.DataAnnotations;

namespace LogInLab.Models.ViewModels
{
    public class MagicLinkRequestViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string  Email { get; set; } = string.Empty;
    }
}
