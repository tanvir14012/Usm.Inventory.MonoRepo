using System.Text.Json;
using Shared.AI.Python;
using Xunit;

namespace Usm.Shared.AI.Tests;

public sealed class TransformersWrapperTests
{
    [Fact]
    public async Task SummarizeAsync_UsesPersistentManager()
    {
        var manager = new FakeManager();
        var wrapper = new TransformersWrapper(manager);

        var summary = await wrapper.SummarizeAsync("hello world");

        Assert.Equal("summary", summary);
        Assert.Equal(PythonOperations.Summarization, manager.LastRequest!.Operation);
    }

    private sealed class FakeManager : IPythonProcessManager
    {
        public PythonRequest? LastRequest { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public PythonRuntimeSnapshot GetSnapshot() => new(0, 0, 0, 0, false, null);

        public Task<PythonResponse> InvokeAsync(PythonRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new PythonResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Result = System.Text.Json.JsonSerializer.SerializeToElement("summary")
            });
        }

        public async Task<T> InvokeAsync<T>(PythonRequest request, CancellationToken cancellationToken = default)
        {
            var response = await InvokeAsync(request, cancellationToken);
            return JsonSerializer.Deserialize<T>(response.Result!.Value.GetRawText())!;
        }

        public Task<float[]> GetEmbeddingAsync(string text, string model, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PythonResponse> ClassifyAsync(string text, string model, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<string, List<string>>> ExtractEntitiesAsync(string text, string model, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<T> InvokeCustomAsync<T>(string functionName, IDictionary<string, object?> arguments, string? model = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
