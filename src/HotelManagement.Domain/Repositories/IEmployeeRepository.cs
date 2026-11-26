using HotelManagement.Domain.Entities;

namespace HotelManagement.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(
        long? hotelId,
        CancellationToken cancellationToken);

    Task<Employee?> GetByIdAsync(
        long employeeId,
        CancellationToken cancellationToken);

    Task<Employee> CreateAsync(
        Employee employee,
        CancellationToken cancellationToken);

    Task<Employee> UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken);
}