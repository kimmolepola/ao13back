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

var builder = WebApplication.CreateBuilder(args);
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
    options.AddPolicy(name: "AllowAll",
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                        });
});
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
// .AddJwtBearer(jwtOptions =>
// {
// 	jwtOptions.Authority = "ao13v1";
// 	jwtOptions.Audience = "ao13v1";
//     jwtOptions.RequireHttpsMetadata = false;
// });
var jwtOptions = builder.Configuration
	.GetSection("JwtOptions")
    .Get<JwtOptions>();
var authOptions = builder.Configuration
    .GetSection("AuthOptions")
    .Get<AuthOptions>();
var emailOptions = builder.Configuration
    .GetSection("EmailOptions")
    .Get<EmailOptions>();
Random rnd = new Random();
var smtpClient = new SmtpClient(emailOptions?.EmailHost)
{
    Port = 587,
    Credentials = new NetworkCredential(emailOptions?.EmailUserName, emailOptions?.AppPassword),
    EnableSsl = true,
};

// builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        //convert the string signing key to byte array
        byte[] signingKeyBytes = Encoding.UTF8
        	.GetBytes(jwtOptions.SigningKey);

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
var app = builder.Build();
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

app.MapGet("/users", async (UserDb db) =>
{ 
    return await db.Users.ToListAsync();
});

app.MapGet("/users/{id}", async (int id, UserDb db) =>
    await db.Users.FindAsync(id)
        is User user
            ? Results.Ok(user)
            : Results.NotFound());

app.MapPost("/users", async (User user, UserDb db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{user.Id}", user);
});

app.MapPut("/users/{id}", async (int id, User inputUser, UserDb db) =>
{
    var user = await db.Users.FindAsync(id);

    if (user is null) return Results.NotFound();

    user.UserName = inputUser.UserName;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/users/{id}", async (int id, UserDb db) =>
{
    if (await db.Users.FindAsync(id) is User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.MapPost("/api/v1/auth/login", (UserDb db, Login data) =>
{
    Console.WriteLine("--login: " + data);
    User? user;
    if (data.Username.Contains('@')) {
    user = db.Users.Where(u => u.Email == data.Username).FirstOrDefault();
    } else {
    user = db.Users.Where(u => u.UserName == data.Username).FirstOrDefault();
    }
    if (user == null) {
        return Results.Unauthorized();
    }

    bool validCredentials = BCrypt.Net.BCrypt.Verify(data.Password, user.Password);

    if (!validCredentials) {
        return Results.Unauthorized();
    }

      var claims = new List<Claim>()
    {
        new Claim("sub", "" + user.Id),
        new Claim("name", "" + user.Id),
        new Claim("aud", "ao13v1")
    };

    if (jwtOptions?.SigningKey == null) {
        return Results.InternalServerError();
    }
    var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);
    var symmetricKey = new SymmetricSecurityKey(keyBytes);
    var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtOptions.Issuer,
        audience: jwtOptions.Audience,
        claims: claims,
        expires: DateTime.Now.AddSeconds(jwtOptions.ExpirationSeconds),
        signingCredentials: signingCredentials);
    
    var rawToken = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        score = user.Score,
        userId = user.Id,
        username = user.UserName,
        token = rawToken,
    });
});
        

app.MapPost("/api/v1/auth/logout", (UserDb db, HttpContext http, [FromHeader(Name = "Authorization")] string authorization) => {
    Console.WriteLine("--logout: " + authorization);
     Console.WriteLine("##################");
       var iam=http.User.Claims.Where( c=>c.Type == "name").Select(c=>c.Value ).SingleOrDefault();
    //    var id=http.User.Claims.Where( c=>c.Type == "sub").Select(c=>c.Value ).SingleOrDefault();
       var aud=http.User.Claims.Where( c=>c.Type == "aud").Select(c=>c.Value ).ToList();
       Console.WriteLine("id: " + iam);
}).RequireAuthorization();

app.MapPost("/api/v1/auth/signup", (UserDb db, Signup data) => {
    Console.WriteLine("--signup" + data);
    User? user = db.Users.Where(u => u.Email == data.Email).FirstOrDefault();
    if (user != null) {
        return Results.Conflict(new {error = "Email already exist"});
    }
    
    var token = RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMOPQRSTUVXYZ1234567890", 32);
    long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    SignupRequests.signupRequests[data.Email] = new SignupRequest(milliseconds, token);

    var client = builder.Configuration.GetSection("Client").Get<Client>();;

    var link = client?.Address + "/confirm-email?token=" + token + "&email=" + data.Email;

    string subject = "Complete the registration";
    string message = 
        "<html><body>"
        + "<p>Welcome to AO13</p><p>Complete the registration by clicking <a href="
        + link 
        + ">here</a>.</p>"
        + "<p>If the link does not work, copy this to your browser address bar:<br>"
        + link
        + "</body></html>";
    MailMessage mail = new MailMessage(emailOptions?.FromEmail, data.Email, subject, message);
    mail.IsBodyHtml = true;
    smtpClient.Send(mail);

    return Results.Ok();

});

app.MapPost("/api/v1/auth/confirmSignup", async (UserDb db, ConfirmSignup data) => {
    Console.WriteLine("--confirm signup" + data + " " + " qqq " + SignupRequests.signupRequests.Count);
    if (SignupRequests.signupRequests.TryGetValue(data.Email, out SignupRequest? signupRequest)) {
        bool isValid = signupRequest != null && signupRequest.Token == data.Token;
        if (!isValid) {
            return Results.Unauthorized();
        }

        var salt = BCrypt.Net.BCrypt.GenerateSalt(Int32.Parse(authOptions?.BcryptSalt));
        var hash = BCrypt.Net.BCrypt.HashPassword(data.Password, salt);
        Guid idGuid = Guid.NewGuid();
        string id = idGuid.ToString();
        int userNameInt  = rnd.Next(100000000, 1999999999);
        string userName = userNameInt.ToString();
        Console.WriteLine("--id:" + id + " - " + hash + " - " + data.Token);        
        User user = new User(){
            Id = id,
            Email = data.Email,
            UserName = userName,
            Password = hash,
            Score = 0,
        };

        try {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        } catch {
            return Results.InternalServerError();
        }

        string subject = "Welcome to AO13";
        string message = 
            "<html><body>"
            + "<p>Welcome to AO13</p>"
            + "<p>Your temporary username is " + userName + "<br>"
            + "</body></html>";
        MailMessage mail = new MailMessage(emailOptions?.FromEmail, data.Email, subject, message);
        mail.IsBodyHtml = true;
        smtpClient.Send(mail);

        var claims = new List<Claim>()
        {
            new Claim("sub", "" + user.Id),
            new Claim("name", "" + user.Id),
            new Claim("aud", "ao13v1")
        };

        if (jwtOptions?.SigningKey == null) {
            return Results.InternalServerError();
        }
        var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);
        var symmetricKey = new SymmetricSecurityKey(keyBytes);
        var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: DateTime.Now.AddSeconds(jwtOptions.ExpirationSeconds),
            signingCredentials: signingCredentials);
        
        var rawToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new
        {
            score = user.Score,
            userId = user.Id,
            username = user.UserName,
            token = rawToken,
        });   
    } else {
        return Results.Unauthorized();        
    };
 });

app.Run();

public static class SignupRequests {

    static SignupRequests() {
        ExpiredHandler();
    }

    static async void ExpiredHandler() {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync())
        {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            foreach (string key in signupRequests.Keys)
            {
                Console.WriteLine(key);
                if (signupRequests[key].TimeStamp + 600000 < milliseconds) {
                    signupRequests.Remove(key);
                };
            }
        }
    }

    public static Dictionary<string,  SignupRequest> signupRequests = new Dictionary<string, SignupRequest>();
};
public record SignupRequest(long TimeStamp, string Token);
public record ConfirmSignup(string Email, string Password, string Token);
public record class Client(string Address);
public record Signup(string Email);
public record Login(string Username, string Password);
public record class JwtOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    int ExpirationSeconds
);

public record class AuthOptions(
    string BcryptSalt
);

public record class EmailOptions(
    string EmailHost,
    string AppPassword,
    string EmailUserName,
    string FromEmail
);