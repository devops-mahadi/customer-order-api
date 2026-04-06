using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Presentation.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrder.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>
    /// Get order by order number
    /// </summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrderNumber(string orderNumber)
    {
        var order = await orderService.GetByOrderNumberAsync(orderNumber);
        if (order == null)
        {
            return NotFound(new { message = $"Order '{orderNumber}' not found" });
        }

        return Ok(order);
    }

    /// <summary>
    /// Get orders by customer email
    /// </summary>
    [HttpGet("customer/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomerEmail(string email)
    {
        var orders = await orderService.GetByCustomerEmailAsync(email);
        return Ok(orders);
    }

    /// <summary>
    /// Get filtered orders with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < ApplicationConstants.Pagination.MinPageNumber)
        {
            return BadRequest(new { message = "Page number must be greater than 0" });
        }

        var orders = await orderService.GetFilteredAsync(startDate, endDate, status, pageNumber, pageSize);
        return Ok(orders);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var orderNumber = await orderService.CreateAsync(request);
            //CreatedAtAction(nameof(GetByOrderNumber), new { orderNumber = order.OrderNumber }, order);
            return Ok(orderNumber);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    [HttpPut("{orderNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string orderNumber, [FromBody] UpdateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await orderService.UpdateAsync(orderNumber, request);
        if (!success)
        {
            return NotFound(new { message = $"Order '{orderNumber}' not found" });
        }

        return Ok(new { message = "Order updated successfully", orderNumber });
    }
}
