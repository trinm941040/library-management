using System.Security.Cryptography;
using System.Text;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Books;

public static class BookSearchableTextBuilder
{
    public static string Build(Book book) => Build(book.Title, book.Author, book.Category, book.Description);

    public static string Build(string title, string author, string category, string? description)
    {
        var parts = new List<string>
        {
            $"Title: {title.Trim()}", $"Author: {author.Trim()}", $"Category: {category.Trim()}"
        };
        if (!string.IsNullOrWhiteSpace(description)) parts.Add($"Description: {description.Trim()}");
        return string.Join('\n', parts);
    }

    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static bool HasChanged(string previousText, string nextText) =>
        !string.Equals(previousText, nextText, StringComparison.Ordinal);
}
