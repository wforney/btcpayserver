using BTCPayServer.Data;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Security.Greenfield;

public class APIKeyRepository
{
    private readonly ApplicationDbContextFactory _applicationDbContextFactory;

    public APIKeyRepository(ApplicationDbContextFactory applicationDbContextFactory)
    {
        _applicationDbContextFactory = applicationDbContextFactory;
    }

    public async Task<APIKeyData> GetKey(string apiKey, bool includeUser = false)
    {
        await using ApplicationDbContext context = _applicationDbContextFactory.CreateContext();
        if (includeUser)
        {
            return await context.ApiKeys.Include(data => data.User).SingleOrDefaultAsync(data => data.Id == apiKey && data.Type != APIKeyType.Legacy);
        }
        return await context.ApiKeys.SingleOrDefaultAsync(data => data.Id == apiKey && data.Type != APIKeyType.Legacy);
    }

    public async Task<List<APIKeyData>> GetKeys(APIKeyQuery query)
    {
        using ApplicationDbContext context = _applicationDbContextFactory.CreateContext();
        IQueryable<APIKeyData> queryable = context.ApiKeys.AsQueryable();
        if (query != null)
        {
            if (query.UserId != null && query.UserId.Any())
            {
                queryable = queryable.Where(data => query.UserId.Contains(data.UserId));
            }
        }

        return await queryable.ToListAsync();
    }

    public async Task CreateKey(APIKeyData key)
    {
        if (key.Type == APIKeyType.Legacy || !string.IsNullOrEmpty(key.StoreId) || string.IsNullOrEmpty(key.UserId))
        {
            throw new InvalidOperationException("cannot save a bitpay legacy api key with this repository");
        }

        using ApplicationDbContext context = _applicationDbContextFactory.CreateContext();
        await context.ApiKeys.AddAsync(key);
        await context.SaveChangesAsync();
    }

    public async Task<bool> Remove(string id, string getUserId)
    {
        using (ApplicationDbContext context = _applicationDbContextFactory.CreateContext())
        {
            APIKeyData key = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(context.ApiKeys,
                data => data.Id == id && data.UserId == getUserId);
            if (key == null)
            {
                return false;
            }

            context.ApiKeys.Remove(key);
            await context.SaveChangesAsync();
        }
        return true;
    }

    public class APIKeyQuery
    {
        public string[] UserId { get; set; }
    }
}
