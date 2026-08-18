using System.ComponentModel.DataAnnotations;

namespace LogInLab.Models.ViewModels
{
    public class MfaConfirmViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }
}
