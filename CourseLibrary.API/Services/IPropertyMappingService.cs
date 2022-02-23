namespace CourseLibrary.API.Services;

public interface IPropertyMappingService
{
    Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();
    bool ValidMappingExists<TSource, TDestination>(string fields);
}