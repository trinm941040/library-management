using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UTH.Library.Application.Abstractions.AI;
using UTH.Library.Application.Common;

namespace UTH.Library.Infrastructure.AI;

internal sealed class OpenAiEmbeddingService(HttpClient httpClient, IOptions<AiEmbeddingOptions> options)
    : IEmbeddingService
{
    private readonly AiEmbeddingOptions settings = options.Value;
    public int Dimensions => settings.EmbeddingDimensions;

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Nội dung tạo embedding là bắt buộc.", nameof(text));
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new ExternalServiceUnavailableException("Chưa cấu hình khóa API cho dịch vụ embedding.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = settings.EmbeddingModel,
            input = text,
            dimensions = settings.EmbeddingDimensions
        });
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceUnavailableException(
                    $"Dịch vụ embedding từ chối yêu cầu (HTTP {(int)response.StatusCode}).");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
            var vector = payload?.Data?.FirstOrDefault()?.Embedding;
            if (vector is null || vector.Length != Dimensions)
                throw new ExternalServiceUnavailableException(
                    $"Dịch vụ embedding trả về vector không đúng {Dimensions} chiều.");
            return vector;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceUnavailableException("Dịch vụ embedding phản hồi quá thời gian cho phép.");
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalServiceUnavailableException("Không thể kết nối dịch vụ embedding.", exception);
        }
    }

    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingData>? Data);
    private sealed record EmbeddingData(float[] Embedding);
}
