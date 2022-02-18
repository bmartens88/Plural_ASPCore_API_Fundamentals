using AutoMapper;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public AuthorsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
    {
        _courseLibraryRepository =
            courseLibraryRepository ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [HttpHead]
    public ActionResult<IEnumerable<AuthorDto>> GetAuthors([FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {
        var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo));
    }

    [HttpGet("{authorId:guid}")]
    public ActionResult<AuthorDto> GetAuthor(Guid authorId)
    {
        var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

        if (authorFromRepo is null)
            return NotFound();

        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }
}