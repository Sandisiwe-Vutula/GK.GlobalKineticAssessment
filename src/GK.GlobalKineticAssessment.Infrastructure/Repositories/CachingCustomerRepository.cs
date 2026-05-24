using System.Text.Json;
using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GK.GlobalKineticAssessment.Infrastructure.Repositories;

public sealed class CachingCustomerRepository : ICustomerRepository
{
    private readonly ICustomerRepository _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingCustomerRepository> _logger;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    private static string KeyById(Guid id) => $"gk:cust:{id}";
    private static string KeyAll()         => "gk:custs:all";
    private static string KeyPaged(string? f, int p, int s) => $"gk:custs:{f ?? ""}:{p}:{s}";

    private sealed record PagedDto(List<Customer> Items, int TotalCount);

    public CachingCustomerRepository(
        ICustomerRepository inner, 
        IDistributedCache cache, 
        ILogger<CachingCustomerRepository> logger)
    { _inner = inner; 
      _cache = cache; 
      _logger = logger; 
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var raw = await SafeGet(KeyById(id), ct);
        if (raw is not null) return JsonSerializer.Deserialize<Customer>(raw, Opts);
        var c = await _inner.GetByIdAsync(id, ct);
        if (c is not null) await SafeSet(KeyById(id), c, ct);
        return c;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        var raw = await SafeGet(KeyAll(), ct);
        if (raw is not null) return JsonSerializer.Deserialize<List<Customer>>(raw, Opts) ?? [];
        var list = (await _inner.GetAllAsync(ct)).ToList();
        await SafeSet(KeyAll(), list, ct);
        return list;
    }

    public async Task<Customer> AddAsync(Customer entity, CancellationToken ct = default)
    {
        var r = await _inner.AddAsync(entity, ct);
        await SafeRemove(KeyAll(), ct);
        return r;
    }

    public async Task<Customer> UpdateAsync(Customer entity, CancellationToken ct = default)
    {
        var r = await _inner.UpdateAsync(entity, ct);
        await SafeRemove(KeyById(entity.Id), ct);
        await SafeRemove(KeyAll(), ct);
        return r;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _inner.DeleteAsync(id, ct);
        await SafeRemove(KeyById(id), ct);
        await SafeRemove(KeyAll(), ct);
        return r;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _inner.ExistsAsync(id, ct);

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? f, int page, int size, CancellationToken ct = default)
    {
        var raw = await SafeGet(KeyPaged(f, page, size), ct);
        if (raw is not null)
        {
            var dto = JsonSerializer.Deserialize<PagedDto>(raw, Opts);
            if (dto is not null) return (dto.Items, dto.TotalCount);
        }
        var (items, total) = await _inner.GetPagedAsync(f, page, size, ct);
        var itemList = items.ToList();
        await SafeSet(KeyPaged(f, page, size), new PagedDto(itemList, total), ct);
        return (itemList, total);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default) =>
        _inner.EmailExistsAsync(email, excludeId, ct);

    private async Task<string?> SafeGet(string key, CancellationToken ct)
    {
        try   { return await _cache.GetStringAsync(key, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Cache GET failed {Key}", key); return null; }
    }

    private async Task SafeSet<TVal>(string key, TVal val, CancellationToken ct)
    {
        try
        {
            var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(val, Opts), opts, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Cache SET failed {Key}", key); }
    }

    private async Task SafeRemove(string key, CancellationToken ct)
    {
        try   { await _cache.RemoveAsync(key, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Cache REMOVE failed {Key}", key); }
    }
}
