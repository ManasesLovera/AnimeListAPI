using AnimeListAPI.Data;
using AnimeListAPI.DTOs;
using AnimeListAPI.Models;
using AnimeListAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
{
    return Results.Ok("Hello World");
})
.WithOpenApi();

app.MapPost("/api/register", async (UserDto userDto, ApplicationDbContext context) =>
{
    var user = new User
    {
        Username = userDto.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password)
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    return Results.Ok(new { Message = "User registered successfully" });
});

app.MapPost("/api/login", async (UserDto userDto, ApplicationDbContext context, TokenService tokenService) =>
{
    var user = await context.Users.SingleOrDefaultAsync(u => u.Username == userDto.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }
    var token = tokenService.GenerateToken(user);
    return Results.Created($"/api/animes/{user.Id}",token);
});

app.MapGet("/animes", (ApplicationDbContext context) =>
{
    return context.Animes.ToList();
})
    .RequireAuthorization();

app.MapGet("/api/animes/{id:int}", async ([FromRoute] int id, ApplicationDbContext context, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var anime = await context.Animes.SingleOrDefaultAsync(a => a.Id == id);
    if (anime == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(anime);
})
    .WithName("GetAnime")
    .RequireAuthorization();

//app.MapPut();

app.MapDelete("/api/animes/{id:int}", async ([FromRoute] int id, ApplicationDbContext context) =>
{
    var anime = await context.Animes.SingleOrDefaultAsync(a => a.Id == id);
    if (anime == null)
    {
        return Results.NotFound();
    }
    context.Animes.Remove(anime);
    await context.SaveChangesAsync();
    return Results.Ok("Anime deleted");
});

app.MapPost("/animes", async (AnimeDto animeDto, ApplicationDbContext context) =>
{
    var anime = new Anime
    {
        Title = animeDto.Title
    };
    context.Animes.Add(anime);
    await context.SaveChangesAsync();

    return Results.Ok(anime);
})
    .RequireAuthorization();

app.MapPost("/user-anime-list", async (UserAnimeListDto userAnimeListDto, ApplicationDbContext context, ClaimsPrincipal user) =>
{
    var username = user.Identity!.Name;
    var dbUser = await context.Users.SingleOrDefaultAsync(u => u.Username == username);
    if (dbUser == null)
    {
        return Results.Unauthorized();
    }
    var userAnimeList = new UserAnimeList
    {
        UserId = dbUser.Id,
        AnimeId = userAnimeListDto.AnimeId,
        Status = userAnimeListDto.Status
    };
    context.UserAnimeLists.Add(userAnimeList);
    await context.SaveChangesAsync();

    return Results.Ok(userAnimeList);
})
    .RequireAuthorization();

app.Run();
