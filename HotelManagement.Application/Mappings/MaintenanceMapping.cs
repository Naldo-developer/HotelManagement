using HotelManagement.Application.Dtos.Maintenances;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.Mappings;

internal static class MaintenanceMapping
{
    public static MaintenanceDto ToDto(Maintenance maintenance)
    {
        return new MaintenanceDto
        {
            MaintenanceId = maintenance.MaintenanceId,
            RoomId = maintenance.RoomId,
            StartDate = maintenance.StartDate,
            EndDate = maintenance.EndDate,
            Category = maintenance.Category,
            Description = maintenance.Description,
            Status = maintenance.Status,
            ReportedByUserId = maintenance.ReportedByUserId,
            AssignedEmployeeId = maintenance.AssignedEmployeeId,
            Cost = maintenance.Cost,
            CreatedAt = maintenance.CreatedAt,
            CompletedAt = maintenance.CompletedAt
        };
    }
}