using Microsoft.ML;
using Microsoft.ML.Data;

namespace Api.Services;

public class TextEmbeddingService
{
    private readonly MLContext _ml;
    private readonly ITransformer _model;
    private readonly object _lock = new();

    private class Input
    {
        public string Text { get; set; } = "";
    }

    private class Output
    {
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }

    public TextEmbeddingService()
    {
        _ml = new MLContext(seed: 42);
        // Minimal “fit-once” pipeline: Text -> TF-IDF
        var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(Input.Text));
        // Fit on an empty tiny dataset so the model is ready; we can refit later if needed
        var empty = _ml.Data.LoadFromEnumerable(new List<Input>());
        _model = pipeline.Fit(empty);
    }

    public float[] Transform(string text)
    {
        lock (_lock)
        {
            var data = _ml.Data.LoadFromEnumerable(new[] { new Input { Text = text ?? "" } });
            var transformed = _model.Transform(data);
            var outCols = _ml
                .Data.CreateEnumerable<Output>(transformed, reuseRowObject: false)
                .First();
            return outCols.Features;
        }
    }
}
