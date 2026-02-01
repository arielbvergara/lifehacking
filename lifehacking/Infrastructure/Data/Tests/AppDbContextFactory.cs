using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data.Tests;

public static class AppDbContextFactory
{
    extension(IServiceCollection services)
    {
        public void AddInMemoryDatabase()
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("AppDb"));
        }
    }
}
