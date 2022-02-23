using System.Linq.Dynamic.Core;
using CourseLibrary.API.Services;

namespace CourseLibrary.API.Helpers;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> source,
        string orderBy, Dictionary<string, PropertyMappingValue> mappingDictionary)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (mappingDictionary is null)
            throw new ArgumentNullException(nameof(mappingDictionary));

        if (string.IsNullOrWhiteSpace(orderBy))
            return source;

        var orderByString = string.Empty;

        // The orderby string is separated by ",", so we split it
        var orderByAfterSplit = orderBy.Split(',');

        // Apply each orderby clause in reverse order - otherwise, the
        // IQueryable will be ordered in the wrong order
        foreach (var orderByClause in orderByAfterSplit.Reverse())
        {
            // Trim the orderby clause, as it might contain leading
            // or trailing spaces. Can't trim the var in foreach,
            // so use another var
            var trimmedOrderByClause = orderByClause.Trim();

            // If the sort option ends with " desc", we order
            // descending, otherwise ascending
            var orderDescending = trimmedOrderByClause.EndsWith(" desc");

            // Remove " asc" or " desc" from the orderBy clause, so we
            // get the property name to look for in the mapping dictionary
            var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
            var propertyName = indexOfFirstSpace == -1
                ? trimmedOrderByClause
                : trimmedOrderByClause.Remove(indexOfFirstSpace);

            // Find the matching property
            if (!mappingDictionary.ContainsKey(propertyName))
                throw new ArgumentException($"Key mapping for {propertyName} is missing");

            // Get the PropertyMappingValue
            var propertyMappingValue = mappingDictionary[propertyName];

            if (propertyMappingValue is null)
                throw new ArgumentNullException(nameof(propertyMappingValue));

            // Run through the property names
            // So the orderby clauses are applied in the correct order
            foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
            {
                // Revert sort order if necessary
                if (propertyMappingValue.Revert)
                    orderDescending = !orderDescending;

                orderByString = orderByString +
                                (string.IsNullOrWhiteSpace(orderByString)
                                    ? string.Empty
                                    : ", ")
                                + destinationProperty
                                + (orderDescending ? " descending" : " ascending");
            }
        }

        return source.OrderBy(orderByString);
    }
}