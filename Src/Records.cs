namespace ao13back.Src;

public record SignalingArgs(string RemoteId, object? Description, object? Candidate);
public record SignupRequest(long TimeStamp, string Token);
public record PasswordResetRequest(long TimeStamp, string Token);
public record UpdateUsername(string Username);
public record ResetPassword(string Email, string Password, string Token);
public record ConfirmSignup(string Email, string Password, string Token);
public record ClientOptions(string Address, string CorsOrigins);
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

public record TurnOptions(
    string HmacSecret,
    string TurnUrl
);

