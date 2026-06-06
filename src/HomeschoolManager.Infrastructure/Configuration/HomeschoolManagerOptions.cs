namespace HomeschoolManager.Infrastructure.Configuration;

public sealed class HomeschoolManagerOptions
{
    public string DataRoot { get; set; } = "";

    public bool UseDevelopmentDataRoot { get; set; }

    public string StudentPin { get; set; } = "1234";
}
