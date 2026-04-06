using CustomerOrder.Application.Interfaces;
using CustomerOrder.Presentation.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrder.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    /// <summary>
    /// Get customer by email
    /// </summary>
    [HttpGet("{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEmail(string email)
    {
        var customer = await _customerService.GetByEmailAsync(email);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with email '{email}' not found" });
        }

        return Ok(customer);
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var success = await _customerService.CreateAsync(request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to create customer" });
            }

            var customer = await _customerService.GetByEmailAsync(request.Email);
            return CreatedAtAction(nameof(GetByEmail), new { email = request.Email }, customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    [HttpPut("{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string email, [FromBody] UpdateCustomerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var success = await _customerService.UpdateAsync(email, request);
            if (!success)
            {
                return NotFound(new { message = $"Customer with email '{email}' not found" });
            }

            var customer = await _customerService.GetByEmailAsync(email);
            return Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    [HttpDelete("{email}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string email)
    {
        var success = await _customerService.DeleteAsync(email);
        if (!success)
        {
            return NotFound(new { message = $"Customer with email '{email}' not found" });
        }

        return NoContent();
    }
}
