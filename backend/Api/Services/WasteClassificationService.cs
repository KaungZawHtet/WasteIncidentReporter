using System.Collections.Immutable;
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
        var data = _ml.Data.LoadFromEnumerable(TrainingData());
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

    private static IEnumerable<TrainingSample> TrainingData() =>
        [
            new TrainingSample
            {
                Text = "Plastic bottles, cans overflowing recycling bin downtown",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Mixed recyclables piling up near transit station",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Cardboard and paper waste stacked beside warehouse",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Glass jars and aluminum piled beside neighborhood drop-off",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Bundle of newspapers left near library entrance",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Plastic packaging blowing across parking lot",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Recycling carts overflowing with cardboard boxes",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Blue bins full of cans and bottles behind school",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Chemical spill with strong odor near river",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Toxic paint buckets dumped illegally",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Oil drums leaking in industrial yard",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Pesticide containers found near community garden",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Laboratory solvents dumped behind clinic",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Mercury thermometer broken on sidewalk",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Acidic liquid seeping from warehouse loading dock",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Battery acid leaking beside electric substation",
                Label = "hazardous",
            },
            new TrainingSample { Text = "Rotting food scraps attracting pests", Label = "organic" },
            new TrainingSample
            {
                Text = "Yard waste and leaves blocking storm drain",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Compost bin overflow with organic matter",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Cafe tossing spoiled produce into alley",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Grocery store dumpster full of rotten fruit",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Restaurant grease traps spilling onto sidewalk",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Park littered with grass clippings and branches",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Farmers market bins full of unsold vegetables",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Abandoned computers and monitors near office park",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Pile of batteries and phones discarded",
                Label = "e-waste",
            },
            new TrainingSample { Text = "CRT monitors dumped behind mall", Label = "e-waste" },
            new TrainingSample
            {
                Text = "Laptop screens and cables strewn across loading dock",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Server racks abandoned near data center",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Printer cartridges tossed behind copy shop",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Smartphones and chargers mixed with regular trash",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Obsolete televisions stacked behind theater",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Construction rubble and concrete dumped roadside",
                Label = "bulk",
            },
            new TrainingSample
            {
                Text = "Old furniture and mattresses left in alley",
                Label = "bulk",
            },
            new TrainingSample { Text = "Demolition debris obstructing sidewalk", Label = "bulk" },
            new TrainingSample
            {
                Text = "Broken pallets and drywall piled behind project site",
                Label = "bulk",
            },
            new TrainingSample
            {
                Text = "Discarded carpet rolls blocking apartment driveway",
                Label = "bulk",
            },
            new TrainingSample
            {
                Text = "Large tree limbs stacked beside bike path",
                Label = "bulk",
            },
            new TrainingSample { Text = "Hot tub shell dumped next to playground", Label = "bulk" },
            new TrainingSample
            {
                Text = "Metal beams and bricks abandoned beside parking lot",
                Label = "bulk",
            },
            new TrainingSample
            {
                Text = "Illegal dumping of mixed trash in vacant lot",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Garbage bags dumped at night behind store",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Truck unloading waste outside permitted zone",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Household trash thrown over park fence",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Contractor dumping debris off the highway ramp",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Dumpster contents spread across alley overnight",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Mixed refuse left beside stormwater pond",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Trash trailers emptying onto rural roadside",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Overflowing recycling bins near metro station",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Broken glass bottles scattered near collection point",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Aluminum cans dumped near freight platform",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Plastic wrap and cardboard tossed outside warehouse",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Overloaded curbside bins with paper and cans",
                Label = "recyclables",
            },
            new TrainingSample
            {
                Text = "Hazardous solvent drums rusting on pier",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Glow sticks and lab materials leaking in trash",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Used needles discovered near clinic dumpster",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Expired pharmaceuticals dumped behind pharmacy",
                Label = "hazardous",
            },
            new TrainingSample
            {
                Text = "Spoiled meat leaking from grocery compactor",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Holiday tree piles blocking community garden gate",
                Label = "organic",
            },
            new TrainingSample
            {
                Text = "Electronics kiosk overflowing with phones",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Abandoned copier and fax machines near lobby",
                Label = "e-waste",
            },
            new TrainingSample
            {
                Text = "Couch and dresser dumped beside river trail",
                Label = "bulk",
            },
            new TrainingSample
            {
                Text = "Construction site leaving drywall scraps on curb",
                Label = "bulk",
            },
            new TrainingSample
            {
                Text = "Pickup truck dumping trash bags in field",
                Label = "illegal_dumping",
            },
            new TrainingSample
            {
                Text = "Contractor disposing rubble in public park",
                Label = "illegal_dumping",
            },
        ];
}
