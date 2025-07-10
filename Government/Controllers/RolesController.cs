using Government.ApplicationServices.services;
using Microsoft.AspNetCore.Authorization;
using SurvayBasket.Contracts.Role.cs;
using SurvayBasket.Errors;

namespace SurvayBasket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles="Admin")]

    public class RolesController(IRoleService roleService) : ControllerBase
    {
        private readonly IRoleService roleService = roleService;

        [HttpGet("")]
        public async Task<ActionResult> GetAll()
        {
            var result = await roleService.GetAllAsync();
            return Ok(result.Value());

        }

        [HttpGet("active")]
        public async Task<ActionResult> GetAllActive()
        {
            var result = await roleService.GetAllActiveAsync();
            return Ok(result.Value());

        }

        [HttpGet("{id}")]
        public async Task<ActionResult> Get([FromRoute] string id , CancellationToken cancellationToken)
        {
            var result = await roleService.GetAsync(id , cancellationToken);
            return  result.IsSuccess ?    
                    Ok(result.Value()) :
                    result.ToProblem(statuscode:StatusCodes.Status404NotFound);

        }

        [HttpPost("")]
        public async Task<ActionResult> Add([FromBody] RoleRequest request, CancellationToken cancellationToken)
        {
            var result = await roleService.AddAsync(request, cancellationToken);

            if (result.IsSuccess)
                return CreatedAtAction(nameof(Get), new { id = result.Value()!.Id }, result.Value());

            return (result.Error.Equals(RoleErrors.InvalidPermissions)) 
                  ? result.ToProblem(statuscode: StatusCodes.Status400BadRequest) 
                  : result.ToProblem(statuscode: StatusCodes.Status409Conflict);

        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update( [FromRoute] string id ,  [FromBody] RoleRequest request, CancellationToken cancellationToken)
        {
            var result = await roleService.UpdateAsync(id , request, cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            if (result.Error.Equals(RoleErrors.RoleNotFound))
                return result.ToProblem(statuscode: StatusCodes.Status404NotFound);
            

            return (result.Error.Equals(RoleErrors.InvalidPermissions))
                  ? result.ToProblem(statuscode: StatusCodes.Status400BadRequest)
                  : result.ToProblem(statuscode: StatusCodes.Status409Conflict);

        }

        [HttpPut("{id}/Toggle_Status")]
        public async Task<ActionResult> Toggle([FromRoute] string id, CancellationToken cancellationToken)
        {
            var result = await roleService.ToggleAsync(id, cancellationToken);

            return (result.IsSuccess)
                  ? NoContent()
                  : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }
    }
}
