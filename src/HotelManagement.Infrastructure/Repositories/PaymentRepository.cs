using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using HotelManagement.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Repositories;

public class PaymentRepository(HotelManagementDbContext context) : IPaymentRepository
{
    public async Task<Payment> CreateAsync(
        long reservationId,
        decimal amount,
        string paymentMethod,
        long userId,
        string? reference,
        string? notes,
        CancellationToken cancellationToken)
    {
        var reservationExists = await context.Reservations
            .AsNoTracking()
            .AnyAsync(
                reservation => reservation.ReservationId == reservationId,
                cancellationToken);

        if (!reservationExists)
        {
            throw new ResourceNotFoundException(
                "La reserva indicada no existe.");
        }

        var user = await context.AppUsers
            .Include(appUser => appUser.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(
                appUser => appUser.UserId == userId,
                cancellationToken);

        if (user is null || !user.Active || user.Employee is null || !user.Employee.Active)
        {
            throw new BusinessValidationException(
                "El usuario que registra el pago no está activo.");
        }

        var payment = new Payment
        {
            ReservationId = reservationId,
            TransactionType = "PAYMENT",
            Amount = amount,
            PaymentMethod = paymentMethod,
            Status = "COMPLETED",
            Reference = string.IsNullOrWhiteSpace(reference)
                ? null
                : reference.Trim(),
            PaidAt = DateTime.UtcNow,
            RecordedByUserId = userId,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim()
        };

        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);

        return payment;
    }

    public Task<List<Payment>> GetByReservationIdAsync(
        long reservationId,
        CancellationToken cancellationToken)
    {
        return context.Payments
            .AsNoTracking()
            .Where(payment => payment.ReservationId == reservationId)
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.PaymentId)
            .ToListAsync(cancellationToken);
    }
}