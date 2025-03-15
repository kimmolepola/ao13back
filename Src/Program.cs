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
            builder.Services.AddSqlite<UserDb>("Data Source=DB.db");
            builder.Services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "ao13back";
                config.Title = "ao13back v1";
                config.Version = "v1";
            });
            JwtOptions? jwtOptions = builder.Configuration
                .GetSection("JwtOptions")
                .Get<JwtOptions>();
            AuthOptions? authOptions = builder.Configuration
                .GetSection("AuthOptions")
                .Get<AuthOptions>();
            EmailOptions? emailOptions = builder.Configuration
                .GetSection("EmailOptions")
                .Get<EmailOptions>();
            ClientOptions? clientOptions = builder.Configuration
                .GetSection("ClientOptions")
                .Get<ClientOptions>();
            TurnOptions? turnOptions = builder.Configuration
                .GetSection("TurnOptions")
                .Get<TurnOptions>();
            Console.WriteLine("----------------------");
            Console.WriteLine("CorsOrigins: " + clientOptions.CorsOrigins);
            Console.WriteLine("----------------------");
            Random random = new();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    name: "WithOrigins",
                    builder =>
                    {
                        builder
                            .WithOrigins(clientOptions.CorsOrigins)
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
                        .GetBytes(jwtOptions?.SigningKey);

                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
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

            AuthService authService = new(app, random, jwtOptions, clientOptions, authOptions, emailOptions);
            UserService userService = new(app);
            TurnService turnService = new(app, turnOptions);
            SignalingHub signalingHub = new();
            GameObjectService gameObjectService = new(app);

            app.Run();
        }
    }
}

