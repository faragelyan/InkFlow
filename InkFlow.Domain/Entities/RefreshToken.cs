namespace InkFlow.Domain;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedOnUtc { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresOnUtc;
    public bool IsRevoked => RevokedOnUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}
