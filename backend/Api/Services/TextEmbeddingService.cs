using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        var pipeline = _ml.Transforms.Text.FeaturizeText("Features", nameof(Input.Text));

        var seedCorpus = BuildSeedCorpus().ToList();
        if (seedCorpus.Count == 0)
        {
            seedCorpus.Add("waste incident report");
        }

        var data = _ml.Data.LoadFromEnumerable(
            seedCorpus.Select(text => new Input { Text = text })
        );
        _model = pipeline.Fit(data);
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

    private static IEnumerable<string> BuildSeedCorpus() =>
        new[]
        {
            "Overflowing recycling bins near metro station",
            "Construction debris blocking bike lane downtown",
            "Illegal dumping of mixed trash in vacant lot",
            "Oil drums leaking in industrial yard",
            "Rotting food waste attracting pests in alley",
            "Chemical spill reported along riverbank",
            "E-waste pile of monitors and CPUs beside office park",
            "Furniture abandoned near housing complex",
            "Yard waste clogging storm drain after storm",
            "Plastic bottles scattered across beach boardwalk",
            "Truck unloading debris outside permitted zone",
            "Dumpster overflowing with cardboard and paper",
            "Batteries and phones discarded behind electronics shop",
            "Expired pharmaceuticals dumped behind pharmacy",
            "Restaurant grease leaking onto sidewalk",
            "Blue recycling carts full of glass jars",
            "Cafe tossing spoiled produce behind building",
            "Household trash bags thrown over park fence",
            "Laptop screens and cables near loading dock",
            "Abandoned copier and fax machines in lobby",
            "Old sofa dumped beside parking lot",
            "Demolition rubble obstructing sidewalk",
            "Contractor dumping debris off highway ramp",
            "Metal beams and bricks left beside trail",
            "Pesticide containers found near community garden",
            "Mercury thermometer broken on street",
            "Glow sticks and lab materials leaking in trash",
            "Curbside bins overflowing with paper and cans",
            "Grocery store dumpster full of rotten fruit",
            "Park littered with branches and leaves",
            "Smartphones mixed with household trash",
            "Server racks abandoned behind data center",
            "Holiday tree pile blocking community garden gate",
            "Spoiled meat leaking from grocery compactor",
            "Hazardous solvent drums rusting on pier",
            "Contractor disposing rubble in public park",
            "Dumpster contents spread across alley overnight",
            "Trash trailers emptying along rural roadside",
            "Large tree limbs stacked beside bike path",
            "Plastic packaging blowing across parking lot",
        };
}
