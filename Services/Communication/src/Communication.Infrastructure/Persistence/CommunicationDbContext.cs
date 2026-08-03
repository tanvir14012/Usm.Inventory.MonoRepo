using Communication.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace Communication.Infrastructure.Persistence;

public class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options)
    : ServiceDbContext(options, "communication")
{
    public DbSet<Notification> Notifications => Set<Notification>();
}
