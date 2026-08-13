using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        // Part of the identity/permission schema — one context, one migration
        // history, separate schema (CQ-002).
        builder.ToTable("Resource", DentalDbContext.IdentitySchema);

        builder.HasKey(resource => resource.Id);
        builder.Property(resource => resource.Id).ValueGeneratedNever().HasMaxLength(128);

        builder.Property(resource => resource.Name).HasMaxLength(100);
        builder.Property(resource => resource.Route).IsRequired().HasMaxLength(200);
        builder.Property(resource => resource.IsPublic).IsRequired();

        // DR-015 resolves a route name to a Resource on every access check.
        builder.HasIndex(resource => resource.Route);
    }
}
