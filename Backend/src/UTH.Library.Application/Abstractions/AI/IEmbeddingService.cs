namespace UTH.Library.Application.Abstractions.AI;

public interface IEmbeddingService
{
    int Dimensions { get; }

    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
