using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class ApartmentStaffAssignmentConfiguration : IEntityTypeConfiguration<ApartmentStaffAssignment>
{
    public void Configure(EntityTypeBuilder<ApartmentStaffAssignment> builder)
    {
        builder.ToTable("apartment_staff_assignments");

        builder.HasKey(assignment => assignment.Id);

        builder.HasIndex(assignment => new { assignment.ApartmentId, assignment.UserId }).IsUnique();

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(assignment => assignment.ApartmentId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.UserId);
    }
}