using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.SearchStaffCandidate;

public sealed record SearchStaffCandidateQuery(Guid ApartmentId, string Email) : IQuery<StaffCandidateResponse>;