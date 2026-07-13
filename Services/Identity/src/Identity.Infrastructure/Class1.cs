using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.Localization;
using Usm.Shared.BuildingBlocks.Messaging;
using Usm.Shared.Data.DbContextExtensions;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=usm_inventory;Username=usm_admin;Password=usm_admin_dev";

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddResxLocalization();
        services.AddRabbitMqMessaging(configuration);
        return services;
    }
}

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<LoginDevice> LoginDevices => Set<LoginDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginDevice>(builder =>
        {
            builder.ToTable("login_devices");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.DeviceId).HasMaxLength(150).IsRequired();
            builder.Property(entity => entity.DisplayName).HasJsonbLocalization();
            builder.ApplySoftDeleteFilter();
        });

        base.OnModelCreating(modelBuilder);
    }
}
