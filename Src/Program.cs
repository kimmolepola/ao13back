using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ao13back.Src
{
    class Program
    {
        public Program(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "ao13back";
                config.Title = "ao13back v1";
                config.Version = "v1";
            });
            builder.Configuration.AddEnvironmentVariables(prefix: "ASPNETCORE_Ao13back__");
            ConfigurationManager Configuration = builder.Configuration;

            string dbConnectionString = "Data Source=" + Configuration["DBConnectionString:Path"];

            Console.WriteLine("----------------------------------");
            Console.WriteLine("CorsOrigins: " + Configuration["ClientOptions:CorsOrigins"]);
            Console.WriteLine("dbConnectionString: " + dbConnectionString);
            Console.WriteLine("----------------------------------");



            builder.Services.AddSqlite<UserDb>(dbConnectionString);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    name: "WithOrigins",
                    builder =>
                    {
                        builder
                            .WithOrigins(Configuration["ClientOptions:CorsOrigins"])
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
                    //convert the string signing key to byte array
                    byte[] signingKeyBytes = Encoding.UTF8
                        .GetBytes(Configuration["JWTOptions:SigningKey"]);

                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["JWTOptions:Issuer"],
                        ValidAudience = Configuration["JWTOptions:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
                    };

                    opts.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/api/v1/hub")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddSignalR();
            WebApplication app = builder.Build();
            app.UseCors("WithOrigins");
            app.UseAuthentication();
            app.UseAuthorization();
            if (app.Environment.IsDevelopment())
            {
                app.UseOpenApi();
                app.UseSwaggerUi(config =>
                {
                    config.DocumentTitle = "TodoAPI";
                    config.Path = "/swagger";
                    config.DocumentPath = "/swagger/{documentName}/swagger.json";
                    config.DocExpansion = "list";
                });
            }
            app.MapHub<SignalingHub>("/api/v1/hub");

            app.MapGet("/", () => "hello");
            Random random = new();

            AuthService authService = new(app, random, Configuration);
            UserService userService = new(app);
            TurnService turnService = new(app, Configuration);
            SignalingHub signalingHub = new();
            GameObjectService gameObjectService = new(app);

            app.Run();
        }
    }
}

