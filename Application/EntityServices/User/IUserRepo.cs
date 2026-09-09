namespace Application.EntityServices.User;

public interface IUserRepo
{
    Task<Domain.Entities.User?> GetAsync(int id);
    Task<Domain.Entities.User?> GetUserByUsername(string username);
}