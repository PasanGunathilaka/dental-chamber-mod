using DentalManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission", DentalDbContext.IdentitySchema);

        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id).ValueGeneratedNever().HasMaxLength(128);

        builder.Property(permission => permission.RoleId).IsRequired().HasMaxLength(450);
        builder.Property(permission => permission.RoleName).HasMaxLength(256);
        builder.Property(permission => permission.ResourceId).IsRequired().HasMaxLength(128);

        builder.HasOne(permission => permission.Resource)
            .WithMany(resource => resource.Permissions)
            .HasForeignKey(permission => permission.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // The role FK is declared without a navigation: Permission lives in the
        // domain project, which holds no reference to ASP.NET Core Identity
        // (spec FR-02). The relationship still reaches the database.
        builder.HasOne<IdentityRole>()
            .WithMany()
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // DR-015 looks a grant up by role + resource; DR-016 seeds by role.
        builder.HasIndex(permission => new { permission.RoleId, permission.ResourceId })
            .IsUnique()
            .HasDatabaseName("IX_Permission_RoleId_ResourceId");

        builder.HasIndex(permission => permission.ResourceId);
    }
}
