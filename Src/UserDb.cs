using Microsoft.EntityFrameworkCore;

namespace ao13back.Src;

class UserDb : DbContext
{
    public UserDb(DbContextOptions<UserDb> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
}