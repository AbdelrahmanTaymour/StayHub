using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Title)
            .HasMaxLength(200)
            .HasConversion(title => title.Value, title => new Title(title));

        builder.Property(request => request.Description)
            .HasMaxLength(2000)
            .HasConversion(description => description.Value, description => new Description(description));

        builder.HasIndex(request => new { request.ApartmentId, request.Status });

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(request => request.ApartmentId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.ReportedByUserId);

        builder.Property<uint>("Version").IsRowVersion();
    }
}