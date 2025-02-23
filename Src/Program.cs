using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Net.Mail;
using System.Net;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

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
                .Get<ClientOptions>(); ;
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

            app.Run();
        }
    }


    public record ResetPassword(string Email, string Password, string Token);
    public record ConfirmSignup(string Email, string Password, string Token);
    public record ClientOptions(string Address);
    public record Signup(string Email);
    public record Login(string Username, string Password);
    public record RequestResetPassword(string Username);
    public record JwtOptions(
        string Issuer,
        string Audience,
        string SigningKey,
        int ExpirationSeconds
    );

    public record AuthOptions(
        string BcryptSalt
    );

    public record EmailOptions(
        string EmailHost,
        string AppPassword,
        string EmailUserName,
        string FromEmail
    );
}

