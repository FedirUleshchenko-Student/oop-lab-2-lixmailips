using liXMaiLips.Models;
using System.Xml.Linq;

namespace liXMaiLips.Strategies;

public class LinqToXmlParseStrategy : IXmlParseStrategy
{
    public List<StaffMember> Search(string xmlContent, SearchParams parameters)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return new List<StaffMember>();

        try
        {
            XDocument doc = XDocument.Parse(xmlContent);

            // Знаходимо всі елементи, які виступають записами (наприклад, StaffMember)
            var query = doc.Descendants()
                .Select(el => new StaffMember
                {
                    // Шукаємо дані як в атрибутах (як на вашій діаграмі), так і в тегах (на випадок іншої структури)
                    FullName = el.Attribute("fullName")?.Value ?? el.Attribute("FullName")?.Value ?? el.Element("FullName")?.Value ?? string.Empty,
                    Faculty = el.Attribute("faculty")?.Value ?? el.Attribute("Faculty")?.Value ?? el.Element("Faculty")?.Value ?? string.Empty,
                    Department = el.Attribute("department")?.Value ?? el.Attribute("Department")?.Value ?? el.Element("Department")?.Value ?? string.Empty,
                    EducationType = el.Attribute("educationType")?.Value ?? el.Attribute("EducationType")?.Value ?? el.Element("EducationType")?.Value ?? string.Empty,
                    Institution = el.Attribute("institution")?.Value ?? el.Attribute("Institution")?.Value ?? el.Element("Institution")?.Value ?? string.Empty,
                    Dates = el.Attribute("dates")?.Value ?? el.Attribute("Dates")?.Value ?? el.Element("Dates")?.Value ?? string.Empty
                })
                // returning only people with existing name (clearing technical tags)
                .Where(s => !string.IsNullOrEmpty(s.FullName));

            // Застосовуємо динамічні параметри пошуку (фільтрацію)
            if (parameters != null && !parameters.IsEmpty())
            {
                if (parameters.Faculty != null && parameters.Faculty.Any())
                {
                    query = query.Where(s => parameters.Faculty.Contains(s.Faculty, StringComparer.OrdinalIgnoreCase));
                }

                if (parameters.FullName != null && parameters.FullName.Any())
                {
                    query = query.Where(s => parameters.FullName.Contains(s.FullName, StringComparer.OrdinalIgnoreCase));
                }
            }

            return query.ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка LINQ парсингу: {ex.Message}");
            return new List<StaffMember>();
        }
    }
}
