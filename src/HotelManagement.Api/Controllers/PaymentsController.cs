using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Payments;
using HotelManagement.Application.UseCases.Payments.Commands;
using HotelManagement.Application.UseCases.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = AppPolicies.Financial)]
public class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(
        [FromBody] CreatePaymentDto input,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var payment = await sender.Send(
            new CreatePaymentCommand(input, userId),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByReservation),
            new { reservationId = input.ReservationId },
            payment);
    }

    [HttpGet("by-reservation/{reservationId:long}")]
    public async Task<ActionResult<List<PaymentDto>>> GetByReservation(
        long reservationId,
        CancellationToken cancellationToken)
    {
        var payments = await sender.Send(
            new GetPaymentsByReservationQuery(reservationId),
            cancellationToken);

        return Ok(payments);
    }
    [HttpGet("summary/by-reservation/{reservationId:long}")]
    public async Task<ActionResult<PaymentSummaryDto>> GetSummary(
        long reservationId,
        CancellationToken cancellationToken)
    {
        var summary = await sender.Send(
            new GetPaymentSummaryQuery(reservationId),
            cancellationToken);

        return Ok(summary);
    }
}