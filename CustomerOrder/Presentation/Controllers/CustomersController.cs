using System.Text;
using System.Text.Json;
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

    // GDPR Endpoints

    /// <summary>
    /// GDPR Article 20: Export customer data (Right to Data Portability)
    /// </summary>
    [HttpGet("{email}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportData(string email)
    {
        try
        {
            var exportData = await _customerService.ExportDataAsync(email);

            // Return as downloadable JSON file
            var jsonContent = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(jsonContent);
            return File(bytes, "application/json", $"customer-data-{email}-{DateTime.UtcNow:yyyyMMdd}.json");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GDPR Article 17: Anonymize customer (Right to be Forgotten)
    /// Soft delete with data anonymization
    /// </summary>
    [HttpPost("{email}/anonymize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Anonymize(string email, [FromQuery] string reason = "Customer requested deletion")
    {
        try
        {
            var success = await _customerService.AnonymizeAsync(email, reason);
            if (!success)
            {
                return NotFound(new { message = $"Customer with email '{email}' not found" });
            }

            return Ok(new
            {
                message = "Customer data has been anonymized successfully",
                email,
                anonymizedAt = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GDPR Article 7: Grant consent for data processing
    /// </summary>
    [HttpPost("{email}/consents")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantConsent(string email, [FromBody] ConsentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var success = await _customerService.GrantConsentAsync(
                email,
                request.ConsentType,
                ipAddress,
                userAgent,
                request.ConsentVersion);

            if (!success)
            {
                return BadRequest(new { message = "Failed to grant consent" });
            }

            return CreatedAtAction(nameof(GetConsents), new { email }, new
            {
                message = "Consent granted successfully",
                consentType = request.ConsentType,
                grantedAt = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GDPR Article 7: Revoke consent for data processing
    /// </summary>
    [HttpDelete("{email}/consents/{consentType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeConsent(string email, string consentType)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var success = await _customerService.RevokeConsentAsync(email, consentType, ipAddress, userAgent);
            if (!success)
            {
                return NotFound(new { message = $"Consent '{consentType}' not found for customer '{email}'" });
            }

            return Ok(new
            {
                message = "Consent revoked successfully",
                consentType,
                revokedAt = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all consents for a customer
    /// </summary>
    [HttpGet("{email}/consents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsents(string email)
    {
        try
        {
            var consents = await _customerService.GetConsentsAsync(email);
            return Ok(consents);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
