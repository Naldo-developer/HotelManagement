using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration; 
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class GuestRepository : IGuestRepository
{
    private readonly HotelManagementDbContext _context;

    public GuestRepository(HotelManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Guest?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _context.Guests 
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GuestId == id, cancellationToken);
    }
    

    public async Task<IReadOnlyList<Guest>> GetAllAsync(
        string? name,
        string? documentType,
        string? documentNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Guest> query = _context.Guests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var text = name.Trim().ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(text) ||
                g.LastName.ToLower().Contains(text));
        }

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            var type = documentType.Trim();
            query = query.Where(g => g.DocumentType == type);
        }

        if (!string.IsNullOrWhiteSpace(documentNumber))
        {
            var number = documentNumber.Trim();
            query = query.Where(g => g.DocumentNumber == number);
        }

        return await query
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .ThenBy(g => g.GuestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}