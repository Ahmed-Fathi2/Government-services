
using Government.Errors;
using Serilog;
using Stripe;
using System.Net;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDependancy(builder.Configuration);

var stripeSettings = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSettings["SecretKey"];


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) 
    .MinimumLevel.Error() 
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
    )
    .CreateLogger();

// Replace the default logger
builder.Host.UseSerilog();

//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.Listen(IPAddress.Loopback, 5050); // ????? ?????? ??? 5050
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

app.UseStaticFiles();

app.Run();
