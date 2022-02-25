﻿using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors/{authorId:guid}/[controller]")]
[ResponseCache(CacheProfileName = "240SecondsCacheProfile")]
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

    [HttpGet(Name = "GetCoursesForAuthor")]
    public ActionResult<IEnumerable<CourseDto>> GetCoursesForAuthor(Guid authorId)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();

        var coursesForAuthorFromRepo = _courseLibraryRepository.GetCourses(authorId);
        return Ok(_mapper.Map<IEnumerable<CourseDto>>(coursesForAuthorFromRepo));
    }

    [HttpGet("{courseId:guid}", Name = "GetCourseForAuthor")]
    [ResponseCache(Duration = 120)]
    public ActionResult<CourseDto> GetCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();

        var courseForAuthorFromRepo = _courseLibraryRepository.GetCourse(authorId, courseId);

        if (courseForAuthorFromRepo is null)
            return NotFound();

        return Ok(_mapper.Map<CourseDto>(courseForAuthorFromRepo));
    }

    [HttpPost(Name = "CreateCourseForAuthor")]
    public ActionResult<CourseDto> CreateCourseForAuthor(Guid authorId, [FromBody] CourseForCreationDto course)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId)) return NotFound();

        var courseEntity = _mapper.Map<Course>(course);
        _courseLibraryRepository.AddCourse(authorId, courseEntity);
        _courseLibraryRepository.Save();

        var courseToReturn = _mapper.Map<CourseDto>(courseEntity);
        return CreatedAtRoute(nameof(GetCourseForAuthor),
            new {authorId = authorId, courseId = courseToReturn.Id}, courseToReturn);
    }

    [HttpPut("{courseId:guid}")]
    public IActionResult UpdateCourseForAuthor(Guid authorId, Guid courseId, [FromBody] CourseForUpdateDto course)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();
        var courseForAuthorFromRepo = _courseLibraryRepository.GetCourse(authorId, courseId);
        if (courseForAuthorFromRepo is null)
        {
            var courseToAdd = _mapper.Map<Course>(course);
            courseToAdd.Id = courseId;

            _courseLibraryRepository.AddCourse(authorId, courseToAdd);

            _courseLibraryRepository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);

            return CreatedAtRoute(nameof(GetCourseForAuthor),
                new {authorId = authorId, courseId = courseToReturn.Id}, courseToReturn);
        }

        // map the entity to a CourseForUpdateDto
        // apply the updated field values to the dto
        // map the CourseForUpdateDto back to an entity
        _mapper.Map(course, courseForAuthorFromRepo);

        _courseLibraryRepository.UpdateCourse(courseForAuthorFromRepo);

        _courseLibraryRepository.Save();
        return NoContent();
    }

    [HttpPatch("{courseId:guid}")]
    public ActionResult PartiallyUpdateCourseForAuthor(Guid authorId,
        Guid courseId,
        JsonPatchDocument<CourseForUpdateDto> patchDocument)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();

        var courseForAuthorFromRepo = _courseLibraryRepository.GetCourse(authorId, courseId);

        if (courseForAuthorFromRepo is null)
        {
            var courseDto = new CourseForUpdateDto();
            patchDocument.ApplyTo(courseDto, ModelState);

            if (!TryValidateModel(courseDto))
                return ValidationProblem(ModelState);

            var courseToAdd = _mapper.Map<Course>(courseDto);
            courseToAdd.Id = courseId;

            _courseLibraryRepository.AddCourse(authorId, courseToAdd);
            _courseLibraryRepository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);

            return CreatedAtRoute(nameof(GetCourseForAuthor),
                new {authorId = authorId, courseId = courseToReturn.Id},
                courseToReturn);
        }

        var courseToPatch = _mapper.Map<CourseForUpdateDto>(courseForAuthorFromRepo);
        // Add validation
        patchDocument.ApplyTo(courseToPatch, ModelState);

        if (!TryValidateModel(courseToPatch))
            return ValidationProblem(ModelState);

        _mapper.Map(courseToPatch, courseForAuthorFromRepo);

        _courseLibraryRepository.UpdateCourse(courseForAuthorFromRepo);

        _courseLibraryRepository.Save();

        return NoContent();
    }

    [HttpDelete("{courseId:guid}")]
    public ActionResult DeleteCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!_courseLibraryRepository.AuthorExists(authorId))
            return NotFound();

        var courseForAuthorFromRepo = _courseLibraryRepository.GetCourse(authorId, courseId);

        if (courseForAuthorFromRepo is null)
            return NotFound();

        _courseLibraryRepository.DeleteCourse(courseForAuthorFromRepo);
        _courseLibraryRepository.Save();

        return NoContent();
    }

    // In order to invoke the configured InvalidModelStateResponseFactory (Program.cs)
    public override ActionResult ValidationProblem(ModelStateDictionary modelStateDictionary)
    {
        var options = HttpContext.RequestServices
            .GetRequiredService<IOptions<ApiBehaviorOptions>>();
        return (ActionResult) options.Value.InvalidModelStateResponseFactory(ControllerContext);
    }
}