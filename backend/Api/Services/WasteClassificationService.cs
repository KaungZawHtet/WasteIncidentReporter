using System.Collections.Immutable;
using System.Globalization;
using CsvHelper;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Api.Services;

public class WasteClassificationService
{
    private readonly MLContext _ml;
    private readonly ITransformer _model;
    private readonly PredictionEngine<TrainingSample, ClassificationOutput> _engine;
    private readonly IReadOnlyList<string> _labels;
    private readonly object _lock = new();

    private class TrainingSample
    {
        public string Text { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    private class ClassificationOutput
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        public float[] Score { get; set; } = Array.Empty<float>();
    }

    public record ClassificationScore(string Label, float Confidence);

    public record ClassificationResult(string Label, IReadOnlyList<ClassificationScore> Scores);

    public WasteClassificationService()
    {
        _ml = new MLContext(seed: 17);

        var samples = LoadTrainingSamples().ToList();

        var data = _ml.Data.LoadFromEnumerable(samples);
        var pipeline = _ml
            .Transforms.Text.FeaturizeText("Features", nameof(TrainingSample.Text))
            .Append(_ml.Transforms.Conversion.MapValueToKey("Label"))
            .Append(_ml.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _model = pipeline.Fit(data);
        _engine = _ml.Model.CreatePredictionEngine<TrainingSample, ClassificationOutput>(_model);
        _labels = ExtractLabelNames();
    }

    public ClassificationResult Predict(string? description)
    {
        var text = description ?? string.Empty;
        ClassificationOutput output;
        lock (_lock)
        {
            output = _engine.Predict(new TrainingSample { Text = text });
        }

        var probs = Softmax(output.Score);
        var scored = _labels
            .Zip(probs, (label, score) => new ClassificationScore(label, score))
            .OrderByDescending(s => s.Confidence)
            .ToList();

        var label = scored.Count > 0 ? scored[0].Label : "unknown";
        return new ClassificationResult(label, scored);
    }

    private IReadOnlyList<string> ExtractLabelNames()
    {
        var scoreSchema = _engine.OutputSchema["Score"];
        var slotNames = default(VBuffer<ReadOnlyMemory<char>>);
        scoreSchema.GetSlotNames(ref slotNames);
        return slotNames.DenseValues().Select(v => v.ToString()).ToImmutableArray();
    }

    private static float[] Softmax(IReadOnlyList<float> values)
    {
        if (values.Count == 0)
        {
            return Array.Empty<float>();
        }

        var max = values.Max();
        var exp = values.Select(v => MathF.Exp(v - max)).ToArray();
        var sum = exp.Sum();
        return sum == 0
            ? Enumerable.Repeat(0f, values.Count).ToArray()
            : exp.Select(v => v / sum).ToArray();
    }

    private static IEnumerable<TrainingSample> LoadTrainingSamples()
    {
        var path = ResolveClassificationCsvPath();
        if (!File.Exists(path))
        {
            return Array.Empty<TrainingSample>();
        }

        try
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            if (!csv.Read())
            {
                return Array.Empty<TrainingSample>();
            }

            csv.ReadHeader();
            var rows = new List<TrainingSample>();
            while (csv.Read())
            {
                var text = csv.GetField("description");
                var category = csv.GetField("category");
                if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(category))
                {
                    continue;
                }

                rows.Add(new TrainingSample { Text = text.Trim(), Label = category.Trim() });
            }

            return rows;
        }
        catch
        {
            return Array.Empty<TrainingSample>();
        }
    }

    private static string ResolveClassificationCsvPath() =>
        Path.Combine(AppContext.BaseDirectory, "db", "waste_classification_samples.csv");
}
