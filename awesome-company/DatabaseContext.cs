using awesome_company.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Data.Entity.Core.Objects;
using System.Linq;

namespace awesome_company;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(builder =>
        {
            //Set the table
            builder.ToTable("Companies");

            //create the relationship
            builder
            .HasMany(company => company.Employees)
            .WithOne()
            .HasForeignKey(employee => employee.companyId)
            .IsRequired();

            //Add data
            builder.HasData(new Company { Id = 1, Name = "Awesome Company" });
        });

        modelBuilder.Entity<Employee>(builder =>
        {
            builder.ToTable("Employees");

            var employees = Enumerable.Range(1, 1000)
            .Select(id => new Employee
            {
                Id = id,
                Name = $"Employee #{id}",
                Salary = 100.0m,
                Age = 35,
                companyId = 1,
            })
            .ToList();

            builder.HasData(employees);

        });
    }

    //Regular EF query
    public Company? GetCompanyById(int id)
    {
        return Set<Company>().FirstOrDefault(c => c.Id == id);
    }

    //Regular EF query No Tracking
    public Company? GetCompanyByIdNoTracking(int id)
    {
        return Set<Company>().AsNoTracking().FirstOrDefault(c => c.Id == id);
    }

    //Regular EF query Async
    public async Task<Employee?> GetEmployeeByNameAndAgeAsync(string name, int age)
    {
        return await Set<Employee>().FirstOrDefaultAsync(c => c.Name == name && c.Age == age);
    }




    //Calling the compiled query EF query
    public Company? GetCompanyByIdCompiled(int id)
    {
        return GetById(this, id);
    }

    //Calling the compiled query EF query
    public Company? GetCompanyByIdCompiledNoTracking(int id)
    {
        return GetByIdNoTracking(this, id);
    }

    //Calling the compiled query EF query
    public async Task<Employee?> GetEmployeeByNameAndAgeCompiledAsync(string name, int age)
    {
        return await GetEmployeeByNameAndAge(this, name, age);
    }

    //CompiledQuery
    private static readonly Func<DatabaseContext, int, Company?> GetById = EF.CompileQuery(
            (DatabaseContext context, int id) =>
            context.Set<Company>().FirstOrDefault(c => c.Id == id));


    private static readonly Func<DatabaseContext, int, Company?> GetByIdNoTracking = EF.CompileQuery(
            (DatabaseContext context, int id) =>
            context.Set<Company>().AsNoTracking().FirstOrDefault(c => c.Id == id));

    private static readonly Func<DatabaseContext, string, int, Task<Employee?>> GetEmployeeByNameAndAge = EF.CompileAsyncQuery(
            (DatabaseContext context,string name, int age) =>
            context.Set<Employee>().FirstOrDefault(c => c.Name == name && c.Age == age));

}


