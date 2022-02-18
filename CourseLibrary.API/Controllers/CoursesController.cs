using AutoMapper;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors/{authorId:guid}/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public CoursesController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
    {
        _courseLibraryRepository = courseLibraryRepository ??
                                   throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
                  throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    public ActionResult<IEnumerable<CourseDto>> GetCoursesForAuthor(Guid authorId)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();

        var coursesForAuthorFromRepo = _courseLibraryRepository.GetCourses(authorId);
        return Ok(_mapper.Map<IEnumerable<CourseDto>>(coursesForAuthorFromRepo));
    }

    [HttpGet("{courseId:guid}")]
    public ActionResult<CourseDto> GetCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();

        var courseForAuthorFromRepo = _courseLibraryRepository.GetCourse(authorId, courseId);

        if (courseForAuthorFromRepo is null)
            return NotFound();

        return Ok(_mapper.Map<CourseDto>(courseForAuthorFromRepo));
    }
}