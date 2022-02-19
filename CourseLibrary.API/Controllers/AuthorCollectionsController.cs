﻿using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorCollectionsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public AuthorCollectionsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
    {
        _courseLibraryRepository = courseLibraryRepository
                                   ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper
                  ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet("({ids})", Name = "GetAuthorCollection")]
    public IActionResult GetAuthorCollection(
        [FromRoute] [ModelBinder(BinderType = typeof(ArrayModelBinder))]
        IEnumerable<Guid> ids)
    {
        if (ids is null)
            return BadRequest();

        var authorEntities = _courseLibraryRepository.GetAuthors(ids);

        if (ids.Count() != authorEntities.Count())
            return NotFound();

        var authorsToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

        return Ok(authorsToReturn);
    }

    [HttpPost]
    public ActionResult<IEnumerable<AuthorDto>> CreateAuthorCollection(
        [FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
    {
        var authorEntities = _mapper.Map<IEnumerable<Author>>(authorCollection);
        foreach (var author in authorEntities) _courseLibraryRepository.AddAuthor(author);

        _courseLibraryRepository.Save();

        var authorCollectionToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

        var idsAsString = string.Join(",", authorCollectionToReturn.Select(a => a.Id));
        return CreatedAtRoute("GetAuthorCollection",
            new {ids = idsAsString},
            authorCollectionToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorOptions()
    {
        Response.Headers.Add("Allow", "GET,OPTIONS,POST");
        return Ok();
    }
}