using System.ComponentModel.DataAnnotations;

namespace LogInLab.Models.ViewModels
{
    public class VerifyLoginMfaViewModel
    {
        [Required]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }
}
