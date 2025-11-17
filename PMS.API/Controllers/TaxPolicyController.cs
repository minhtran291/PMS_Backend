using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Application.DTOs.TaxPolicy;
using PMS.Application.Services.TaxPolicy;
using PMS.Core.Domain.Constant;
using System.Security.Claims;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaxPolicyController : ControllerBase
    {
        private readonly ITaxPolicySerivce _taxPolicySerivce;

        public TaxPolicyController(ITaxPolicySerivce taxPolicySerivce)
        {
            _taxPolicySerivce = taxPolicySerivce;
        }

        [HttpPost, Authorize(Roles = UserRoles.ACCOUNTANT)]
        [Route("create-tax-policy")]
        public async Task<IActionResult> Create(CreateTaxPolicyDTO dto)
        {
            var result = await _taxPolicySerivce.CreateAsync(dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.ACCOUNTANT)]
        [Route("update-tax-policy")]
        public async Task<IActionResult> Update(UpdateTaxPolicyDTO dto)
        {
            var result = await _taxPolicySerivce.UpdateAsync(dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpPost, Authorize(Roles = UserRoles.ACCOUNTANT)]
        [Route("disable-enable-tax-policy")]
        public async Task<IActionResult> DisableEnable(int taxId)
        {
            var result = await _taxPolicySerivce.DisableEnableAsync(taxId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpDelete, Authorize(Roles = UserRoles.ACCOUNTANT)]
        [Route("delete-tax-policy")]
        public async Task<IActionResult> Delete(int taxId)
        {
            var result = await _taxPolicySerivce.DeleteAsync(taxId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.ACCOUNTANT)]
        [Route("tax-policy-list")]
        public async Task<IActionResult> List()
        {
            var result = await _taxPolicySerivce.ListAsync();

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet, Authorize(Roles = UserRoles.ACCOUNTANT)]
        [Route("tax-policy-details")]
        public async Task<IActionResult> Details(int taxId)
        {
            var result = await _taxPolicySerivce.DetailsAsync(taxId);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
    }
}
