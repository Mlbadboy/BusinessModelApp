using BusinessModelApp.Core.Domain;
using BusinessModelApp.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IProductService
    {
        Task<Product> GetProductAsync(int productId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(CancellationToken cancellationToken = default);
        Task<Product> UpdateProductPriceAsync(int productId, decimal newPrice, CancellationToken cancellationToken = default);
        Task InvalidateProductCacheAsync(int productId, CancellationToken cancellationToken = default);
        Task<CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default);
    }

    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICacheService _cache;
        private readonly IDistributedLockService _lockService;
        private readonly ILogger<ProductService> _logger;
        private const string ProductCachePrefix = "product:";
        private const string FeaturedProductsCacheKey = "featured:products";
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

        public ProductService(
            IProductRepository productRepository,
            ICacheService cache,
            IDistributedLockService lockService,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Product> GetProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{ProductCachePrefix}{productId}";
            
            try
            {
                // Try to get from cache first
                var product = await _cache.GetAsync<Product>(cacheKey, cancellationToken);
                if (product != null)
                {
                    _logger.LogDebug("Cache hit for product {ProductId}", productId);
                    return product;
                }

                _logger.LogDebug("Cache miss for product {ProductId}, loading from database", productId);
                
                // Use a lock to prevent cache stampede
                await using (var lockHandle = await _lockService.AcquireLockAsync(
                    $"lock:{cacheKey}", 
                    TimeSpan.FromSeconds(30),
                    LockTimeout,
                    cancellationToken: cancellationToken))
                {
                    if (lockHandle == null)
                    {
                        throw new TimeoutException($"Could not acquire lock for product {productId} after {LockTimeout.TotalSeconds} seconds");
                    }

                    // Double-check cache after acquiring lock
                    product = await _cache.GetAsync<Product>(cacheKey, cancellationToken);
                    if (product != null)
                    {
                        _logger.LogDebug("Cache populated by another thread for product {ProductId}", productId);
                        return product;
                    }

                    // Load from database
                    product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                    if (product == null)
                    {
                        return null;
                    }

                    // Cache the result
                    await _cache.SetAsync(
                        cacheKey,
                        product,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = DefaultCacheExpiration,
                            Size = CalculateProductSize(product)
                        },
                        cancellationToken);

                    return product;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to get from cache first
                var products = await _cache.GetAsync<IEnumerable<Product>>(FeaturedProductsCacheKey, cancellationToken);
                if (products != null)
                {
                    return products;
                }

                _logger.LogDebug("Cache miss for featured products, loading from database");
                products = await _productRepository.GetFeaturedProductsAsync(cancellationToken);
                await _cache.SetAsync(
                    FeaturedProductsCacheKey,
                    products,
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(10),
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    },
                    cancellationToken);

                return products ?? Array.Empty<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured products");
                throw;
            }
        }

        public async Task<Product> UpdateProductPriceAsync(int productId, decimal newPrice, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{ProductCachePrefix}{productId}";
            
            try
            {
                await using (var lockHandle = await _lockService.AcquireLockAsync(
                    $"update:{productId}", 
                    TimeSpan.FromSeconds(30),
                    LockTimeout,
                    cancellationToken: cancellationToken))
                {
                    if (lockHandle == null)
                    {
                        throw new TimeoutException($"Could not acquire update lock for product {productId}");
                    }

                    // Update in database
                    var product = await _productRepository.UpdatePriceAsync(productId, newPrice, cancellationToken);
                    if (product == null)
                    {
                        return null;
                    }

                    // Update cache
                    await _cache.SetAsync(
                        cacheKey,
                        product,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = DefaultCacheExpiration,
                            Size = CalculateProductSize(product)
                        },
                        cancellationToken);

                    // Invalidate related caches
                    await InvalidateRelatedCachesAsync(productId, cancellationToken);

                    return product;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product price for {ProductId}", productId);
                throw;
            }
        }

        public async Task InvalidateProductCacheAsync(int productId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{ProductCachePrefix}{productId}";
            
            try
            {
                // Remove the product from cache
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                _logger.LogDebug("Invalidated cache for product {ProductId}", productId);

                // Invalidate related caches
                await InvalidateRelatedCachesAsync(productId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _cache.GetStatsAsync(cancellationToken);
                return new CacheStats
                {
                    TotalHits = stats.TotalCacheHits,
                    TotalMisses = stats.TotalCacheMisses,
                    CurrentItemCount = stats.CurrentEntryCount,
                    HitRatio = stats.HitRatio,
                    TotalSize = stats.CurrentSize,
                    SizeLimit = stats.SizeLimit
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache statistics");
                throw;
            }
        }

        private async Task InvalidateRelatedCachesAsync(int productId, CancellationToken cancellationToken)
        {
            // Invalidate the featured products cache if this product is featured
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product?.IsFeatured == true)
            {
                await _cache.RemoveAsync(FeaturedProductsCacheKey, cancellationToken);
                _logger.LogDebug("Invalidated featured products cache due to update of product {ProductId}", productId);
            }
        }

        private static long CalculateProductSize(Product product)
        {
            // Simple size estimation for the product
            if (product == null) return 0;
            
            long size = sizeof(int) * 2; // Id and CategoryId
            size += (product.Name?.Length * sizeof(char)) ?? 0;
            size += (product.Description?.Length * sizeof(char)) ?? 0;
            size += sizeof(decimal); // Price
            size += sizeof(bool) * 2; // IsFeatured and IsActive
            
            return Math.Max(1, size); // Ensure at least 1 byte
        }
    }

    public class CacheStats
    {
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public long CurrentItemCount { get; set; }
        public double HitRatio { get; set; }
        public long TotalSize { get; set; }
        public long SizeLimit { get; set; }
    }
}
