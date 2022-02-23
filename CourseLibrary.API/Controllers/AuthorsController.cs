using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly IPropertyCheckerService _propertyCheckerService;

    public AuthorsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper,
        IPropertyMappingService propertyMappingService, IPropertyCheckerService propertyCheckerService)
    {
        _courseLibraryRepository =
            courseLibraryRepository ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService
                                  ?? throw new ArgumentNullException(nameof(propertyMappingService));
        _propertyCheckerService = propertyCheckerService
                                  ?? throw new ArgumentNullException(nameof(propertyCheckerService));
    }

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    public IActionResult GetAuthors(
        [FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {
        if (!_propertyMappingService.ValidMappingExists<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            return BadRequest();
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            return BadRequest();
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

        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameters.Fields));
    }

    [HttpGet("{authorId:guid}", Name = "GetAuthor")]
    public IActionResult GetAuthor(Guid authorId, string fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
            return BadRequest();
        var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

        if (authorFromRepo is null)
            return NotFound();

        return Ok(_mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields));
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
                fields = authorsResourceParameters.Fields,
                orderBy = authorsResourceParameters.OrderBy,
                pageNumber = authorsResourceParameters.PageNumber - 1,
                pageSize = authorsResourceParameters.PageSize,
                mainCategory = authorsResourceParameters.MainCategory,
                searchQuery = authorsResourceParameters.SearchQuery
            }),
            ResourceUriType.NextPage => Url.Link(nameof(GetAuthors), new
            {
                fields = authorsResourceParameters.Fields,
                orderBy = authorsResourceParameters.OrderBy,
                pageNumber = authorsResourceParameters.PageNumber + 1,
                pageSize = authorsResourceParameters.PageSize,
                mainCategory = authorsResourceParameters.MainCategory,
                searchQuery = authorsResourceParameters.SearchQuery
            }),
            _ => Url.Link(nameof(GetAuthors), new
            {
                fields = authorsResourceParameters.Fields,
                orderBy = authorsResourceParameters.OrderBy,
                pageNumber = authorsResourceParameters.PageNumber,
                pageSize = authorsResourceParameters.PageSize,
                mainCategory = authorsResourceParameters.MainCategory,
                searchQuery = authorsResourceParameters.SearchQuery
            })
        };
    }
}