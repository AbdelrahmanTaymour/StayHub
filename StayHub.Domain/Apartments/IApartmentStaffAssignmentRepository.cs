using StayHub.Domain.Users;

namespace StayHub.Domain.Apartments;

public interface IApartmentStaffAssignmentRepository
{
    Task<ApartmentStaffAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
 
    Task<ApartmentStaffAssignment?> GetActiveAsync(
        Guid apartmentId,
        Guid userId,
        CancellationToken cancellationToken = default);
 
    Task<IReadOnlyList<ApartmentStaffAssignment>> GetActiveByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default);
 
    void Add(ApartmentStaffAssignment assignment);
}