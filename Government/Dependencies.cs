using FluentValidation;
using FluentValidation.AspNetCore;
using Government.ApplicationServices.AdminServices;
using Government.ApplicationServices.Fields;
using Government.ApplicationServices.Files;
using Government.ApplicationServices.GovernmentServices;
using Government.ApplicationServices.PaymentService;
using Government.ApplicationServices.RequestServices;
using Government.ApplicationServices.Results;
using Government.ApplicationServices.services;
using Government.ApplicationServices.UploadFiles;
using Government.ApplicationServices.UploadServiceImage;
using Government.Authentication;
using Government.Errors;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using SurvayBasket.ApplicationServices.SendingEmail;
using SurvayBasket.ApplicationServices.UserAccount;
using SurvayBasket.Settigns.cs;
using System.Text;

namespace Government
{
    public static class Dependencies
    {
        public static IServiceCollection AddDependancy(this IServiceCollection services, IConfiguration configuration) {

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

            // services.AddControllers();
            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader());
            });


            // using identity
            services.AddIdentity<AppUser, AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<IRequestService, RequestService>();
            services.AddScoped<IAdminAuthService, AdminAuthService>();
            services.AddScoped<IService, service>();
            services.AddScoped<IAdminResponseToRequest, AdminResponseToRequest>();
            services.AddScoped<IEmailSender, EmailService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IFieldServcie, FieldServcie>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRequiredFileServcie, RequiredFileServcie>();
            services.AddScoped<IAttachedFileServcie, AttachedFileServcie>();
            services.AddScoped<Iserviceimage, serviceimage>();
            services.AddScoped<IPaymentService, PaymentService>();
           // services.AddScoped<IOtpService, OtpService>();
           


            // Exception Handler
            services.AddExceptionHandler<GlobalExceptionHandler>()
                    .AddProblemDetails();

            //Swagger
            services.AddSwagger()
                    .AddConnnectionString(configuration)
                    .AddAuthConfig(configuration)
                    .AddMapster();
             
            // fluent validation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddFluentValidationAutoValidation();


            services.Configure<EmailSettings>(configuration.GetSection(nameof(EmailSettings)));

            return services;
        }


        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddConnnectionString(this IServiceCollection services, IConfiguration configuration)
        {

            var connectionstring = configuration.GetConnectionString("GovernmentConnection") ??
                throw new InvalidOperationException("Connection String 'GovernmentConnection' Not Found !!");

            services.AddDbContext<AppDbContext>(option => option.UseSqlServer(connectionstring).ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));



            return services;
        }

        public static IServiceCollection AddAuthConfig(this IServiceCollection services, IConfiguration configuration)
        {


      
            services.AddSingleton<IAdminJwtProvider, AdminJwtProvider>();

            /* option pattern configurations */
           // services.Configure<JwtOption>(configuration.GetSection(nameof(JwtOption)));

            services.AddOptions<JwtOption>()
                    .BindConfiguration(nameof(JwtOption))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            var Jwtsettings = configuration.GetSection(nameof(JwtOption)).Get<JwtOption>();


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwtsettings!.Key)),
                    ValidIssuer = Jwtsettings.Issuer,
                    ValidAudience = Jwtsettings.Audience
                };
            });

            return services;

        }

        private static IServiceCollection AddMapster(this IServiceCollection service)
        {

            //mapster
            var mappingconfig = TypeAdapterConfig.GlobalSettings;
            mappingconfig.Scan(Assembly.GetExecutingAssembly());
            service.AddSingleton<IMapper>(new Mapper(mappingconfig));

            return service;
        }
    }
}
