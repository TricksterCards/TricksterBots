var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(p => p.AddPolicy("TricksterCards",
    policyBuilder => { policyBuilder.WithOrigins("http://localhost:63677", "https://www.trickstercards.com").AllowAnyMethod().AllowAnyHeader(); }));

var app = builder.Build();

app.UseCors("TricksterCards");

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseDefaultFiles();

app.UseStaticFiles();

app.MapControllers();

app.Run();
