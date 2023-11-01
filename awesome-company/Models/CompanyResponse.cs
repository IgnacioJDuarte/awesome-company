namespace awesome_company.Models;

public class CompanyResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public CompanyResponse(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
