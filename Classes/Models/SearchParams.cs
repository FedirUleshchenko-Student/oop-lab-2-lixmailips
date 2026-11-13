using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liXMaiLips.Models;

public class SearchParams
{
    public List<string> FullName = new();
    public List<string> Faculty = new();
    public List<string> Department = new();
    public List<string> EducationType = new();
    public List<string> Insitution = new();

    public bool IsEmpty() =>
           !FullName.Any()
        && !Faculty.Any()
        && !Department.Any()
        && !EducationType.Any()
        && !Insitution.Any()
        ;
}
