using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class TodoItemTests
{
    [Fact]
    public void Create_EmptyTitle_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TodoItem.Create(" ", DateTime.UtcNow));
    }

    [Fact]
    public void MarkCompleted_IncompleteTodo_MarksTodoCompleted()
    {
        var todo = TodoItem.Create("Read", DateTime.UtcNow);

        todo.MarkCompleted();

        Assert.True(todo.IsCompleted);
    }
}