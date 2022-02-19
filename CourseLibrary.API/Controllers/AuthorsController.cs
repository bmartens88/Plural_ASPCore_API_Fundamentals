using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
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

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    public ActionResult<IEnumerable<AuthorDto>> GetAuthors(
        [FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {
        var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);
        var previousPageLink = authorsFromRepo.HasPrevious
            ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
            : null;
        var nextPageLink = authorsFromRepo.HasNext
            ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
            : null;

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            previousPageLink,
            nextPageLink
        };
        
        Response.Headers.Add("X-Pagination",
            JsonSerializer.Serialize(paginationMetadata));
        
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

    private string CreateAuthorsResourceUri(
        AuthorsResourceParameters authorsResourceParameters,
        ResourceUriType type)
    {
        return type switch
        {
            ResourceUriType.PreviousPage => Url.Link(nameof(GetAuthors), new
            {
                pageNumber = authorsResourceParameters.PageNumber - 1,
                pageSize = authorsResourceParameters.PageSize,
                mainCategory = authorsResourceParameters.MainCategory,
                searchQuery = authorsResourceParameters.SearchQuery
            }),
            ResourceUriType.NextPage => Url.Link(nameof(GetAuthors), new
            {
                pageNumber = authorsResourceParameters.PageNumber + 1,
                pageSize = authorsResourceParameters.PageSize,
                mainCategory = authorsResourceParameters.MainCategory,
                searchQuery = authorsResourceParameters.SearchQuery
            }),
            _ => Url.Link(nameof(GetAuthors), new
            {
                pageNumber = authorsResourceParameters.PageNumber,
                pageSize = authorsResourceParameters.PageSize,
                mainCategory = authorsResourceParameters.MainCategory,
                searchQuery = authorsResourceParameters.SearchQuery
            })
        };
    }
}