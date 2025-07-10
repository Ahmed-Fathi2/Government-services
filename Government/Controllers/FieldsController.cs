using Government.ApplicationServices.Fields;
using Government.Contracts.FilesAndFileds;
using Microsoft.AspNetCore.Authorization;

namespace Government.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class FieldsController(IFieldServcie fieldServcie) : ControllerBase
    {
        private readonly IFieldServcie fieldServcie = fieldServcie;

        [HttpGet("Attached/Request/{requestId}")]
        [Authorize]
        public async Task<IActionResult> GetUserRequestFields([FromRoute] int requestId, CancellationToken cancellationToken)
        {

            var result = await fieldServcie.GetUserRequestFileds(requestId, cancellationToken);

            return result.IsSuccess ? Ok(result.Value()) : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }

        
        [HttpPut("Attached/Request/{requestId}")]
        [Authorize(Roles = "MobileUser")]
        public async Task<IActionResult> UpdateUserFields([FromRoute] int requestId, [FromBody] UserFields userFields, CancellationToken cancellationToken)
        {
            var result = await fieldServcie.UpdateUserFieldsAsync(requestId, userFields, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }


        [HttpGet("Required/Service/{serviceId}")]
        [Authorize]
        public async Task<IActionResult> GetServiceFields([FromRoute] int serviceId, CancellationToken cancellationToken)
        {

            var result = await fieldServcie.GetServiceFieldAsync(serviceId, cancellationToken);

            return result.IsSuccess ?
                        Ok(result.Value())
                      : result.ToProblem(statuscode: StatusCodes.Status404NotFound);
        }


        [HttpPut("Required/Servcie/{serviceId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateServiceFields([FromRoute] int serviceId, [FromBody] FieldsTest fieldsTest, CancellationToken cancellationToken)
        {
            var result = await fieldServcie.UpdateFieldsAsync(serviceId, fieldsTest, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }



    }
}
