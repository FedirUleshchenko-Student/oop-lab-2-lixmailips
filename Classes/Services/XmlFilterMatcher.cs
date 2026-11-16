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

            // If specific values are selected, match against any semicolon-separated item (OR within parameter)
            if (allowedValues != null && allowedValues.Count > 0)
            {
                var recordValues = actualValue.Split(
                    "; ",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                bool hasMatch = recordValues.Any(rv =>
                    allowedValues.Any(av => string.Equals(av, rv, StringComparison.OrdinalIgnoreCase)));

                if (!hasMatch)
                    return false;
            }
        }

        return true;
    }
}