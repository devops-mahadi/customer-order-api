using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrder.Infrastructure.Repositories;

public class ConsentRepository(CustomerOrderDbContext dbContext) : IConsentRepository
{
    public async Task<bool> CreateAsync(CustomerConsent consent)
    {
        dbContext.CustomerConsents.Add(consent);
        int rowCount = await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return rowCount > 0;
    }

    public async Task<bool> UpdateAsync(CustomerConsent consent)
    {
        dbContext.CustomerConsents.Update(consent);
        int rowCount = await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return rowCount > 0;
    }

    public async Task<IEnumerable<CustomerConsent>> GetByCustomerIdAsync(int customerId)
    {
        return await dbContext.CustomerConsents
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.ConsentDate)
            .ToListAsync();
    }

    public async Task<CustomerConsent?> GetByCustomerIdAndTypeAsync(int customerId, string consentType)
    {
        return await dbContext.CustomerConsents
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId && c.ConsentType == consentType)
            .OrderByDescending(c => c.ConsentDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> RevokeConsentAsync(int customerId, string consentType)
    {
        var consent = await dbContext.CustomerConsents
            .Where(c => c.CustomerId == customerId && c.ConsentType == consentType && c.IsGranted)
            .FirstOrDefaultAsync();

        if (consent == null)
        {
            return false;
        }

        consent.IsGranted = false;
        consent.RevokedDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return true;
    }
}
