using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Application.Exceptions;
using Npgsql;

namespace HotelManagement.Infrastructure.Repositories;

public class EmployeeRepository(HotelManagementDbContext context)
    : IEmployeeRepository
{
    public Task<List<Employee>> GetAllAsync(
        long? hotelId,
        CancellationToken cancellationToken)
    {
        var query = context.Employees
            .AsNoTracking()
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(employee =>
                employee.HotelId == hotelId.Value);
        }

        return query
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.EmployeeId)
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetByIdAsync(
        long employeeId,
        CancellationToken cancellationToken)
    {
        return context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                employee => employee.EmployeeId == employeeId,
                cancellationToken);
    }

    public async Task<Employee> CreateAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        try
        {
            context.Employees.Add(employee);
            await context.SaveChangesAsync(cancellationToken);
            return employee;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_employees_document"
            })
        {
            throw new BusinessConflictException(
                "Ya existe un empleado con ese documento.");
        }
    }

    public async Task<Employee> UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        try
        {
            context.Employees.Update(employee);
            await context.SaveChangesAsync(cancellationToken);
            return employee;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_employees_document"
            })
        {
            throw new BusinessConflictException(
                "Ya existe un empleado con ese documento.");
        }
    }
}