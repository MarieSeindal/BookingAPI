using BookingAPI;
using MySql.Data.MySqlClient;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

const string connectionString = "server=aws.connect.psdb.cloud;user=aeelnb98ugixclyawi59;database=booking;port=3306;password=pscale_pw_GBAam9l0UgLuJdsWVQmri0vydFozUAb67Oxkw0XjWpj;SslMode=VerifyFull";
MySqlConnection conn = new MySqlConnection(connectionString);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(x =>
 x.AllowAnyHeader()
 .AllowAnyMethod()
 .AllowCredentials()
 .WithOrigins(
 new[] { "http://localhost:4200" }));

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () => // Minimal api <-- HUSK DET!!!
{

    /**/
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
    
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/person1", () => 
{
    conn.Open();

    string query = "select * from Persons";
    MySqlCommand cmd = new MySqlCommand(query, conn);
    MySqlDataReader reader = cmd.ExecuteReader();

    var person = new Person();

    while (reader.Read())
    {
        var test1 = reader["PersonID"];
        var test2 = reader["LastName"];
        var test3 = reader["FirstName"];

        var castIDK = (int)test1;

        person.Id = (int)test1;
        person.LName = test2.ToString() ?? "No last name";
        person.FName = test3.ToString() ?? "No first name";

    }
    conn.Close();

    return (person);

})
.WithName("GetPerson1")
.WithOpenApi();

app.MapGet("/today", () =>
{
    var today = new Today();
    return today.dateToday();
})
.WithName("GetTodayDate")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public struct Person{
    public Person(int id, string lName, string fName)
    {
        Id = id;
        LName = lName;
        FName = fName;
    }

    public int Id { get; set;}
    public string LName { get; set;}
    public string FName { get; set;}

}
