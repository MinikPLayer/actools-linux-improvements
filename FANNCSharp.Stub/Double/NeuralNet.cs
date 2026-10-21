using System;
using FANNCSharp;

namespace FANNCSharp.Double;

public class NeuralNet : IDisposable {
    public NeuralNet(NetworkType networkType, uint numLayers, uint[] layers) { }

    public NeuralNet(string filename) { }

    public float LearningRate { get; set; }

    public float LearningMomentum { get; set; }

    public TrainingAlgorithm TrainingAlgorithm { get; set; }

    public ActivationFunction ActivationFunctionHidden { get; set; }

    public ActivationFunction ActivationFunctionOutput { get; set; }

    public void RandomizeWeights(double min, double max) { }

    public void TrainEpoch(TrainingData data) { }

    public double[] Run(double[] input) {
        return Array.Empty<double>();
    }

    public void Save(string filename) { }

    public void Dispose() { }
}
