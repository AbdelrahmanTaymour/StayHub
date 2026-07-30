using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ApartmentRepository(ApplicationDbContext dbContext)
    : Repository<Apartment>(dbContext), IApartmentRepository;