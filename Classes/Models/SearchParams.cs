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

    // TEMP no
    //public List<string> Dates = new();

    public bool IsEmpty() =>
           !FullName.Any()
        && !Faculty.Any()
        && !Department.Any()
        && !EducationType.Any()
        && !Insitution.Any()
        ;

    public override string ToString()
    {
        return
            (FullName.Count == 0 ? "" : $"Імена = [ {FullName} ];")
            +
            (Faculty.Count == 0 ? "" : $"Факультети = [ {Faculty} ];")
            +
            (Department.Count == 0 ? "" : $"Факультети = [ {Department} ];")
            +
            (EducationType.Count == 0 ? "" : $"Тип освіти = [ {EducationType} ];")
            +
            (Insitution.Count == 0 ? "" : $"Університети = [ {Insitution} ];");
    }
}
