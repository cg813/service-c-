using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using AutoMapper;
using System.Threading.Tasks;
using AIQXFileService.Implementation.Services;
using AIQXFileService.Domain.Models;
using AIQXCommon.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace AIQXFileService.Implementation.Controllers
{
    [ApiController]
    [Route("v1")]
    public class FileFetchController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ImageConverter _imageConverter;
        private readonly IMapper _mapper;

        public FileFetchController(IFileService fileService, ImageConverter imageConverter, IMapper mapper)
        {
            _fileService = fileService;
            _imageConverter = imageConverter;
            _mapper = mapper;
        }

        [HttpGet("files")]
        public async Task<ActionResult<DataResponseSchema<IList<FileDto>, DataResponseMeta>>> GetFiles([FromQuery(Name = "refId")] string refId)
        {
            var files = await (String.IsNullOrEmpty(refId)
                ? _fileService.GetAsync()
                : _fileService.GetFilesByRefIdAsync(refId));

            return Ok(_mapper.Map<List<FileDto>>(files));
        }


        // [MediaTokenValidatorGuard(GuardRule.AllowIfId)]
        [HttpGet("files/{id}")]
        public async Task<ActionResult<DataResponseSchema<FileResult, DataResponseMeta>>> GetFile(Guid id, string disposition = "attachment")
        {
            var file = await _fileService.GetByIdAsync(id);
            var stream = _fileService.GetFileStream(file);
            // TODO check header for content disposition
            return disposition == "attachment"
                ? File(stream, file.ContentType, file.Name)
                : File(stream, file.ContentType);
        }

        // [MediaTokenValidatorGuard(GuardRule.AllowIfId)]
        [HttpGet("thumbnail/{id}")]
        [SwaggerResponse(200, type: typeof(byte[]))]
        [Produces("image/png")]
        public async Task<IActionResult> GetImageThumbnail(Guid id, string bg = null, string fit = null, int? w = null,
            int? h = null)
        {
            var file = await _fileService.GetByIdAsync(id);
            var stream = await _fileService.GetImageStreamByIdAsync(id);
            var alteredThumbnail = _imageConverter.GenerateThumbnailIfRequested(stream, file.ContentType, bg, fit, w, h);
            return File(alteredThumbnail, "image/png", true);
        }
    }
}