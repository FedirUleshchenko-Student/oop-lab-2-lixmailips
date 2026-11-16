using liXMaiLips.Models;

namespace liXMaiLips.Services;

internal static class XmlFilterMatcher
{
    public static bool Matches(XmlRecord record, DynamicSearchParams? parameters)
    {
        if (parameters == null || parameters.IsEmpty())
            return true;

        foreach (var (key, allowedValues) in parameters.SelectedFilters)
        {
            if (!record.Fields.TryGetValue(key, out string? actualValue) ||
                string.IsNullOrWhiteSpace(actualValue))
            {
                return false;
            }

            // If specific values are selected, require an exact case-insensitive match (OR within parameter)
            if (allowedValues != null && allowedValues.Count > 0 && !allowedValues.Contains(actualValue))
            {
                return false;
            }
        }

        return true;
    }
}