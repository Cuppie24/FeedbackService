namespace Application.EntityServices.User;

public interface IUserRepository
{
    Task<Domain.Entities.User?> GetUser(int id);
}