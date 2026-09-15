using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class Shelf
{
	private Shelf() { }
	public static Shelf Create(Guid areaId, string code, string label) => new() { Id = Guid.NewGuid(), AreaId = areaId, Code = Required(code, 30).ToUpperInvariant(), Label = Required(label, 150), Status = ShelfStatus.Active, ConcurrencyToken = Guid.NewGuid() };
	public Guid Id { get; private set; } public Guid AreaId { get; private set; } public string Code { get; private set; } = string.Empty; public string Label { get; private set; } = string.Empty; public ShelfStatus Status { get; private set; } public Guid ConcurrencyToken { get; private set; }
	public void Update(string code, string label) { Code = Required(code, 30).ToUpperInvariant(); Label = Required(label, 150); ConcurrencyToken = Guid.NewGuid(); }
	public void Deactivate() { Status = ShelfStatus.Inactive; ConcurrencyToken = Guid.NewGuid(); }
	public void Activate() { Status = ShelfStatus.Active; ConcurrencyToken = Guid.NewGuid(); }
	private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException("Location value is invalid."); return value.Trim(); }
}
