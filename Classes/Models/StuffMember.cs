namespace liXMaiLips.Models;

public class StaffMember
{
    public string FullName = "";
    public string Faculty = "";
    public string Department = "";
    public string EducationType = "";
    public string Institution = "";
    public string Dates = "";

    // TODO
    public override string ToString() => $"FULL DATA OF {FullName}";

    public string ToStringShort() => $"{FullName} ({Department})";
}