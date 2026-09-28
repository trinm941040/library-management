using UTH.Library.Application.Common;

namespace UTH.Library.Application.Features.Books;

public sealed class BookCommandValidator : IValidator<CreateBookCommand>, IValidator<UpdateBookCommand>
{
    public IReadOnlyCollection<string> Validate(CreateBookCommand command) => Validate(command.Title, command.Author, command.Isbn, command.Category);
    public IReadOnlyCollection<string> Validate(UpdateBookCommand command) => Validate(command.Title, command.Author, command.Isbn, command.Category);
    private static IReadOnlyCollection<string> Validate(string title, string author, string isbn, string category)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title)) errors.Add("Tên sách là bắt buộc.");
        if (string.IsNullOrWhiteSpace(author)) errors.Add("Tác giả là bắt buộc.");
        if (string.IsNullOrWhiteSpace(isbn)) errors.Add("ISBN là bắt buộc.");
        if (string.IsNullOrWhiteSpace(category)) errors.Add("Thể loại là bắt buộc.");
        return errors;
    }
}
