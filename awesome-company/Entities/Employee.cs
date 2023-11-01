namespace awesome_company.Entities;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = String.Empty;
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public int companyId { get; set; }
}
