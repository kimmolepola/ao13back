namespace ao13back.Src;
public class User
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public int? Score { get; set; }
}
