namespace UTH.Library.Domain.Entities;
public sealed class Area
{
	private Area() { }
	public static Area Create(Guid branchId, string code, string name) => new() { Id = Guid.NewGuid(), BranchId = branchId, Code = Required(code, 30).ToUpperInvariant(), Name = Required(name, 150), ConcurrencyToken = Guid.NewGuid() };
	public Guid Id { get; private set; } public Guid BranchId { get; private set; } public string Code { get; private set; } = string.Empty; public string Name { get; private set; } = string.Empty; public Guid ConcurrencyToken { get; private set; }
	public void Update(string code, string name) { Code = Required(code, 30).ToUpperInvariant(); Name = Required(name, 150); ConcurrencyToken = Guid.NewGuid(); }
	private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException("Location value is invalid."); return value.Trim(); }
}
