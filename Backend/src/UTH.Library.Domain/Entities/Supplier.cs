using System.Net.Mail;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Supplier
{
    private Supplier() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }
    public RecordStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static Supplier Create(string code, string name, string? contactName, string? email, string? phoneNumber, string? address)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Status = RecordStatus.Active };
        supplier.Update(code, name, contactName, email, phoneNumber, address);
        return supplier;
    }

    public void Update(string code, string name, string? contactName, string? email, string? phoneNumber, string? address)
    {
        Code = Required(code, 30).ToUpperInvariant();
        Name = Required(name, 200);
        ContactName = Optional(contactName, 150);
        Email = NormalizeEmail(email);
        PhoneNumber = NormalizePhone(phoneNumber);
        Address = Optional(address, 500);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Deactivate() { Status = RecordStatus.Inactive; ConcurrencyToken = Guid.NewGuid(); }
    public void Activate() { Status = RecordStatus.Active; ConcurrencyToken = Guid.NewGuid(); }

    private static string Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException("Giá trị nhà cung cấp không hợp lệ.");
        return value.Trim();
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim();
        if (result.Length > max) throw new ArgumentException("Giá trị nhà cung cấp không hợp lệ.");
        return result;
    }

    private static string? NormalizeEmail(string? value)
    {
        var email = Optional(value, 256)?.ToLowerInvariant();
        if (email is not null && (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))
            throw new ArgumentException("Email nhà cung cấp không hợp lệ.");
        return email;
    }

    private static string? NormalizePhone(string? value)
    {
        var phone = Optional(value, 32);
        if (phone is not null && phone.Any(character => !char.IsDigit(character) && character is not '+' and not '-' and not ' ' and not '(' and ')'))
            throw new ArgumentException("Số điện thoại nhà cung cấp không hợp lệ.");
        return phone;
    }
}
