using MediatR;
using Microsoft.AspNetCore.Mvc;
using NuReaper.Application.Commands.ScanPackage;
using NuReaper.Application.Queries.GetScanResult;

namespace NuReaper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackageScanController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost]                    // POST /api/PackageScan
        public async Task<IActionResult> ScanPackage(ScanPackageCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]                    // GET /api/PackageScan?jobId=<jobId>
        public async Task<IActionResult> GetScanResult([FromQuery] GetScanResultQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Problem(detail: ex.Message);
            }
        }
    }
}
