using Microsoft.EntityFrameworkCore;
using TodoApi;
using TodoApi.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins(
                "http://localhost:3000",
                "https://todolist-frontend-km9z.onrender.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials() 
    );
});


builder.Services.AddDbContext<ToDoDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("ToDoDB"),
            new MySqlServerVersion(new Version(8, 0, 0))
    );
});
var jwtKey = builder.Configuration["JwtSettings:Key"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");
app.Run();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapMethods("/login", new[] { "OPTIONS" }, () => Results.Ok())
   .WithName("PreflightLogin");

app.MapGet("/", () => "Welcome to the ToDo API!");


var items = app.MapGroup("/items").RequireAuthorization();

items.MapGet("/", async (ToDoDbContext db) =>
{
    var items = await db.Items.ToListAsync();
    return Results.Ok(items);
});

items.MapPost("/", async (ToDoDbContext db, Item newItem) =>
{
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
});
items.MapPut("/{id}", async (int id, ToDoDbContext db, TaskUpdateDto update) =>
{
    var item = db.Items.FirstOrDefault(app => app.Id == id);
    if (item == null)
        return Results.NotFound();
    item.IsComplete = update.IsComplete;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

items.MapDelete("/{id}", async (ToDoDbContext db, int id) =>
{
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});


app.MapPost("/login", async (ToDoDbContext db, Users login) =>
{
    var isUserExist = await db.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == login.Password);
    if (isUserExist != null)
    {
        var claims = new[]
            {
            new Claim(ClaimTypes.Name, login.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "yourIssuer",
            audience: "yourAudience",
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    return Results.Unauthorized();
}) .RequireCors("AllowFrontend");


app.MapPost("/register", async (ToDoDbContext db, Users newUser) =>
{
    if (await db.Users.AnyAsync(u => u.Username == newUser.Username))
        return Results.BadRequest(new { message = "Username already exists" });

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "User registered successfully" });
});

app.Run();

