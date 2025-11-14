using System.Globalization;
using CsvHelper;
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
        public string Text { get; set; } = string.Empty;
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

        var seedCorpus = LoadCorpusFromCsv().ToList();
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
            var data = _ml.Data.LoadFromEnumerable(
                new[] { new Input { Text = text ?? string.Empty } }
            );
            var transformed = _model.Transform(data);
            var vector = _ml
                .Data.CreateEnumerable<Output>(transformed, reuseRowObject: false)
                .First();
            return vector.Features;
        }
    }

    private static IEnumerable<string> LoadCorpusFromCsv()
    {
        var path = ResolveEmbeddingCsvPath();
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            if (!csv.Read())
            {
                return Array.Empty<string>();
            }

            csv.ReadHeader();
            var rows = new List<string>();
            while (csv.Read())
            {
                var text = csv.GetField("text");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    rows.Add(text.Trim());
                }
            }

            return rows;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string ResolveEmbeddingCsvPath() =>
        Path.Combine(AppContext.BaseDirectory, "db", "text_embedding_corpus.csv");

 
}
