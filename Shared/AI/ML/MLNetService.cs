namespace Shared.AI.ML;

using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

/// <summary>
/// ML.NET integration for machine learning tasks.
/// Provides helpers for training, prediction, and model management.
/// </summary>
public class MLNetService
{
    private readonly MLContext _mlContext;
    private readonly ILogger? _logger;

    public MLNetService(ILogger? logger = null)
    {
        _mlContext = new MLContext();
        _logger = logger;
    }

    /// <summary>
    /// Gets the underlying ML context.
    /// </summary>
    public MLContext Context => _mlContext;

    /// <summary>
    /// Creates a text classification pipeline.
    /// </summary>
    public IEstimator<ITransformer> CreateTextClassificationPipeline<TInput, TOutput>()
        where TInput : class
        where TOutput : class
    {
        _logger?.LogDebug("Creating text classification pipeline");

        return _mlContext.Transforms.Text.FeaturizeText(
            outputColumnName: "Features",
            inputColumnName: nameof(TextClassificationInput.Text))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(TextClassificationOutput.Label),
                featureColumnName: "Features"));
    }

    /// <summary>
    /// Trains a regression model.
    /// </summary>
    public ITransformer TrainRegressionModel(
        IDataView trainingData,
        string labelColumnName,
        string featureColumnName)
    {
        _logger?.LogDebug("Training regression model");

        var pipeline = _mlContext.Transforms.Concatenate(
                outputColumnName: "Features",
                inputColumnNames: featureColumnName)
            .Append(_mlContext.Regression.Trainers.FastTree());

        return pipeline.Fit(trainingData);
    }

    /// <summary>
    /// Trains a clustering model using K-Means.
    /// </summary>
    public ITransformer TrainClusteringModel(
        IDataView trainingData,
        string featureColumnName,
        int numberOfClusters = 3)
    {
        _logger?.LogDebug("Training clustering model with {Clusters} clusters", numberOfClusters);

        var pipeline = _mlContext.Transforms.Concatenate(
                outputColumnName: "Features",
                inputColumnNames: featureColumnName)
            .Append(_mlContext.Clustering.Trainers.KMeans(
                numberOfClusters: numberOfClusters,
                featureColumnName: "Features"));

        return pipeline.Fit(trainingData);
    }

    /// <summary>
    /// Creates a prediction engine for a trained model.
    /// </summary>
    public PredictionEngine<TInput, TOutput> CreatePredictionEngine<TInput, TOutput>(
        ITransformer model)
        where TInput : class
        where TOutput : class, new()
    {
        _logger?.LogDebug("Creating prediction engine");
        return _mlContext.Model.CreatePredictionEngine<TInput, TOutput>(model);
    }

    /// <summary>
    /// Loads a model from file.
    /// </summary>
    public ITransformer LoadModel(string modelPath)
    {
        if (!System.IO.File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");

        _logger?.LogDebug("Loading model from {Path}", modelPath);

        using var stream = System.IO.File.OpenRead(modelPath);
        return _mlContext.Model.Load(stream, out _);
    }

    /// <summary>
    /// Saves a model to file.
    /// </summary>
    public void SaveModel(ITransformer model, IDataView trainingData, string modelPath)
    {
        _logger?.LogDebug("Saving model to {Path}", modelPath);

        using var stream = System.IO.File.Create(modelPath);
        _mlContext.Model.Save(model, trainingData.Schema, stream);
    }

    /// <summary>
    /// Evaluates a classification model.
    /// </summary>
    public BinaryClassificationMetrics EvaluateClassificationModel(
        ITransformer model,
        IDataView testData,
        string labelColumnName)
    {
        _logger?.LogDebug("Evaluating classification model");

        var predictions = model.Transform(testData);
        return _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: labelColumnName);
    }

    /// <summary>
    /// Evaluates a regression model.
    /// </summary>
    public RegressionMetrics EvaluateRegressionModel(
        ITransformer model,
        IDataView testData,
        string labelColumnName)
    {
        _logger?.LogDebug("Evaluating regression model");

        var predictions = model.Transform(testData);
        return _mlContext.Regression.Evaluate(
            predictions,
            labelColumnName: labelColumnName);
    }
}

/// <summary>
/// Input class for text classification.
/// </summary>
public class TextClassificationInput
{
    /// <summary>
    /// The text to classify.
    /// </summary>
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The label.
    /// </summary>
    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Output class for text classification.
/// </summary>
public class TextClassificationOutput
{
    /// <summary>
    /// The predicted label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The score.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Prediction probability.
    /// </summary>
    public float Probability { get; set; }
}

/// <summary>
/// Helper for creating datasets from various sources.
/// </summary>
public class DatasetBuilder
{
    private readonly MLContext _mlContext;
    private readonly ILogger? _logger;

    public DatasetBuilder(MLContext mlContext, ILogger? logger = null)
    {
        _mlContext = mlContext;
        _logger = logger;
    }

    /// <summary>
    /// Loads data from a CSV file.
    /// </summary>
    public IDataView LoadFromCsv<T>(
        string filePath,
        bool hasHeader = true,
        char separatorChar = ',')
        where T : class
    {
        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        _logger?.LogDebug("Loading data from CSV: {Path}", filePath);

        return _mlContext.Data.LoadFromTextFile<T>(
            filePath,
            hasHeader: hasHeader,
            separatorChar: separatorChar);
    }

    /// <summary>
    /// Creates a data view from in-memory data.
    /// </summary>
    public IDataView LoadFromEnumerable<T>(IEnumerable<T> data)
        where T : class
    {
        _logger?.LogDebug("Creating data view from enumerable");
        return _mlContext.Data.LoadFromEnumerable(data);
    }

    /// <summary>
    /// Splits data into training and test sets.
    /// </summary>
    public (IDataView trainingData, IDataView testData) TrainTestSplit(
        IDataView data,
        double testFraction = 0.2)
    {
        _logger?.LogDebug("Splitting data: {TrainFraction}% training, {TestFraction}% testing",
            (1 - testFraction) * 100, testFraction * 100);

        var split = _mlContext.Data.TrainTestSplit(data, testFraction: testFraction);
        return (split.TrainSet, split.TestSet);
    }

    /// <summary>
    /// Applies feature normalization.
    /// </summary>
    public IDataView NormalizeFeatures(
        IDataView data,
        string featureColumnName)
    {
        _logger?.LogDebug("Normalizing features: {FeatureColumn}", featureColumnName);

        var normalizer = _mlContext.Transforms.Conversion.ConvertType(
                outputColumnName: featureColumnName,
                outputKind: DataKind.Single,
                inputColumnName: featureColumnName)
            .Fit(data);

        return normalizer.Transform(data);
    }
}

/// <summary>
/// Feature engineering utilities.
/// </summary>
public static class FeatureEngineering
{
    /// <summary>
    /// Performs one-hot encoding for categorical features.
    /// </summary>
    public static IEstimator<ITransformer> OneHotEncode(
        this MLContext mlContext,
        IEstimator<ITransformer> pipeline,
        params string[] columnNames)
    {
        return columnNames.Aggregate(
            pipeline,
            (p, col) => p.Append(mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: col + "Encoded",
                inputColumnName: col)));
    }

    /// <summary>
    /// Extracts text features using TF-IDF.
    /// </summary>
    public static IEstimator<ITransformer> ExtractTfidf(
        this MLContext mlContext,
        IEstimator<ITransformer> pipeline,
        string inputColumnName,
        string outputColumnName = "TfidfFeatures")
    {
        return pipeline
            .Append(mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: outputColumnName,
                inputColumnName: inputColumnName));
    }

    /// <summary>
    /// Normalizes numeric columns.
    /// </summary>
    public static IEstimator<ITransformer> NormalizeColumns(
        this MLContext mlContext,
        IEstimator<ITransformer> pipeline,
        params string[] columnNames)
    {
        return pipeline.Append(mlContext.Transforms.NormalizeMinMax(columnNames));
    }
}
