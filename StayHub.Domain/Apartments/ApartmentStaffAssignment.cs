using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Domain.Apartments;

public sealed class ApartmentStaffAssignment : Entity
{
    private ApartmentStaffAssignment(Guid id, Guid apartmentId, Guid userId, ApartmentStaffRole role, DateTime utcNow) : base(id)
    {
        ApartmentId = apartmentId;
        UserId = userId;
        Role = role;
        CreatedOnUtc = utcNow;
    }
    
    public Guid ApartmentId { get; private set; }
    public Guid UserId { get; private set; }
    public ApartmentStaffRole Role { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    public static ApartmentStaffAssignment Create(Guid apartmentId, Guid userId, ApartmentStaffRole role,
        DateTime utcNow)
    {
        var assignment = new ApartmentStaffAssignment(
            Guid.NewGuid(),
            apartmentId,
            userId,
            role,
            utcNow);
        
        assignment.RaiseDomainEvent(new ApartmentStaffAssignmentCreatedDomainEvent(assignment.Id, apartmentId, userId));
        
        return assignment;
    }
    
    public Result ChangeRole(ApartmentStaffRole role)
    {
        if (RevokedOnUtc is not null)
        {
            return Result.Failure(ApartmentStaffAssignmentErrors.AlreadyRevoked);
        }
 
        Role = role;
 
        return Result.Success();
    }
    
    public Result Revoke()
    {
        if (RevokedOnUtc is not null)
        {
            return Result.Failure(ApartmentStaffAssignmentErrors.AlreadyRevoked);
        }
 
        RevokedOnUtc = DateTime.UtcNow;
 
        RaiseDomainEvent(new ApartmentStaffAssignmentRevokedDomainEvent(Id));
 
        return Result.Success();
    }
}