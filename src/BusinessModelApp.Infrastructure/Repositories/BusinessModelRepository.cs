using BusinessModelApp.Infrastructure.Data.Models;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public class BusinessModelRepository : IBusinessModelRepository
    {
        private readonly AppDbContext _context;

        public BusinessModelRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BusinessModel> GetBusinessModelByIdAsync(Guid id)
        {
            return await _context.BusinessModels
                .Include(bm => bm.RevenueSources)
                .Include(bm => bm.Expenses)
                .FirstOrDefaultAsync(bm => bm.Id == id);
        }

        public async Task<IEnumerable<BusinessModel>> GetAllBusinessModelsAsync()
        {
            return await _context.BusinessModels
                .Include(bm => bm.RevenueSources)
                .Include(bm => bm.Expenses)
                .ToListAsync();
        }

        public async Task AddBusinessModelAsync(BusinessModel model)
        {
            await _context.BusinessModels.AddAsync(model);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBusinessModelAsync(BusinessModel model)
        {
            _context.BusinessModels.Update(model);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBusinessModelAsync(Guid id)
        {
            var model = await _context.BusinessModels.FindAsync(id);
            if (model != null)
            {
                _context.BusinessModels.Remove(model);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<RevenueSource>> GetRevenueSourcesForModelAsync(Guid modelId)
        {
            return await _context.RevenueSources
                .Where(rs => rs.BusinessModelId == modelId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Expense>> GetExpensesForModelAsync(Guid modelId)
        {
            return await _context.Expenses
                .Where(e => e.BusinessModelId == modelId)
                .ToListAsync();
        }
    }
}
