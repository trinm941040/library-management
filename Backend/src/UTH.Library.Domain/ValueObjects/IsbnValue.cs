namespace UTH.Library.Domain.ValueObjects;

public readonly record struct IsbnValue
{
    private IsbnValue(string value) => Value = value;

    public string Value { get; }

    public static IsbnValue Create(string value)
    {
        var normalized = Normalize(value);
        if (!IsValid(normalized))
            throw new ArgumentException("ISBN must be a valid ISBN-10 or ISBN-13.", nameof(value));

        return new IsbnValue(normalized);
    }

    public static string Normalize(string value) =>
        value.Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

    private static bool IsValid(string value) => value.Length switch
    {
        10 => IsValidIsbn10(value),
        13 => IsValidIsbn13(value),
        _ => false
    };

    private static bool IsValidIsbn10(string value)
    {
        // 1. Loại bỏ các dấu gạch nối và khoảng trắng, chuyển thành chữ hoa
        if (string.IsNullOrWhiteSpace(value)) return false;
        string cleanIsbn = value.Replace("-", "").Replace(" ", "").ToUpper();

        // 2. Kiểm tra độ dài phải đúng 10 ký tự
        if (cleanIsbn.Length != 10) return false;

        // 3. Kiểm tra định dạng: 9 ký tự đầu phải là số, ký tự cuối là số hoặc 'X'
        if (!cleanIsbn.Substring(0, 9).All(char.IsDigit)) return false;
        
        char lastChar = cleanIsbn[9];
        if (!char.IsDigit(lastChar) && lastChar != 'X') return false;

        // 4. Tính tổng checksum theo quy tắc nhân trọng số giảm dần từ 10 đến 2
        int total = 0;
        for (int i = 0; i < 9; i++)
        {
            int digit = cleanIsbn[i] - '0';
            total += digit * (10 - i);
        }

        // 5. Tính số kiểm tra (check digit) hợp lệ
        int remainder = total % 11;
        int checkDigitValue = (11 - remainder) % 11;

        // 6. Lấy giá trị thực tế của ký tự cuối cùng để so sánh
        int actualCheckDigitValue = (lastChar == 'X') ? 10 : (lastChar - '0');

        return checkDigitValue == actualCheckDigitValue;
    }

    private static bool IsValidIsbn13(string value)
    {
        // 1. Loại bỏ các dấu gạch nối và khoảng trắng
        if (string.IsNullOrWhiteSpace(value)) return false;
        string cleanIsbn = value.Replace("-", "").Replace(" ", "");

        // 2. Kiểm tra độ dài phải đúng 13 ký tự và chỉ chứa số
        if (cleanIsbn.Length != 13 || !cleanIsbn.All(char.IsDigit))
        {
            return false;
        }

        // 3. Tính tổng checksum theo quy tắc nhân trọng số xen kẽ 1 và 3
        int total = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = cleanIsbn[i] - '0'; // Chuyển ký tự số sang int
            int weight = (i % 2 == 0) ? 1 : 3;
            total += digit * weight;
        }

        // 4. Tìm số kiểm tra (check digit) hợp lệ
        int remainder = total % 10;
        int checkDigit = (10 - remainder) % 10;

        // 5. So sánh với chữ số cuối cùng của mã nhập vào
        return checkDigit == (cleanIsbn[12] - '0');
    }
}
