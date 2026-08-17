using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container and configure OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure connection string - prefer ConnectionStrings:DefaultConnection, then CONNECTION_STRING env var,
// otherwise build a sensible default that reads SA_PASSWORD from environment (useful for docker-compose)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
                       ?? $"Server=localhost,1433;User Id=sa;Password={Environment.GetEnvironmentVariable("SA_PASSWORD")};Initial Catalog=ExpenseDb;TrustServerCertificate=True;";

// Register EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Add controllers (we will add controllers later)
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
