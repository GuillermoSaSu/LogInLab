namespace LogInLab.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool verify(string password, string hashedPassword);
    }
}
