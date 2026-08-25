using BusinessModelApp.Core.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IProductRepository
    {
        /// <summary>
        /// Gets a product by its unique identifier.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The product if found; otherwise, null.</returns>
        Task<Product> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of featured products.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A list of featured products.</returns>
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the price of a product.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="newPrice">The new price.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The updated product if successful; otherwise, null.</returns>
        Task<Product> UpdatePriceAsync(int productId, decimal newPrice, CancellationToken cancellationToken = default);
    }
}
