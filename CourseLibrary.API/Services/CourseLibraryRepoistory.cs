using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;

namespace CourseLibrary.API.Services;

public class CourseLibraryRepoistory : ICourseLibraryRepository, IDisposable
{
    private readonly CourseLibraryContext _context;
    private readonly IPropertyMappingService _propertyMappingService;

    public CourseLibraryRepoistory(CourseLibraryContext context,
        IPropertyMappingService propertyMappingService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _propertyMappingService = propertyMappingService
                                  ?? throw new ArgumentNullException(nameof(propertyMappingService));
    }

    public IEnumerable<Course> GetCourses(Guid authorId)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentNullException(nameof(authorId));

        return _context.Courses
            .Where(c => c.AuthorId == authorId)
            .OrderBy(c => c.Title).ToList();
    }

    public Course GetCourse(Guid authorId, Guid courseId)
    {
        if (authorId == Guid.Empty) throw new ArgumentNullException(nameof(authorId));

        if (courseId == Guid.Empty)
            throw new ArgumentNullException(nameof(courseId));

        return _context.Courses
            .Where(c => c.AuthorId == authorId && c.Id == courseId).FirstOrDefault();
    }

    public void AddCourse(Guid authorId, Course course)
    {
        if (authorId == Guid.Empty) throw new ArgumentNullException(nameof(authorId));

        if (course is null) throw new ArgumentNullException(nameof(course));

        course.AuthorId = authorId;
        _context.Courses.Add(course);
    }

    public void UpdateCourse(Course course)
    {
        // No implementation
    }

    public void DeleteCourse(Course course)
    {
        _context.Courses.Remove(course);
    }

    public IEnumerable<Author> GetAuthors()
    {
        return _context.Authors.ToList();
    }

    public Author GetAuthor(Guid authorId)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentNullException(nameof(authorId));

        return _context.Authors.FirstOrDefault(a => a.Id == authorId);
    }

    public PagedList<Author> GetAuthors(AuthorsResourceParameters authorsResourceParameters)
    {
        if (authorsResourceParameters is null) throw new ArgumentNullException(nameof(authorsResourceParameters));

        var collection = _context.Authors as IQueryable<Author>;

        if (!string.IsNullOrWhiteSpace(authorsResourceParameters.MainCategory))
        {
            var mainCategory = authorsResourceParameters.MainCategory.Trim();
            collection = collection.Where(a => a.MainCategory == mainCategory);
        }

        if (!string.IsNullOrWhiteSpace(authorsResourceParameters.SearchQuery))
        {
            var searchQuery = authorsResourceParameters.SearchQuery.Trim();
            collection = collection.Where(a => a.MainCategory.Contains(searchQuery)
                                               || a.FirstName.Contains(searchQuery)
                                               || a.LastName.Contains(searchQuery));
        }

        if (!string.IsNullOrWhiteSpace(authorsResourceParameters.OrderBy))
        {
            // Get property mapping dictionary
            var authorPropertyMappingDictionary =
                _propertyMappingService.GetPropertyMapping<AuthorDto, Author>();

            collection = collection.ApplySort(authorsResourceParameters.OrderBy, authorPropertyMappingDictionary);
        }

        return PagedList<Author>.Create(collection,
            authorsResourceParameters.PageNumber,
            authorsResourceParameters.PageSize);
    }

    public IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds)
    {
        if (authorIds is null)
            throw new ArgumentNullException(nameof(authorIds));

        return _context.Authors.Where(a => authorIds.Contains(a.Id))
            .OrderBy(a => a.FirstName)
            .OrderBy(a => a.LastName)
            .ToList();
    }

    public void AddAuthor(Author author)
    {
        if (author is null)
            throw new ArgumentNullException(nameof(author));

        author.Id = Guid.NewGuid();

        foreach (var course in author.Courses) course.Id = Guid.NewGuid();

        _context.Authors.Add(author);
    }

    public void DeleteAuthor(Author author)
    {
        if (author is null)
            throw new ArgumentNullException(nameof(author));

        _context.Authors.Remove(author);
    }

    public void UpdateAuthor(Author author)
    {
        throw new NotImplementedException();
    }

    public bool AuthorExists(Guid authorId)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentNullException(nameof(authorId));

        return _context.Authors.Any(a => a.Id == authorId);
    }

    public bool Save()
    {
        return _context.SaveChanges() >= 0;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
    }
}