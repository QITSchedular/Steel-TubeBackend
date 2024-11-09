﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NLog;
using Polly;
using ST_Production.Hubs;
using ST_Production.Middlewares;
using ST_Production.Services;
using System.Text;

namespace ST_Production
{
    public class Startup
    {
        //https://stackoverflow.com/questions/71492149/how-to-get-connectionstring-from-secrets-json-in-asp-net-core-6 //connection string
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        public Startup(IConfiguration configuration)
        {
            LogManager.LoadConfiguration(String.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSignalR();
            services.AddScoped<NotificationService>();

            //Add the ExceptionHandler middleware to handle all exceptions in one place to services collection in "Transient" scope
            services.AddTransient<ExceptionHandler>();
            //var builder = WebApplication.CreateBuilder(services);

            //builder.Services.AddCors(options =>
            //{
            //    options.AddPolicy(name: "MyPolicy",
            //                policy =>
            //                {
            //                    policy.WithOrigins("http://example.com",
            //                        "http://www.contoso.com",
            //                        "https://cors1.azurewebsites.net",
            //                        "https://cors3.azurewebsites.net",
            //                        "https://localhost:44398",
            //                        "https://localhost:5001")
            //                            .WithMethods("PUT", "DELETE", "GET");
            //                });
            //});

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 1048576000; // 1000MB
            });

            #region TODO
            services.AddCors(options =>
            {
                options.AddPolicy("MyCorsPolicy",
                    builder =>
                    {
                    ////for Nakawa : 3001
                    //builder
                    //.WithOrigins("http://192.168.25.3:3000", "https://192.168.25.113:3000", "https://14.99.230.26:3000", "http://192.168.25.113:3000", "https://192.168.25.3:3001", "https://192.168.1.14:3000", "https://192.168.25.3:3000", "http://192.168.1.98:3001", "https://192.168.1.98:3001", "https://qrkusters.netlify.app", "http://localhost:3000", "https://localhost:3000", "http://192.168.1.102:3001", "https://192.168.1.102:3001", "http://192.168.1.168:3001", "https://192.168.1.168:3001", "http://192.168.1.226:3001", "https://192.168.1.226:3001")
                    //.AllowAnyMethod()
                    //.AllowCredentials()
                    //.AllowAnyHeader();

                    // for Namanve : 3002
                    builder
                        .WithOrigins("http://192.168.25.3:3000", "https://192.168.25.113:3000", "https://14.99.230.26:3000", "http://192.168.25.113:3000", "https://192.168.25.3:3002", "https://192.168.1.14:3000", "https://192.168.25.3:3000", "http://192.168.1.98:3002", "https://192.168.1.98:3002", "https://qrkusters.netlify.app", "http://localhost:3000", "https://localhost:3000", "http://192.168.1.102:3002", "https://192.168.1.102:3002", "http://192.168.1.168:3002", "https://192.168.1.168:3002", "http://192.168.1.226:3002", "https://192.168.1.226:3002")
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .AllowAnyHeader();
                    });
            });
            #endregion

            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
            //Since ILogger not supported in this method, we need to use traditional Console.Writeline to print messages.
            //And we need these log info only if trace is enabled in logging
            if (Configuration["Logging:LogLevel:Default"].CompareTo("Trace") == 0)
                Console.WriteLine("Adding HttpClient for WeatherService and setting Transient Fault handlers");

            //AddHttpClient::Enabling Single connection for the service using IHttpClientFactory. It can also be used for handling multiple APIs
            services.AddHttpClient<IWeatherService, WeatherService>(w =>
             {
                 w.BaseAddress = new Uri(Configuration["AppSettings:WeatherApiBaseAddress"]);
             })
            .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(5))) //To handle 5XX and 408 http errors
            .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(5))) //To handle continuous Bad Requests                                                                                                   
            .AddPolicyHandler(request =>
            {
                if (request.Method == HttpMethod.Get)
                {
                    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(Convert.ToInt32(Configuration["AppSettings:GetMethodTimeOut"])));
                }
                return Policy.NoOpAsync<HttpResponseMessage>();
            });  //To limit the timeout of the request based on its type.

            //services.AddHttpsRedirection(options =>
            //{
            //    options.HttpsPort = 443; // Set the desired HTTPS port
            //});

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddCookie(x =>
            {
                x.Cookie.Name = "token";
            }).AddJwtBearer(x =>
              {
                  x.RequireHttpsMetadata = false;
                  x.SaveToken = true;
                  x.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                  {
                      ValidateIssuerSigningKey = true,
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
                      ValidateIssuer = false,
                      ValidateAudience = false
                  };
                  x.Events = new JwtBearerEvents
                  {
                      OnMessageReceived = context =>
                      {
                          context.Token = context.Request.Cookies["X-Access-Token"];
                          return Task.CompletedTask;
                      }
                  };
              });

            //var builder = new SqlConnectionStringBuilder(
            //Configuration.GetConnectionString("connectApp:ConnString"));
            //builder.Password = Configuration["DbPassword"];
            //_connection = builder.ConnectionString;
            _connection = Configuration["connectApp:ConnString"];

            if (Configuration["Logging:LogLevel:Default"].CompareTo("Trace") == 0)
                Console.WriteLine("Adding HttpClient for WeatherService and setting Transient Fault handlers completed");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
            //if (env.IsDevelopment() || env.IsProduction())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //    app.UseCors("MyCorsPolicy");
            //    //app.UseDeveloperExceptionPage();
            //}

            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var ex = context.Features.Get<IExceptionHandlerFeature>();
                    if (ex != null)
                    {
                        // Log the exception here
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = ex.Error.Message }));
                    }
                });
            });
            //}

            //app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = string.Empty;
            });

            app.UseCors("MyCorsPolicy");

            app.UseCors(x => x
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .SetIsOriginAllowed(origin => true) // allow any origin
                 .AllowCredentials()); // allow credentials

            ////app.Run(async (context) =>
            ////{
            ////    var result = string.IsNullOrEmpty(_ApplicationApiKey) ? "Null" : "Not Null";
            ////    await context.Response.WriteAsync($"Secret is {result}");
            ////});
            ////app.Run(async (context) =>
            ////{
            ////    await context.Response.WriteAsync($"DB Connection: {_connection}");
            ////});

            app.UseHttpsRedirection();

            app.UseRouting();

            //Use the custom middleware created for Exception handling here
            app.UseMiddleware<ExceptionHandler>();
            app.UseAuthentication();
            app.UseAuthorization();
            //app.MapHub<NotificationHub>("Hubs/notificationHub");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationHub>("Hubs/notificationHub");
            });
        }
    }
}
//working
//https://www.appsloveworld.com/create-asp-net-core-web-api-without-entity-framework


////////reference Example
///https://dev.to/renukapatil/odata-without-entity-framework-in-aspnet-60-and-performing-crud-operations-39li
//https://markjohnson.io/articles/asp-net-core-identity-without-entity-framework/
