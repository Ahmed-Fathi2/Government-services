using Government.ApplicationServices.Results;
using Government.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Government.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        private readonly IDashboardService dashboardService = dashboardService;

        [HttpGet("overview")]
        [Authorize(Roles = "Admin")]

        public async Task<ActionResult<Overview>> GetOverview()
        {
            var overview = await dashboardService.GetOverviewAsync();

            return Ok(overview.Value());
        }


        [HttpGet("requests")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> GetRequestStatistics()
        {
            var result = await dashboardService.GetRequestStatisticsAsync();

            return Ok(result.Value());
        }


        [HttpGet("requests_Per_Month")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> GetRequestStatisticsPerMonth()
        {
            var result = await dashboardService.GetRequestStatisticsPerMonthAsync();

            return Ok(result.Value());
        }


        [HttpGet("services")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> GetServiceStatistics()
        {
            var result = await dashboardService.GetServiceStatisticsAsync();
            return Ok(result.Value());
        }


        [HttpGet("MostRequestedServices")]
        [Authorize]
        public async Task<IActionResult> GetMostRequestedServices()
        {
            var result = await dashboardService.GetMostRequestedServicesAsync();
            return Ok(result.Value());
        }

    }
}
