namespace MicroNamaa.Application.Settings;

/// <summary>Test data for development: one merchant with an owner and a cashier, products, stock, customers and suppliers. Turn it off in production.</summary>
public class SampleDataSettings
{
    public const string SectionName = "SampleData";

    public bool Enabled { get; set; }

    public string OwnerPhone { get; set; } = "0910000001";

    public string CashierPhone { get; set; } = "0910000002";

    public string Password { get; set; } = "Test@12345";
}
