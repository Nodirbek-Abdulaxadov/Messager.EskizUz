namespace Messager.Models;

internal class JWT_Token
{
    public string message { get; set; } = string.Empty;
    public Data data { get; set; } = new Data();
    public string token_type { get; set; } = string.Empty;
}

internal class Data
{
    public string token { get; set; } = string.Empty;
}
