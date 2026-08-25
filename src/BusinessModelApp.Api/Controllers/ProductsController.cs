using BusinessModelApp.Core.Domain;
using BusinessModelApp.Core.Services;
// using BusinessModelApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ILogger<ProductsController> logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The product if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProduct(
            int id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting product with ID {ProductId}", id);
            
            var product = await _productService.GetProductAsync(id, cancellationToken);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(product));
        }

        /// <summary>
        /// Gets a list of featured products.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A list of featured products.</returns>
        [HttpGet("featured")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductDto>))]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetFeaturedProducts(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting featured products");
            
            var products = await _productService.GetFeaturedProductsAsync(cancellationToken);
            return Ok(MapToDtos(products));
        }

        /// <summary>
        /// Updates the price of a product.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <param name="request">The price update request.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The updated product if successful; otherwise, 404 Not Found.</returns>
        [HttpPut("{id}/price")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> UpdateProductPrice(
            int id,
            [FromBody] UpdateProductPriceRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating price for product {ProductId} to {NewPrice}", id, request.NewPrice);
            
            var product = await _productService.UpdateProductPriceAsync(id, request.NewPrice, cancellationToken);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(product));
        }

        /// <summary>
        /// Invalidates the cache for a product.
        /// </summary>
        /// <param name="id">The product ID.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>No content if successful; otherwise, 404 Not Found.</returns>
        [HttpPost("{id}/invalidate-cache")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> InvalidateProductCache(
            int id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Invalidating cache for product {ProductId}", id);
            
            await _productService.InvalidateProductCacheAsync(id, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>Cache statistics.</returns>
        [HttpGet("cache-stats")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CacheStatsDto))]
        public async Task<ActionResult<CacheStatsDto>> GetCacheStats(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cache statistics");
            
            var stats = await _productService.GetCacheStatsAsync(cancellationToken);
            return Ok(new CacheStatsDto
            {
                TotalHits = stats.TotalHits,
                TotalMisses = stats.TotalMisses,
                HitRatio = stats.HitRatio,
                CurrentItemCount = stats.CurrentItemCount,
                TotalSize = stats.TotalSize,
                SizeLimit = stats.SizeLimit,
                SizeUsagePercentage = stats.SizeLimit > 0 ? (double)stats.TotalSize / stats.SizeLimit * 100 : 0
            });
        }

        #region DTOs and Mappings

        public class ProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public bool IsFeatured { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class UpdateProductPriceRequest
        {
            [Required]
            [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
            public decimal NewPrice { get; set; }
        }

        public class CacheStatsDto
        {
            public long TotalHits { get; set; }
            public long TotalMisses { get; set; }
            public double HitRatio { get; set; }
            public long CurrentItemCount { get; set; }
            public long TotalSize { get; set; }
            public long SizeLimit { get; set; }
            public double SizeUsagePercentage { get; set; }
        }

        private static ProductDto MapToDto(Product product)
        {
            if (product == null) return null;

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        private static IEnumerable<ProductDto> MapToDtos(IEnumerable<Product> products)
        {
            if (products == null) yield break;

            foreach (var product in products)
            {
                yield return MapToDto(product);
            }
        }

        #endregion
    }
}
