namespace LogInLab.Domain.Entities
{
    public class WebAuthnCredential
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public byte[] CredentialId { get; set; } = Array.Empty<byte>();
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public uint SignatureCounter { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
