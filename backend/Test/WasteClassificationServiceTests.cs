using System.Linq;
using Api.Services;

namespace Test;

public class WasteClassificationServiceTests
{
    private readonly WasteClassificationService _service = new();

    [Fact]
    public void Predict_KnownHazardousText_ReturnsHazardousLabel()
    {
        var result = _service.Predict("Chemical spill with strong odor detected near riverbank");

        Assert.Equal("hazardous", result.Label);
        Assert.NotEmpty(result.Scores);
        Assert.True(result.Scores.First().Confidence >= result.Scores.Last().Confidence);
    }

    [Fact]
    public void Predict_ReturnsProbabilityDistribution()
    {
        var result = _service.Predict("Bags of plastic bottles at recycling drop off");

        var sum = result.Scores.Sum(s => s.Confidence);
        Assert.Equal("recyclables", result.Label);
        Assert.InRange(sum, 0.99f, 1.01f); // softmax normalization
    }
}
