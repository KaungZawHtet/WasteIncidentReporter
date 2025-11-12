using System.Collections.Immutable;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Api.Services;

public sealed class WasteClassificationService
{
    private readonly MLContext _ml;
    private readonly ITransformer _model;
    private readonly PredictionEngine<TrainingSample, ClassificationOutput> _engine;
    private readonly IReadOnlyList<string> _labels;
    private readonly object _lock = new();

    private sealed class TrainingSample
    {
        public string Text { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    private sealed class ClassificationOutput
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        public float[] Score { get; set; } = Array.Empty<float>();
    }

    public sealed record ClassificationScore(string Label, float Confidence);

    public sealed record ClassificationResult(string Label, IReadOnlyList<ClassificationScore> Scores);

    public WasteClassificationService()
    {
        _ml = new MLContext(seed: 17);
        var data = _ml.Data.LoadFromEnumerable(TrainingData());
        var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(TrainingSample.Text))
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

    private static IEnumerable<TrainingSample> TrainingData() =>
        new[]
        {
            new TrainingSample { Text = "Plastic bottles, cans overflowing recycling bin downtown", Label = "recyclables" },
            new TrainingSample { Text = "Mixed recyclables piling up near transit station", Label = "recyclables" },
            new TrainingSample { Text = "Cardboard and paper waste stacked beside warehouse", Label = "recyclables" },
            new TrainingSample { Text = "Chemical spill with strong odor near river", Label = "hazardous" },
            new TrainingSample { Text = "Toxic paint buckets dumped illegally", Label = "hazardous" },
            new TrainingSample { Text = "Oil drums leaking in industrial yard", Label = "hazardous" },
            new TrainingSample { Text = "Rotting food scraps attracting pests", Label = "organic" },
            new TrainingSample { Text = "Yard waste and leaves blocking storm drain", Label = "organic" },
            new TrainingSample { Text = "Compost bin overflow with organic matter", Label = "organic" },
            new TrainingSample { Text = "Abandoned computers and monitors near office park", Label = "e-waste" },
            new TrainingSample { Text = "Pile of batteries and phones discarded", Label = "e-waste" },
            new TrainingSample { Text = "CRT monitors dumped behind mall", Label = "e-waste" },
            new TrainingSample { Text = "Construction rubble and concrete dumped roadside", Label = "bulk" },
            new TrainingSample { Text = "Old furniture and mattresses left in alley", Label = "bulk" },
            new TrainingSample { Text = "Demolition debris obstructing sidewalk", Label = "bulk" },
            new TrainingSample { Text = "Illegal dumping of mixed trash in vacant lot", Label = "illegal_dumping" },
            new TrainingSample { Text = "Garbage bags dumped at night behind store", Label = "illegal_dumping" },
            new TrainingSample { Text = "Truck unloading waste outside permitted zone", Label = "illegal_dumping" },
        };
}
