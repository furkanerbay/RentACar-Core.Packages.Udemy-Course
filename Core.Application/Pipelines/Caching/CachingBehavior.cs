using Core.Application.Pipelines.Transaction;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Application.Pipelines.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICachableRequest //Sen ICacheableRequest oldugunda calıs demek.
{
    private readonly CacheSettings _cacheSettings;//Oncelikle cacheSetting'i okumamız almamız lazım
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    //Biz burada cache işlemlerini yapacagız burada .Net Core'un bir alt yapısı var. Onu kullanacağız. Distribür cache

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger, IConfiguration configuration)
    {
        //CacheSettings() -> CacheSettings nesnesine atmaya yarıyor
        _cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? throw new InvalidOperationException() ;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache) //Eğer ki bypass seçmiş ise
        {
            return await next(); //Direk kodu çalıştır.
        }

        TResponse response;
        byte[]? cachedResponse = await _cache.GetAsync(request.CacheKey, cancellationToken); //Cacheler byte array olarak tutulur. Null olabilir.

        if (cachedResponse!=null) //Null'dan farklı ise cache de var demek. Hiç veri tabanına gitme 
        {
            response = JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cachedResponse)); //Cachede ki datayı da serialize etmemiz gerekir.
            //Byte array olarak tutulmuş datayı al serialize et demek.
            _logger.LogInformation($"Fetched from Cache -> {request.CacheKey}");
        }

        else //Cache de yok ise once cache e eklememız sonra da dondurmemız gerekıyor
        {
            response = await getResponseAndAddToCache(request,next,cancellationToken);
        }

        return response;

    }

    private async Task<TResponse?> getResponseAndAddToCache(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        TResponse response = await next(); //Veritabanına kesin olarak gidecek.

        TimeSpan slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromDays(_cacheSettings.SlidingExpiration); //Sliding Expiration var ise onu ver yoksa appsettingsde verileni geç.
        DistributedCacheEntryOptions cacheOptions = new() { SlidingExpiration = slidingExpiration }; //DistributedCacheEntryOptions'dan cacheOptions'u olusturmamız gerekiyor.
        //Cache'a kayıt edeceız ama opsiyonlar ile.

        byte[] serializedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response)); //Response'u byte array e cevırmem gerekıyor.

        await _cache.SetAsync(request.CacheKey, serializedData, cacheOptions, cancellationToken);
        _logger.LogInformation($"Added to Cache -> {request.CacheKey}");

        if (request.CacheGroupKey != null) //Cache anahatrını cache grubuna da eklememız gerekıyor. cacheGorupAnahtarı varsa null farklı ıse 
            await addCacheKeyToGroup(request, slidingExpiration, cancellationToken); //Onu gruba ekle.

        return response;

    }

    private async Task addCacheKeyToGroup(TRequest request, TimeSpan slidingExpration, CancellationToken cancellationToken)
    {
        byte[]? cacheGroupCache = await _cache.GetAsync(key: request.CacheGroupKey!, cancellationToken);
        HashSet<string> cacheKeysInGroup;
        if (cacheGroupCache != null)
        {
            cacheKeysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cacheGroupCache))!;
            if (!cacheKeysInGroup.Contains(request.CacheKey))
                cacheKeysInGroup.Add(request.CacheKey);
        }
        else
            cacheKeysInGroup = new HashSet<string>(new[] { request.CacheKey });
        byte[] newCacheGroupCache = JsonSerializer.SerializeToUtf8Bytes(cacheKeysInGroup);

        byte[]? cacheGroupCacheSlidingExpirationCache = await _cache.GetAsync(
            key: $"{request.CacheGroupKey}SlidingExpiration",
            cancellationToken
        );
        int? cacheGroupCacheSlidingExpirationValue = null;
        if (cacheGroupCacheSlidingExpirationCache != null)
            cacheGroupCacheSlidingExpirationValue = Convert.ToInt32(Encoding.Default.GetString(cacheGroupCacheSlidingExpirationCache));
        if (cacheGroupCacheSlidingExpirationValue == null || slidingExpration.TotalSeconds > cacheGroupCacheSlidingExpirationValue)
            cacheGroupCacheSlidingExpirationValue = Convert.ToInt32(slidingExpration.TotalSeconds);
        byte[] serializeCachedGroupSlidingExpirationData = JsonSerializer.SerializeToUtf8Bytes(cacheGroupCacheSlidingExpirationValue);

        DistributedCacheEntryOptions cacheOptions =
            new() { SlidingExpiration = TimeSpan.FromSeconds(Convert.ToDouble(cacheGroupCacheSlidingExpirationValue)) };

        await _cache.SetAsync(key: request.CacheGroupKey!, newCacheGroupCache, cacheOptions, cancellationToken);
        _logger.LogInformation($"Added to Cache -> {request.CacheGroupKey}");

        await _cache.SetAsync(
            key: $"{request.CacheGroupKey}SlidingExpiration",
            serializeCachedGroupSlidingExpirationData,
            cacheOptions,
            cancellationToken
        );
        _logger.LogInformation($"Added to Cache -> {request.CacheGroupKey}SlidingExpiration");
    }
}

//Cached data
//CachedGroupKey - cacheKeys[] 
//CacheGroupKey grubunun ıcıne cacheKeys arrayını eklemelıyız.