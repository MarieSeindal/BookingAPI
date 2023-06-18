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

#region Initial code

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

MySqlConnection conn = new MySqlConnection(getConnectionString());


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

#endregion

#region User Service

app.MapPost("/user", async (HttpRequest request) => // Add user
{
    var user = await request.ReadFromJsonAsync<UserDTO>(); //sql is executed ok, but it returns null to service. Make a function to generate guid in service?
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

}).WithName("PostUser").WithOpenApi().Accepts<UserDTO>("application/json");

app.MapGet("/user", () => // Get all users
{
    connectToDB();

    string query = "SELECT * FROM Users;";
    MySqlCommand cmd = new MySqlCommand(query, conn);
    List<UserDTO> users = new List<UserDTO>();

    try
    {
        MySqlDataReader reader = cmd.ExecuteReader();


        while (reader.Read())
        {
            var tempUser = new UserDTO("","","","",false);

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

#endregion

#region Booking Service

app.MapPost("/booking/{userId}", async (string userId, HttpRequest request) => // Post a booking to a user
{
    var booking = await request.ReadFromJsonAsync<BookingDTO>(); //sql is executed ok, but it returns null to service. Make a function to generate guid in service?
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

}).WithName("PostBooking").WithOpenApi().Accepts<BookingDTO>("application/json");

app.MapGet("/booking/{userId}", (string userId) => // Get all bookings for a user
{
    TimeZoneInfo est = null;
    try
    {
        est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    } catch { Debug.WriteLine("Time zone not found"); }

    connectToDB();



    string query = $"SELECT * FROM Bookings where UserID = '{userId}';";
    MySqlCommand cmd = new MySqlCommand(query, conn);
    List<BookingDTO> bookings = new List<BookingDTO>();

    try
    {
        MySqlDataReader reader = cmd.ExecuteReader();



        while (reader.Read())
        {

            var tempBooking = new BookingDTO("", "", "", DateTime.Now, DateTime.Now, false, 0, ""); // just temp data

            tempBooking.Id = reader["BookingID"].ToString() ?? "N/A";
            tempBooking.UserId = reader["UserID"].ToString() ?? "N/A";
            tempBooking.Title = reader["Title"].ToString() ?? "";
            tempBooking.StartDate = (DateTime)reader["StartDate"];
            tempBooking.StartDate = tempBooking.StartDate.ToLocalTime();
            tempBooking.EndDate = (DateTime)reader["EndDate"];
            tempBooking.EndDate = tempBooking.EndDate.ToLocalTime();
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

app.MapPut("/booking/{bookingId}", async (string bookingId, HttpRequest request) => // Post a booking to a user
{
    var b = await request.ReadFromJsonAsync<BookingDTO>(); //sql is executed ok, but it returns null to service. Make a function to generate guid in service?
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


}).WithName("UpdateBooking").WithOpenApi().Accepts<BookingDTO>("application/json");

app.MapDelete("/booking/{bookingID}", (string bookingId) => // Delete a booking with id
{

    connectToDB();

    string query = $"DELETE from Bookings WHERE BookingID ='{bookingId}';";

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

string getConnectionString()
{
    try
    {
        StreamReader r = new StreamReader(@"./Connection.txt");

        string connString = r.ReadLine();

        return connString;
    } catch
    {
        Debug.WriteLine("Error in connections tring to db. Potentially missing the file?");
    }

    return "NO CONNECTION STRING";

}

