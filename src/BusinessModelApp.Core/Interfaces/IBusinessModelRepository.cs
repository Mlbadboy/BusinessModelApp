using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Dtos;
// using BusinessModelApp.Core.Entities;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IBusinessModelRepository
    {
        Task<IEnumerable<BusinessModelDto>> GetAllAsync();
        Task<BusinessModelDto> GetByIdAsync(Guid id);
        Task<BusinessModelDto> AddAsync(BusinessModelDto businessModelDto);
        Task<BusinessModelDto> UpdateAsync(BusinessModelDto businessModelDto);
        Task DeleteAsync(Guid id);
    }
}
