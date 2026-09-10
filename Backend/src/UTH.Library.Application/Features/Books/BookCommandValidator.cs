using UTH.Library.Application.Common;

namespace UTH.Library.Application.Features.Books;

public sealed class BookCommandValidator : IValidator<CreateBookCommand>, IValidator<UpdateBookCommand>
{
    public IReadOnlyCollection<string> Validate(CreateBookCommand command) => Validate(command.Title, command.Author, command.Isbn, command.Category, command.Quantity);
    public IReadOnlyCollection<string> Validate(UpdateBookCommand command) => Validate(command.Title, command.Author, command.Isbn, command.Category, command.Quantity);
    private static IReadOnlyCollection<string> Validate(string title, string author, string isbn, string category, int quantity)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title)) errors.Add("Book title is required.");
        if (string.IsNullOrWhiteSpace(author)) errors.Add("Book author is required.");
        if (string.IsNullOrWhiteSpace(isbn)) errors.Add("Book ISBN is required.");
        if (string.IsNullOrWhiteSpace(category)) errors.Add("Book category is required.");
        if (quantity < 0) errors.Add("Book quantity cannot be negative.");
        return errors;
    }
}
