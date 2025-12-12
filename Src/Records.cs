namespace ao13back.Src;

public record PlayerState(string ClientId, int Score);
// public record SignalingArgs(string RemoteId, object? Description, object? Candidate);
public record SignalingArgs(string Id, string Type, string? Description, string? Candidate, string? Mid);
// public record SignalingArgs(string RemoteId, object? Description, object? Candidate, string? Idx, string? Typex, string? Descriptionx, string? Candidatex, string? Midx);
public record SignupRequest(long TimeStamp, string Token);
public record PasswordResetRequest(long TimeStamp, string Token);
public record UpdateUsername(string Username);
public record ResetPassword(string Email, string Password, string Token);
public record ConfirmSignup(string Email, string Password, string Token);
public record ClientOptions(string Address, string CorsOrigins);
public record Signup(string Email);
public record ServerLogin(string Id, string Password);
public record Login(string Username, string Password);
public record RequestResetPassword(string Username);
public record JwtOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    int ExpirationMinutes
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

