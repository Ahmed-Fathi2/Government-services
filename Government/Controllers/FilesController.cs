using Government.ApplicationServices.Files;
using Government.Contracts;
using Government.Contracts.FilesAndFileds;
using Microsoft.AspNetCore.Authorization;

namespace Government.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class FilesController( IFileService fileService) : ControllerBase
    {
     
        private readonly IFileService fileService = fileService;

        [HttpGet("Attached/Request/{requestId}")]
        public async Task<IActionResult> GetUserRequestFiles([FromRoute] int requestId, CancellationToken cancellationToken)
        {

            var result = await fileService.GetAttachedFilesAsync(requestId, cancellationToken);

            return result.IsSuccess ? Ok(result.Value()) : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }


        [HttpPut("Attached/Request/{requestId}")]
        public async Task<IActionResult> UpdateUserFiles([FromRoute] int requestId, [FromForm] UserFiles userFiles, CancellationToken cancellationToken)
        {
            var result = await fileService.UpdateUserFilesAsync(requestId, userFiles, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }


        [HttpGet("Required/Service/{serviceId}")]
       // [AllowAnonymous]
        public async Task<IActionResult> GetServiceFiles([FromRoute] int serviceId, CancellationToken cancellationToken)
        {

            var result = await fileService.GetServiceFilesAsync(serviceId, cancellationToken);

            return result.IsSuccess ?
                        Ok(result.Value())
                      : result.ToProblem(statuscode: StatusCodes.Status404NotFound);
        }


        [HttpPut("Required/Servcie/{serviceId}")]
        public async Task<IActionResult> UpdateServiceFiles([FromRoute] int serviceId, [FromBody] FilesUpdated filesUpdate, CancellationToken cancellationToken)
        {
            var result = await fileService.UpdateFilesAsync(serviceId, filesUpdate, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }


        [HttpGet(" Required/Download/{id}")]
        public async Task<IActionResult> DownloadServiceRequiredFile([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await fileService.DownloadServiceFileAsync(id, cancellationToken);

            return !result.IsSuccess ?
                    result.ToProblem(statuscode: StatusCodes.Status404NotFound) :
                    File(result.Value()!.fileContent, result.Value()!.contentType, result.Value()!.fileName);


        }


        [HttpGet("Attached/Download/{id}")]
        public async Task<IActionResult> DownloadUserAttachedFile([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await fileService.DownloadAttachedFileAsync(id, cancellationToken);

            return !result.IsSuccess ?
                    result.ToProblem(statuscode: StatusCodes.Status404NotFound) :
                    File(result.Value()!.fileContent, result.Value()!.contentType, result.Value()!.fileName);


        }

        [HttpGet("Image/Service/{serviceId}")]
       // [AllowAnonymous]
        public async Task<IActionResult> GetServiceImage([FromRoute] int serviceId, CancellationToken cancellationToken)
        {

            var result = await fileService.GetServiceImageAsync(serviceId, cancellationToken);

            return result.IsSuccess ?
                        Ok(result.Value())
                      : result.ToProblem(statuscode: StatusCodes.Status404NotFound);
        }

        [HttpGet(" Image/Download/{id}")]
        public async Task<IActionResult> DownloadServiceImage([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await fileService.DownloadServiceImageAsync(id, cancellationToken);

            return !result.IsSuccess ?
                    result.ToProblem(statuscode: StatusCodes.Status404NotFound) :
                    File(result.Value()!.fileContent, result.Value()!.contentType, result.Value()!.fileName);


        }



        [HttpPut("Image/Servcie/{serviceId}")]
        public async Task<IActionResult> UpdateServiceImage([FromRoute] int serviceId, [FromForm] NewImage image, CancellationToken cancellationToken)
        {
            var result = await fileService.UpdateImageAsync(serviceId, image, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem(statuscode: StatusCodes.Status404NotFound);

        }

    }
}
