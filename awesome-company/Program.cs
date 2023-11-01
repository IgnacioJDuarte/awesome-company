using awesome_company;
using awesome_company.Entities;
using awesome_company.Models;
using awesome_company.Options;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddDbContext<DatabaseContext>(
//o => o.UseSqlServer(builder.Configuration.GetConnectionString("DBConnectionString")));
//builder.Services.AddDbContext<DatabaseContext>(
//    dbContextOptionsBuilder =>
//    {
//        var connectionString = builder.Configuration.GetConnectionString("DBConnectionString");

//        dbContextOptionsBuilder.UseSqlServer(connectionString, sqlServerAction => 
//        {
//            sqlServerAction.EnableRetryOnFailure(3);
//            sqlServerAction.CommandTimeout(30);
//        });

//        //Only for debug purposes because we are exposing sensitive data
//        dbContextOptionsBuilder.EnableDetailedErrors(true);
//        dbContextOptionsBuilder.EnableSensitiveDataLogging(true);
//    });

builder.Services.ConfigureOptions<DatabaseOptionsSetup>();
builder.Services.AddDbContext<DatabaseContext>(
        (serviceProvider, dbContextOptionsBuilder) =>
        {
            var dataBaseOptions = serviceProvider.GetService<IOptions<DatabaseOptions>>()!.Value;
            var connectionString = builder.Configuration.GetConnectionString("DBConnectionString");

            dbContextOptionsBuilder.UseSqlServer(dataBaseOptions.ConnectionString, sqlServerAction =>
            {
                sqlServerAction.EnableRetryOnFailure(dataBaseOptions.MaxRetryCount);
                sqlServerAction.CommandTimeout(dataBaseOptions.CommandTimeout);
            });

            //Only for debug purposes because we are exposing sensitive data
            dbContextOptionsBuilder.EnableDetailedErrors(dataBaseOptions.EnableDetailedErrors);
            dbContextOptionsBuilder.EnableSensitiveDataLogging(dataBaseOptions.EnableSensitveDataLogging);
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


//This approach will make 1k update queries one per employee
app.MapPut("increase-salaries", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .Include(c => c.Employees)
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found");
    }

    foreach (var employee in company.Employees)
    {
        employee.Salary *= 1.1m;
    }

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

//In this approach we use only one query
app.MapPut("increase-salaries-sql", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found");
    }

    //We must create a Transaction, because InterpolatedSQL impacts in the database before SaveChanges Async
    await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId = {company.Id}");
    company.LastSalaryUpdateUtc = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

//Dapper+EF approach
app.MapPut("increase-salaries-sql-dapper", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found");
    }

    //We must create a Transaction, because InterpolatedSQL impacts in the database before SaveChanges Async
    //set it into a variable to tell dapper to use the same transaction than EF
    var transaction = await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database.GetDbConnection().ExecuteAsync(
        "UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId = @companyId",
        new {CompanyId = company.Id},
        transaction.GetDbTransaction());
    company.LastSalaryUpdateUtc = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

app.MapGet("companies/{companyId:int}", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found");
    }

    var response = new CompanyResponse(company.Id, company.Name);

    return Results.Ok(response);
});


//Fastest delete
app.MapDelete("employees/{employeeId:int}", async (int employeeId, DatabaseContext dbContext) =>
{
    await dbContext.Set<Employee>().Where(p => p.Id == employeeId).ExecuteDeleteAsync();

    return Results.NoContent();
});

app.Run();
