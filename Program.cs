using Azure.Core;
using BookingAPI;
using BookingAPI.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Diagnostics;
using System.Net;
using System.Xml.Linq;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


const string connectionString = "Server=aws.connect.psdb.cloud;Database=booking;user=4bybcv1yrknj1xi9ycz1;password=pscale_pw_wkMqPf5peopnBjUNgGoa3vXV56yNtLO1g0nSbULLJfm;SslMode=VerifyFull;";
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

// Minimal api <-- HUSK DET!!!

// maybe? https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-7.0
// app.MapGet("/bookings", async (DatabaseContext context) => await context.Bookings.ToListAsync());
// app.MapGet("/", () => "Welcome to minimal APIs");


app.MapPost("/user", async (HttpRequest request) => // Add user
{
    var user = await request.ReadFromJsonAsync<User>(); //sql is executed ok, but it returns null to service. Make a function to generate guid in service?
    // person.Id = userId; I dont need the person object to function, as data is being put intoo insert statement.

    connectToDB();

    var userId = Guid.NewGuid().ToString();

    string insert = "INSERT INTO Users(UserID, LastName, FirstName, Password, IsAdmin) ";
    string query = insert + $"VALUES('{userId}', '{user?.LastName}', '{user?.FirstName}', MD5('{user?.Password}'), {user?.IsAdmin});";
    MySqlCommand cmd = new MySqlCommand(query, conn);

    try { var returnedFromDB = cmd.ExecuteScalar();} 
    catch {
        Debug.WriteLine("Some error in db statement maybe?");
    }

    try { conn.Close(); }
    catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }

    // return Results.Ok("User created"); // !!!!!!!!!!!!!!!!!!!!!!!!! Skal pakke sind logik om det lykkedes eller ej

}).WithName("PostUser").WithOpenApi();

app.MapGet("/user", () => // Get all users
{
    connectToDB();

    string query = "SELECT * FROM Users;";
    MySqlCommand cmd = new MySqlCommand(query, conn);
    List<User> users = new List<User>();

    try
    {
        MySqlDataReader reader = cmd.ExecuteReader();


        while (reader.Read())
        {
            var tempUser = new User("","","","",false);

            tempUser.UserId = reader["UserID"].ToString() ?? "N/A";
            tempUser.LastName = reader["LastName"].ToString() ?? "N/A";
            tempUser.FirstName = reader["FirstName"].ToString() ?? "N/A";
            tempUser.Password = reader["Password"].ToString() ?? "N/A";
            tempUser.IsAdmin = (bool)reader["IsAdmin"]; // Booleans are saved as 0=false and nonZero=true

            users.Add(tempUser);

        }

    }
    catch
    {
        Debug.WriteLine("Some error in sql statement");
    }

    try { conn.Close(); }
    catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }

    return users;


}).WithName("GetUsers").WithOpenApi();


app.MapGet("/user/permission/{userId}", (string userId) => // Get permision for a user
{
    connectToDB();


    string query = $"SELECT IsAdmin FROM Users where UserID = '{userId}';";
    MySqlCommand cmd = new MySqlCommand(query, conn);

    var adminAccess = false;

    try
    {
        MySqlDataReader reader = cmd.ExecuteReader();



        while (reader.Read())
        {
            var temp = reader["IsAdmin"]; // Booleans are saved as 0=false and nonZero=true

            switch (temp)
            {
                case true: adminAccess = true; break;
                case false: adminAccess = false; break;
            }

        }

    }
    catch
    {
        Debug.WriteLine("Some error in sql statement");
    }

    try { conn.Close(); }
    catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }

    return adminAccess;
}).WithName("GetUserPermission").WithOpenApi();



app.MapGet("/bookings/{userId}", (string userId) => // Get all bookings for a user
{
    connectToDB();



    string query = $"SELECT * FROM Bookings where UserID = '{userId}';";
    MySqlCommand cmd = new MySqlCommand(query, conn);
    List<Booking> bookings = new List<Booking>();

    try
    {
        MySqlDataReader reader = cmd.ExecuteReader();



        while (reader.Read())
        {

            var tempBooking = new Booking("", "", "", DateTime.Now, DateTime.Now, false, 0, ""); // just temp data

            tempBooking.Id = reader["BookingID"].ToString() ?? "N/A";
            tempBooking.UserId = reader["UserID"].ToString() ?? "N/A";
            tempBooking.Title = reader["Title"].ToString() ?? "";
            tempBooking.StartDate = (DateTime)reader["StartDate"];
            tempBooking.EndDate = (DateTime)reader["EndDate"];
            tempBooking.AllDay = (bool)reader["AllDay"]; // Booleans are saved as 0=false and nonZero=true
            tempBooking.RoomId = (int)reader["LocationID"];
            tempBooking.Description = reader["Description"].ToString() ?? "";

            bookings.Add(tempBooking);

        }

    }
    catch
    {
        Debug.WriteLine("Some error in sql statement");
    }

    try { conn.Close(); }
    catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }

    return bookings;

}).WithName("GetBookings") .WithOpenApi();


app.MapPost("/booking/{userId}", async (string userId, HttpRequest request) => // Post a booking to a user
{
    var booking = await request.ReadFromJsonAsync<Booking>(); //sql is executed ok, but it returns null to service. Make a function to generate guid in service?
    // person.Id = userId; I dont need the person object to function, as data is being put intoo insert statement.

    connectToDB();


    var startDate = booking?.StartDate.ToString("yyyy-MM-dd HH:mm:00");
    var endDate = booking?.EndDate.ToString("yyyy-MM-dd HH:mm:00");
    var bookingId = Guid.NewGuid().ToString();


    string insert = "INSERT INTO Bookings(BookingID, UserID, Title, StartDate, EndDate, AllDay, LocationID, Description) ";
    string query = insert + $"VALUES('{bookingId}','{userId}','{booking?.Title}','{startDate}','{endDate}',{booking?.AllDay},'{booking?.RoomId}','{booking?.Description}');";
    MySqlCommand cmd = new MySqlCommand(query, conn);

    try
    { 
        var returnedFromDB = cmd.ExecuteScalar();
    }
    catch
    {
        Debug.WriteLine("Some error in sql statement");
    }

    try { conn.Close(); } catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }

}).WithName("PostBooking").WithOpenApi();


app.MapGet("/booking/{bookId}", (int bookId) => // Gets booking with id NOT IN USE 
{
    var booking = new Booking("Book1", "1", "T", DateTime.UtcNow, DateTime.Now, true, 666, "Fun");
    return booking;
    //return "get a bookings";
}).WithName("GetBooking").WithOpenApi();


app.MapDelete("/booking/{bookID}", (string bookId) => // Delete a booking with id
{

    connectToDB();

    string query = $"DELETE from Bookings WHERE BookingID ='{bookId}';";

    MySqlCommand cmd = new MySqlCommand(query, conn);

    try
    {
        var returnedFromDB = cmd.ExecuteScalar();
    }
    catch
    {
        Debug.WriteLine("Some error in sql statement");
    }

    try { conn.Close(); }
    catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }

}).WithName("DeleteBooking").WithOpenApi();

app.MapPut("/booking/{bookingId}", async (string bookingId, HttpRequest request) => // Post a booking to a user
{
    var b = await request.ReadFromJsonAsync<Booking>(); //sql is executed ok, but it returns null to service. Make a function to generate guid in service?
    // person.Id = userId; I dont need the person object to function, as data is being put intoo insert statement.

    connectToDB();


    var startDate = b?.StartDate.ToString("yyyy-MM-dd HH:mm:00");
    var endDate = b?.EndDate.ToString("yyyy-MM-dd HH:mm:00");

    string command = "UPDATE Bookings ";
    string updatedvalues = $"SET Title = '{b?.Title}', StartDate = '{startDate}', EndDate = '{endDate}', AllDay = {b?.AllDay}, LocationID = {b?.RoomId}, Description = '{b?.Description}' ";
    string condition = $"WHERE BookingID = '{bookingId}';";
    string query = command+updatedvalues+condition;

    MySqlCommand cmd = new MySqlCommand(query, conn);

    try
    {
        var returnedFromDB = cmd.ExecuteScalar();
    }
    catch
    {
        Debug.WriteLine("Some error in sql statement");
    }

    try { conn.Close(); }
    catch
    {
        string e = "Could not close. Database error contact administrator";
        Debug.WriteLine(e);
    }


}).WithName("UpdateBooking").WithOpenApi();




#region Random devellopment test

//--------------------------------------//
// - - - - - Test calls below - - - - - //
//--------------------------------------//

app.MapGet("/users/{userId}/books/{bookId}", (int userId, int bookId) => 
    $"The user id is {userId} and book id is {bookId}. Then you cna create and sql insert statement with these parameters {userId} and {bookId}"
);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () => 
{
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

    string query = "select * from Persons where PersonID = 1";
    MySqlCommand cmd = new MySqlCommand(query, conn);
    MySqlDataReader reader = cmd.ExecuteReader();

    var person = new Person(0,"","");

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

#endregion

app.Run();


void connectToDB()
{
    bool canConnect = false;
    var stopCounter = 50;
    while (!canConnect && stopCounter > 0)
    {
        try
        {
            stopCounter--;
            conn.Open();
            Debug.WriteLine("Connected");
            canConnect = true;
        }
        catch (Exception)
        {
            Debug.WriteLine("Could not connect");
            Thread.Sleep(500);
            Debug.WriteLine("Retrying...");
        }
    }
    var exits = " ";
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
