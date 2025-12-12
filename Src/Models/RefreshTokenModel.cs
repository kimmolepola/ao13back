public class RefreshToken
{
    public int Id { get; set; }   // <-- Primary key
    public string TokenHash { get; set; }   // store hash, not raw token
    public string UserId { get; set; }
    public DateTime Expires { get; set; }
    public bool Revoked { get; set; }
    public DateTime Created { get; set; }
    public string Role { get; set; }
}
