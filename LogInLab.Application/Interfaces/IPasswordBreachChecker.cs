namespace LogInLab.Application.Interfaces
{
    public interface IPasswordBreachChecker
    {
        Task<bool> IsBreachedAsync(string password);
    }
}
