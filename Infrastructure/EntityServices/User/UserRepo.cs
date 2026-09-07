using Application.EntityServices.User;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EntityServices.User;

public class UserRepo(AppDbContext db) : IUserRepo
{
    public Task<Domain.Entities.User?> GetUser(int id)
    {
        return db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);
    }
}
