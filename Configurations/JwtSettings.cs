namespace canteen_management.Configurations;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CanteenAPI";
    public string Audience { get; set; } = "CanteenClient";
    public int ExpireMinutes { get; set; } = 60;
}
