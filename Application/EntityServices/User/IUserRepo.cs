namespace Application.EntityServices.User;

public interface IUserRepo
{
    Task<Domain.Entities.User?> GetUser(int id);
    Task<Domain.Entities.User?> GetUserByUsername(string username);
}