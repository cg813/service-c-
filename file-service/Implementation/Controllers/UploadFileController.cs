using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AIQXFileService.Implementation.Services;
using AIQXFileService.Domain.Models;
using AIQXCommon.Models;
using AIQXCommon.Middlewares;

namespace AIQXFileService.Implementation.Controllers
{
    [ApiController]
    [Route("v1")]
    public class UploadFileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;

        public UploadFileController(IFileService fileService, IMapper mapper)
        {
            _fileService = fileService;
            _mapper = mapper;
        }

        // [MediaTokenValidatorGuard]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] CreateFileDto request)
        {
            var authInfo = HttpContext.GetAuthorizationOrFail();
            var result = await _fileService.CreateFileFromUploadAsync(request, authInfo.Id);
            return StatusCode(201, _mapper.Map<FileDto>(result));
        }
    }
}