using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Net.Mail;
using System.Net;

namespace ao13back.Src;

class AuthService
{
    public AuthService(
        WebApplication app,
        Random random,
        ConfigurationManager Configuration)
    {
        var smtpClient = new SmtpClient(Configuration["EmailOptions:EmailHost"])
        {
            Port = 587,
            Credentials = new NetworkCredential(Configuration["EmailOptions:EmailUserName"], Configuration["EmailOptions:AppPassword"]),
            EnableSsl = true,
        };
        app.MapPost("/api/v1/auth/login", (UserDb db, Login data) =>
        {
            Console.WriteLine("Login: " + data);
            User? user;
            if (data.Username.Contains('@'))
            {
                user = db.Users.Where(u => u.Email == data.Username).FirstOrDefault();
            }
            else
            {
                user = db.Users.Where(u => u.UserName == data.Username).FirstOrDefault();
            }
            if (user == null)
            {
                return Results.Unauthorized();
            }

            bool validCredentials = BCrypt.Net.BCrypt.Verify(data.Password, user.Password);

            if (!validCredentials)
            {
                return Results.Unauthorized();
            }

            var claims = new List<Claim>()
            {
                new Claim("name", "" + user.Id),
            };

            if (Configuration["JWTOptions:SigningKey"] == null)
            {
                return Results.InternalServerError();
            }
            var keyBytes = Encoding.UTF8.GetBytes(Configuration["JWTOptions:SigningKey"]);
            var symmetricKey = new SymmetricSecurityKey(keyBytes);
            var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Configuration["JWTOptions:Issuer"],
                audience: Configuration["JWTOptions:Audience"],
                claims: claims,
                expires: DateTime.Now.AddSeconds(int.Parse(Configuration["JWTOptions:ExpirationSeconds"])),
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


        app.MapPost("/api/v1/auth/logout", (UserDb db, HttpContext http, [FromHeader(Name = "Authorization")] string authorization) =>
        {
            Console.WriteLine("Logout: " + authorization);
            string? id = http.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
            SignalingHub.Disconnect(id);
            return Results.Ok();
        }).RequireAuthorization();

        app.MapPost("/api/v1/auth/signup", (UserDb db, Signup data) =>
        {
            Console.WriteLine("Signup" + data);
            User? user = db.Users.Where(u => u.Email == data.Email).FirstOrDefault();
            if (user != null)
            {
                return Results.Conflict(new { error = "Email already exist" });
            }
            if (SignupRequests.signupRequests.ContainsKey(data.Email))
            {
                return Results.Conflict(new { error = "Signup email already sent. Check your inbox or try again later." });
            }

            var token = RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMOPQRSTUVXYZ1234567890", 32);
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            SignupRequests.signupRequests[data.Email] = new SignupRequest(milliseconds, token);

            var link = Configuration["ClientOptions:Address"] + "/confirm-email?token=" + token + "&email=" + data.Email;

            string subject = "Complete the registration";
            string message =
                "<html><body>"
                + "<p>Welcome to AO13</p>"
                + "<p>Complete the registration by clicking <a href="
                + link
                + ">here</a>.</p>"
                + "<p>If the link does not work, copy this to your browser address bar:<br>"
                + link
                + "</body></html>";
            MailMessage mail = new(Configuration["EmailOptions:FromEmail"], data.Email, subject, message) { IsBodyHtml = true };
            smtpClient.Send(mail);

            return Results.Ok();

        });

        app.MapPost("/api/v1/auth/confirmSignup", async (UserDb db, ConfirmSignup data) =>
        {
            Console.WriteLine("Confirm sighup: " + data);
            if (SignupRequests.signupRequests.TryGetValue(data.Email, out SignupRequest? signupRequest))
            {
                bool isValid = signupRequest != null && signupRequest.Token == data.Token;
                if (!isValid)
                {
                    return Results.Unauthorized();
                }

                var salt = BCrypt.Net.BCrypt.GenerateSalt(int.Parse(Configuration["AuthOptions:BcryptSalt"]));
                var hash = BCrypt.Net.BCrypt.HashPassword(data.Password, salt);
                Guid idGuid = Guid.NewGuid();
                string id = idGuid.ToString();
                int userNameInt = random.Next(100000000, 1999999999);
                string userName = userNameInt.ToString();
                User user = new()
                {
                    Id = id,
                    Email = data.Email,
                    UserName = userName,
                    Password = hash,
                    Score = 0,
                };

                try
                {
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                    SignupRequests.signupRequests.Remove(data.Email);
                }
                catch
                {
                    return Results.InternalServerError();
                }

                string subject = "Welcome to AO13";
                string message =
                    "<html><body>"
                    + "<p>Welcome to AO13</p>"
                    + "<p>Your temporary username is " + userName + "<br>"
                    + "</body></html>";
                MailMessage mail = new(Configuration["EmailOptions:FromEmail"], data.Email, subject, message) { IsBodyHtml = true };
                smtpClient.Send(mail);

                var claims = new List<Claim>()
                {
                        new("name", "" + user.Id),
                };

                if (Configuration["JWTOptions:SigningKey"] == null)
                {
                    return Results.InternalServerError();
                }
                var keyBytes = Encoding.UTF8.GetBytes(Configuration["JWTOptions:SigningKey"]);
                var symmetricKey = new SymmetricSecurityKey(keyBytes);
                var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: Configuration["JWTOptions:Issuer"],
                    audience: Configuration["JWTOptions:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddSeconds(int.Parse(Configuration["JWTOptions:ExpirationSeconds"])),
                    signingCredentials: signingCredentials);

                var rawToken = new JwtSecurityTokenHandler().WriteToken(token);

                return Results.Ok(new
                {
                    score = user.Score,
                    userId = user.Id,
                    username = user.UserName,
                    token = rawToken,
                });
            }
            else
            {
                return Results.Unauthorized();
            }
        });

        app.MapPost("/api/v1/auth/requestResetPassword", (UserDb db, RequestResetPassword data) =>
        {
            Console.WriteLine("RequestResetPassword " + data);
            User? user;
            if (data.Username.Contains('@'))
            {
                user = db.Users.Where(u => u.Email == data.Username).FirstOrDefault();
            }
            else
            {
                user = db.Users.Where(u => u.UserName == data.Username).FirstOrDefault();
            }
            if (user == null)
            {
                return Results.BadRequest(new { error = "User does not exist" });
            }
            if (PasswordResetRequests.passwordResetRequests.ContainsKey(user.Email))
            {
                return Results.Conflict(new { error = "Password reset email already sent. Check your inbox or try again later." });
            }
            var token = RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMOPQRSTUVXYZ1234567890", 32);
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            PasswordResetRequests.passwordResetRequests[user.Email] = new PasswordResetRequest(milliseconds, token);

            var link = Configuration["ClientOptions:Address"] + "/reset-password?token=" + token + "&email=" + user.Email;

            string subject = "Password reset request";
            string message =
                "<html><body>"
                + "<p>Password reset request</p>"
                + "<p>Reset your password by clicking <a href="
                + link
                + ">here</a>.</p>"
                + "<p>If the link does not work, copy this to your browser address bar:<br>"
                + link
                + "</body></html>";
            MailMessage mail = new(Configuration["EmailOptions:FromEmail"], user.Email, subject, message) { IsBodyHtml = true };
            smtpClient.Send(mail);

            return Results.Ok();
        });

        app.MapPost("/api/v1/auth/resetPassword", async (UserDb db, ResetPassword data) =>
        {
            Console.WriteLine("Reset password: " + data);
            if (PasswordResetRequests.passwordResetRequests.TryGetValue(data.Email, out PasswordResetRequest? passwordResetRequest))
            {
                bool isValid = passwordResetRequest != null && passwordResetRequest.Token == data.Token;
                if (!isValid)
                {
                    return Results.Unauthorized();
                }
                User? user = db.Users.Where(u => u.Email == data.Email).FirstOrDefault();

                if (user == null)
                {
                    return Results.NoContent();
                }

                var salt = BCrypt.Net.BCrypt.GenerateSalt(int.Parse(Configuration["AuthOptions:BcryptSalt"]));
                var hash = BCrypt.Net.BCrypt.HashPassword(data.Password, salt);

                user.Password = hash;
                try
                {
                    db.Users.Update(user);
                    await db.SaveChangesAsync();
                    PasswordResetRequests.passwordResetRequests.Remove(data.Email);
                }
                catch
                {
                    return Results.InternalServerError();
                }

                string subject = "Password reset successfully";
                string message =
                    "<html><body>"
                    + "<p>Your password has been changed successfully</p>"
                    + "</body></html>";
                MailMessage mail = new(Configuration["EmailOptions:FromEmail"], user.Email, subject, message) { IsBodyHtml = true };
                smtpClient.Send(mail);

                return Results.Ok();
            }
            return Results.Unauthorized();
        });
    }

    private static class PasswordResetRequests
    {
        public static readonly Dictionary<string, PasswordResetRequest> passwordResetRequests = [];

        static PasswordResetRequests()
        {
            ExpiredHandler();
        }

        static async void ExpiredHandler()
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

            while (await timer.WaitForNextTickAsync())
            {
                long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                foreach (string key in passwordResetRequests.Keys)
                {
                    Console.WriteLine(key);
                    if (passwordResetRequests[key].TimeStamp + 600000 < milliseconds)
                    {
                        passwordResetRequests.Remove(key);
                    }
                }
            }
        }
    };

    private static class SignupRequests
    {
        public static readonly Dictionary<string, SignupRequest> signupRequests = [];

        static SignupRequests()
        {
            ExpiredHandler();
        }

        static async void ExpiredHandler()
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

            while (await timer.WaitForNextTickAsync())
            {
                long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                foreach (string key in signupRequests.Keys)
                {
                    Console.WriteLine(key);
                    if (signupRequests[key].TimeStamp + 600000 < milliseconds)
                    {
                        signupRequests.Remove(key);
                    }
                }
            }
        }
    }
}
