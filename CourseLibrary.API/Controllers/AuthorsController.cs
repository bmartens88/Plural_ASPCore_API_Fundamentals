using AutoMapper;
using CourseLibrary.API.Entities;
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
    public ActionResult<IEnumerable<AuthorDto>> GetAuthors(
        [FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {
        var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo));
    }

    [HttpGet("{authorId:guid}", Name = "GetAuthor")]
    public ActionResult<AuthorDto> GetAuthor(Guid authorId)
    {
        var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

        if (authorFromRepo is null)
            return NotFound();

        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

    [HttpPost]
    public ActionResult<AuthorDto> CreateAuthor([FromBody] AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);
        _courseLibraryRepository.AddAuthor(authorEntity);
        _courseLibraryRepository.Save();
        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
        return CreatedAtRoute(nameof(GetAuthor),
            new {authorId = authorToReturn.Id}, authorToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorOptions()
    {
        Response.Headers.Add("Allow", "GET,OPTIONS,POST");
        return Ok();
    }

    [HttpDelete("{authorId:guid}")]
    public ActionResult DeleteAuthor(Guid authorId)
    {
        var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

        if (authorFromRepo is null)
            return NotFound();

        _courseLibraryRepository.DeleteAuthor(authorFromRepo);
        _courseLibraryRepository.Save();

        return NoContent();
    }
}