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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    name: "AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
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
                .Get<TurnOptions>(); ;
            Random random = new();

            // builder.Services.AddSingleton(jwtOptions);
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
                });
            builder.Services.AddAuthorization();
            WebApplication app = builder.Build();
            app.UseCors("AllowAll");
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

            AuthService authService = new(app, random, jwtOptions, clientOptions, authOptions, emailOptions);
            UserService userService = new(app);
            TurnService turnService = new(app, turnOptions);
            SocketService socketService = new(app);

            app.Run();
        }
    }
}

