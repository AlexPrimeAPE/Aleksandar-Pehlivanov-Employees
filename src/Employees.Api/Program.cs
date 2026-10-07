using Employees.Api;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = UploadLimits.MaxBytes);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = UploadLimits.MaxBytes);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapAnalyzeEndpoint();

app.Run();

public partial class Program;
