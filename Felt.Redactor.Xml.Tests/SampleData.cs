namespace Felt.Redactor.Xml.Tests
{
    internal static class SampleData
    {
        internal static readonly object UserBillingHistory = new
        {
            Username = "jdoe",
            Password = "P@ssw0rd5",
            PasswordHistory = new[] { "P@ssw0rd1", "P@ssw0rd1", "P@ssw0rd3" },

            FirstName = "John",
            LastName = "Doe",
            Age = 26,

            SocialSecurityNumber = 1234567890,

            Payments = new[]
            {
                new
                {
                    Date = "2018-02-03",
                    Value = 31.00d,
                    Type = "Check",
                    CheckNumber = "2468",
                    CreditCardData = null as dynamic
                },
                new
                {
                    Date = "2018-03-04",
                    Value = 28.16d,
                    Type = "CreditCard",
                    CheckNumber = default(string),
                    CreditCardData = new
                    {
                        Type = "Visa",
                        Number = "4111111111111111",
                        Expiration = "04/25",
                        Cvv = "258",
                        InPerson = false
                    } as dynamic
                }
            }
        };
    }
}