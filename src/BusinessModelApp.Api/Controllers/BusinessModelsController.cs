using Microsoft.AspNetCore.Mvc;
using BusinessModelApp.Core.Interfaces;

using BusinessModelApp.Core.Dtos;
using BusinessModelApp.Core.DTOs.Revenue;
using BusinessModelApp.Core.DTOs.Expense;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessModelsController : ControllerBase
    {
        private readonly IBusinessModelRepository _businessModelRepository;

        public BusinessModelsController(IBusinessModelRepository businessModelRepository)
        {
            _businessModelRepository = businessModelRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusinessModelDto>>> GetBusinessModels()
        {
            var businessModels = await _businessModelRepository.GetAllAsync();
            return Ok(businessModels);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BusinessModelDto>> GetBusinessModel(Guid id)
        {
            var businessModel = await _businessModelRepository.GetByIdAsync(id);
            if (businessModel == null)
            {
                return NotFound();
            }
            return Ok(businessModel);
        }

        [HttpPost]
        public async Task<ActionResult<BusinessModelDto>> CreateBusinessModel(BusinessModelDto businessModelDto)
        {
            var businessModel = await _businessModelRepository.AddAsync(businessModelDto);
            return CreatedAtAction(nameof(GetBusinessModel), new { id = businessModel.Id }, businessModel);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBusinessModel(Guid id, BusinessModelDto businessModelDto)
        {
            if (id != businessModelDto.Id)
            {
                return BadRequest();
            }

            var businessModel = await _businessModelRepository.UpdateAsync(businessModelDto);
            return Ok(businessModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusinessModel(Guid id)
        {
            await _businessModelRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("{id}/revenues")]
        public async Task<ActionResult<IEnumerable<RevenueSourceDto>>> GetRevenueSources(Guid id)
        {
            var businessModel = await _businessModelRepository.GetByIdAsync(id);
            if (businessModel == null)
            {
                return NotFound();
            }
            return Ok(businessModel.RevenueSources);
        }

        [HttpGet("{id}/expenses")]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(Guid id)
        {
            var businessModel = await _businessModelRepository.GetByIdAsync(id);
            if (businessModel == null)
            {
                return NotFound();
            }
            return Ok(businessModel.Expenses);
        }
    }
}
