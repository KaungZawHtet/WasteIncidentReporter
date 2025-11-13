using Api.Services;

namespace Test;

public class TextEmbeddingServiceTests
{
    private readonly TextEmbeddingService _service = new();

    [Fact]
    public void Transform_ReturnsStableVector_ForSameInput()
    {
        var first = _service.Transform("Overflowing recycling bins downtown");
        var second = _service.Transform("Overflowing recycling bins downtown");

        Assert.NotEmpty(first);
        Assert.Equal(first.Length, second.Length);
        Assert.True(first.SequenceEqual(second));
    }

    [Fact]
    public void Transform_GracefullyHandlesEmptyText()
    {
        var vector = _service.Transform(string.Empty);

        Assert.NotNull(vector);
        Assert.NotEmpty(vector);
        Assert.True(vector.All(v => v >= 0));
    }
}
