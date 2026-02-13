using MediatR;
using Microsoft.AspNetCore.Mvc;
using NuReaper.Application.Commands.ScanPackage;

namespace NuReaper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackageScanController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost]                    // POST /api/package-scan
        public async Task<IActionResult> ScanPackage(ScanPackageCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
