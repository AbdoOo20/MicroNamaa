using PhoneNumbers;

namespace MicroNamaa.Application.Common;

/// <summary>
/// Libyan mobile numbers (091, 092, 093, 094, 095), checked with Google's libphonenumber
/// and stored in the local 10-digit form (09xxxxxxxx).
/// </summary>
public static class LibyanPhone
{
    public const string InvalidMessage = "Enter a valid Libyan mobile number (09xxxxxxxx).";

    private const string Region = "LY";

    private const int LibyaCode = 218;

    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Accepts +218, 00218, 218, a missing leading zero and Arabic-Indic digits. Returns the local form, or null when
    /// libphonenumber does not recognise it as a Libyan mobile number.
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = new string(input
            .Select(character => character is >= '٠' and <= '٩' ? (char)('0' + (character - '٠')) : character)
            .Where(character => char.IsAsciiDigit(character) || character == '+')
            .ToArray());

        if (digits.StartsWith("00"))
        {
            digits = "+" + digits[2..];
        }

        else if (digits.StartsWith("218") && digits.Length == 12)
        {
            digits = "+" + digits;
        }

        PhoneNumber number;

        try
        {
            number = Util.Parse(digits, Region);
        }

        catch (NumberParseException)
        {
            return null;
        }

        var isMobile = Util.GetNumberType(number) is PhoneNumberType.MOBILE or PhoneNumberType.FIXED_LINE_OR_MOBILE;

        if (number.CountryCode != LibyaCode || !Util.IsValidNumberForRegion(number, Region) || !isMobile)
        {
            return null;
        }

        return "0" + number.NationalNumber;
    }

    /// <summary>An optional number: null stays null, anything else must be valid.</summary>
    public static bool TryNormalizeOptional(string? input, out string? phone)
    {
        phone = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        phone = Normalize(input);

        return phone is not null;
    }
}
