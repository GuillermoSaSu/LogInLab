namespace LogInLab.Models.ViewModels
{
    public class SessionViewModel
    {
        public Guid Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime  CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsCurentSession { get; set; }
    }
}
