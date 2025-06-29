
using Government.Errors;
using MassTransit;
using Serilog;
using Stripe;
using System.Net;
using System.Text.Json.Serialization;
using NotificationService.Models;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDependancy(builder.Configuration);

/*******************Stripe********************/

var stripeSettings = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSettings["SecretKey"];


/*******************Logging********************/
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
//    .MinimumLevel.Error()
//    .WriteTo.File(
//        "Logs/log-.txt",
//        rollingInterval: RollingInterval.Day,
//        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
//    )
//    .CreateLogger();
//builder.Host.UseSerilog();

/*******************Rabbitmq********************/
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"],
            builder.Configuration["RabbitMQ:VirtualHost"],
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"]!);
                h.Password(builder.Configuration["RabbitMQ:Password"]!);
            });

        
        cfg.Message<NotificationMessage>(c =>
        {
            c.SetEntityName("NotificationMessage"); 
        });

   
        cfg.ConfigureJsonSerializerOptions(options =>
        {
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        });

    
        cfg.ConfigureEndpoints(context);
    });
});

/******************Central-DB********************/
builder.Services.AddHttpClient("CentralApi", client =>
{
    client.BaseAddress = new Uri("https://central-user-management.agreeabledune-30ad0cb8.uaenorth.azurecontainerapps.io/");
});



var app = builder.Build();


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
