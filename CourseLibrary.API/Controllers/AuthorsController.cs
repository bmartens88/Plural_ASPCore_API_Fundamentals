using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.ActionConstraints;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Net.Http.Headers;

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

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages
        };

        Response.Headers.Add("X-Pagination",
            JsonSerializer.Serialize(paginationMetadata));

        var links = CreateLinksForAuthors(authorsResourceParameters,
            authorsFromRepo.HasNext,
            authorsFromRepo.HasPrevious);

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object>;
            var authorLinks = CreateLinksForAuthor((Guid) authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links
        };

        return Ok(linkedCollectionResource);
    }

    [Produces("application/json",
        "application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.full+json",
        "application/vnd.marvin.author.full.hateoas+json",
        "application/vnd.marvin.author.friendly+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    [HttpGet("{authorId:guid}", Name = "GetAuthor")]
    public IActionResult GetAuthor(Guid authorId, string fields,
        [FromHeader(Name = "Accept")] string mediaType)
    {
        if (!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
            return BadRequest();
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
            return BadRequest();
        var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

        if (authorFromRepo is null)
            return NotFound();

        var includeLinks = parsedMediaType.SubTypeWithoutSuffix
            .EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);

        IEnumerable<LinkDto> links = new List<LinkDto>();

        if (includeLinks)
            links = CreateLinksForAuthor(authorId, fields);

        var primaryMediaType = includeLinks
            ? parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)
            : parsedMediaType.SubTypeWithoutSuffix;

        // Full Author
        if (primaryMediaType == "vnd.marvin.author.full")
        {
            var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
                .ShapeData(fields) as IDictionary<string, object>;

            if (includeLinks)
                fullResourceToReturn.Add(nameof(links), links);

            return Ok(fullResourceToReturn);
        }

        // Friendly Author
        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object>;

        if (includeLinks)
            friendlyResourceToReturn.Add(nameof(links), links);

        return Ok(friendlyResourceToReturn);
    }

    [RequestHeaderMatchesMediaType("Content-Type",
        "application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    public IActionResult CreateAuthorWithDateOfDeath([FromBody] AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);
        _courseLibraryRepository.AddAuthor(authorEntity);
        _courseLibraryRepository.Save();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        var linkedResourceToReturn = authorToReturn.ShapeData(null)
            as IDictionary<string, object>;
        linkedResourceToReturn.Add(nameof(links), links);

        return CreatedAtRoute(nameof(GetAuthor),
            new {authorId = linkedResourceToReturn["Id"]},
            linkedResourceToReturn);
    }

    [RequestHeaderMatchesMediaType("Content-Type",
        "application/json",
        "application/vnd.marvin.authorforcreation+json")]
    [Consumes("application/json", "application/vnd.marvin.authorforcreation+json")]
    [HttpPost(Name = "CreateAuthor")]
    public ActionResult<AuthorDto> CreateAuthor([FromBody] AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);
        _courseLibraryRepository.AddAuthor(authorEntity);
        _courseLibraryRepository.Save();
        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        var linkedResourceToReturn = authorToReturn.ShapeData(null)
            as IDictionary<string, object>;
        linkedResourceToReturn.Add(nameof(links), links);

        return CreatedAtRoute(nameof(GetAuthor),
            new {authorId = linkedResourceToReturn["Id"]}, linkedResourceToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorOptions()
    {
        Response.Headers.Add("Allow", "GET,OPTIONS,POST");
        return Ok();
    }

    [HttpDelete("{authorId:guid}", Name = "DeleteAuthor")]
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
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link(nameof(GetAuthors), new
                {
                    fields = authorsResourceParameters.Fields,
                    orderBy = authorsResourceParameters.OrderBy,
                    pageNumber = authorsResourceParameters.PageNumber - 1,
                    pageSize = authorsResourceParameters.PageSize,
                    mainCategory = authorsResourceParameters.MainCategory,
                    searchQuery = authorsResourceParameters.SearchQuery
                });
            case ResourceUriType.NextPage:
                return Url.Link(nameof(GetAuthors), new
                {
                    fields = authorsResourceParameters.Fields,
                    orderBy = authorsResourceParameters.OrderBy,
                    pageNumber = authorsResourceParameters.PageNumber + 1,
                    pageSize = authorsResourceParameters.PageSize,
                    mainCategory = authorsResourceParameters.MainCategory,
                    searchQuery = authorsResourceParameters.SearchQuery
                });
            case ResourceUriType.Current:
            default:
                return Url.Link(nameof(GetAuthors), new
                {
                    fields = authorsResourceParameters.Fields,
                    orderBy = authorsResourceParameters.OrderBy,
                    pageNumber = authorsResourceParameters.PageNumber,
                    pageSize = authorsResourceParameters.PageSize,
                    mainCategory = authorsResourceParameters.MainCategory,
                    searchQuery = authorsResourceParameters.SearchQuery
                });
        }

        ;
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string fields)
    {
        var links = new List<LinkDto>();

        if (string.IsNullOrWhiteSpace(fields))
            links.Add(
                new LinkDto(Url.Link(nameof(GetAuthor), new {authorId}), "self", "GET"));
        else
            links.Add(
                new LinkDto(Url.Link(nameof(GetAuthor), new {authorId, fields}), "self", "GET"));
        links.Add(
            new LinkDto(Url.Link(nameof(DeleteAuthor), new {authorId}), "delete_author", "DELETE"));
        links.Add(
            new LinkDto(Url.Link(nameof(CoursesController.CreateCourseForAuthor), new {authorId}),
                "create_course_for_author", "POST"));
        links.Add(
            new LinkDto(Url.Link(nameof(CoursesController.GetCoursesForAuthor), new {authorId}), "courses", "GET"));

        return links;
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters,
        bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>();

        // Self
        links.Add(
            new LinkDto(CreateAuthorsResourceUri(
                    authorsResourceParameters, ResourceUriType.Current),
                "self", "GET"));

        if (hasNext)
            links.Add(
                new LinkDto(CreateAuthorsResourceUri(
                        authorsResourceParameters, ResourceUriType.NextPage),
                    "nextPage", "GET"));
        if (hasPrevious)
            links.Add(
                new LinkDto(CreateAuthorsResourceUri(
                        authorsResourceParameters, ResourceUriType.PreviousPage),
                    "previousPage", "GET"));

        return links;
    }
}