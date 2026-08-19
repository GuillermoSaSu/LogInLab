namespace LogInLab.Domain.Entities
{
    public class MfaSecret
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string SecretKeyEncripted { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
