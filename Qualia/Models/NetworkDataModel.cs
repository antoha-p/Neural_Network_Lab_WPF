﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using Qualia.Controls;
using Tools;

namespace Qualia
{
    public class NetworkDataModel : ListNode<NetworkDataModel>
    {
        public long VisualId;
        public ListX<LayerDataModel> Layers;
        public int TargetOutput;
        public List<string> Classes;

        public bool IsEnabled;

        public Color Color;
        public double LearningRate;
        public string RandomizeMode;
        public double? RandomizerParamA;
        public double InputInitial0;
        public double InputInitial1;
        public bool IsAdjustFirstLayerWeights;

        public ICostFunction CostFunction;

        public Statistics Statistics;
        public DynamicStatistics DynamicStatistics;
        public ErrorMatrix ErrorMatrix;
        public Dictionary<string, string> LastStatistics;

        private NetworkDataModel _copy;

        public NetworkDataModel(long visualId, int[] layerSizes)
        {
            VisualId = visualId;
            Layers = new ListX<LayerDataModel>(layerSizes.Length);
            Range.For(layerSizes.Length, ind => CreateLayer(layerSizes[ind], ind < layerSizes.Length - 1 ? layerSizes[ind + 1] : 0));
        }

        public void SetCopy(NetworkDataModel networkModelCopy)
        {
            _copy = networkModelCopy;
        }

        public void CreateLayer(int neuronCount, int weightsCount)
        {
            Layers.Add(new LayerDataModel(Layers.Count, neuronCount, weightsCount));
        }

        public double InputThreshold => (InputInitial0 + InputInitial1) / 2;

        public NeuronDataModel GetMaxActivatedOutputNeuron()
        {
            var neuronsModel = Layers.Last.Neurons;
            var maxNeuronModel = neuronsModel.First;
            var neuronModel = maxNeuronModel;

            while (neuronModel != null)
            {
                if (neuronModel.Activation > maxNeuronModel.Activation)
                {
                    maxNeuronModel = neuronModel;
                }

                neuronModel = neuronModel.Next;
            };

            return maxNeuronModel;
        }

        public void ActivateFirstLayer()
        {
            Layers.First.Neurons.ForEach(neuronModel => neuronModel.Activation = InputInitial1);
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParamA);
        }

        public void ActivateNetwork()
        {
            Layers.ForEach(layer => layer.Neurons.ForEach(neuronModel => neuronModel.Activation = InputInitial1));
            Tools.RandomizeMode.Helper.Invoke(RandomizeMode, this, RandomizerParamA);
        }

        public void FeedForward2()
        {
            var layerModel = Layers.First;
            while (layerModel != Layers.Last)
            {
                var nextNeuron = layerModel.Next.Neurons.First;
                while (nextNeuron != null)
                {
                    if (nextNeuron.IsBiasConnected && nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        var neuron = layerModel.Neurons.First;
                        while (neuron != null)
                        {
                            if (neuron.IsBias)
                            {
                                nextNeuron.Activation += neuron.AxW(nextNeuron);
                            }
                            neuron = neuron.Next;
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParamA);
                    }

                    if (!nextNeuron.IsBias)
                    {
                        nextNeuron.Activation = 0;

                        var neuron = layerModel.Neurons.First;
                        while (neuron != null)
                        {
                            if (neuron.Activation != 0)
                            {
                                if (neuron.Activation == 1)
                                {
                                    nextNeuron.Activation += neuron.WeightTo(nextNeuron).Weight;
                                }
                                else
                                {
                                    nextNeuron.Activation += neuron.AxW(nextNeuron);
                                }
                            }
                            neuron = neuron.Next;
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(nextNeuron.Activation, nextNeuron.ActivationFuncParamA);
                    }

                    nextNeuron = nextNeuron.Next;
                }

                // not connected bias doesn't change it's activation

                layerModel = layerModel.Next;
            }
        }

        unsafe public void FeedForward()
        {
            var layerModel = Layers.First;
            var lastLayerModel = Layers.Last;
            double sum;

            while (layerModel != lastLayerModel)
            {
                var neuronModel = layerModel.Next.Neurons.First;
                while (neuronModel != null)
                {
                    sum = 0;
                    var AxW = neuronModel.ForwardHelper.First;
                    while (AxW != null)
                    {
                        sum += AxW.AxW;
                        AxW = AxW.Next;
                    }

                    neuronModel.Activation = neuronModel.ActivationFunction.Do(sum, neuronModel.ActivationFuncParamA);
                    neuronModel = neuronModel.Next;
                }

                layerModel = layerModel.Next;
            }
        }

        unsafe public void BackPropagation()
        {
            var lastLayerModel = Layers.Last;
            var neuronModel = lastLayerModel.Neurons.First;

            while (neuronModel != null)
            {
                neuronModel.Error = CostFunction.Derivative(this, neuronModel) * neuronModel.ActivationFunction.Derivative(neuronModel.Activation, neuronModel.ActivationFuncParamA);
                neuronModel = neuronModel.Next;
            }

            var layerModel = lastLayerModel;
            var finalLayerModel = Layers[IsAdjustFirstLayerWeights ? 0 : 1];

            while (layerModel != finalLayerModel)
            {
                neuronModel = layerModel.Neurons.First;
                while (neuronModel != null)
                {
                    var AxW = neuronModel.ForwardHelper.First;
                    while (AxW != null)
                    {
                        var prevNeuron = AxW.Neuron;
                        if (prevNeuron.Activation != 0)
                        {
                            prevNeuron.Error += neuronModel.Error * AxW.WeightModel.Weight * prevNeuron.ActivationFunction.Derivative(prevNeuron.Activation, prevNeuron.ActivationFuncParamA);
                        }

                        AxW = AxW.Next;
                    }

                    neuronModel = neuronModel.Next;
                }

                layerModel = layerModel.Previous;
            }

            // update weights

            layerModel = lastLayerModel;
            while (layerModel != finalLayerModel)
            {
                neuronModel = layerModel.Neurons.First;
                while (neuronModel != null)
                {
                    if (neuronModel.Error != 0)
                    {
                        var AxW = neuronModel.ForwardHelper.First;
                        while (AxW != null)
                        {
                            if (AxW.Neuron.Activation != 0)
                            {
                                AxW.WeightModel.Weight += neuronModel.Error * AxW.Neuron.Activation * LearningRate;
                            }

                            AxW = AxW.Next;
                        }

                        neuronModel.Error = 0;
                    }

                    neuronModel = neuronModel.Next;
                }

                layerModel = layerModel.Previous;
            }
        }

        unsafe public void BackPropagation2()
        {
            var lastLayer = Layers.Last;
            var neuron = lastLayer.Neurons.First;

            while (neuron != null)
            {
                neuron.Error = CostFunction.Derivative(this, neuron) * neuron.ActivationFunction.Derivative(neuron.Activation, neuron.ActivationFuncParamA);
                neuron = neuron.Next;
            }

            var layer = lastLayer;
            var finalLayer = Layers[IsAdjustFirstLayerWeights ? 0 : 1];

            while (layer != finalLayer)
            {
                var neuronPrev = layer.Previous.Neurons.First;
                while (neuronPrev != null)
                {
                    neuronPrev.Error = 0;

                    neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        if (neuronPrev.IsBias)
                        {
                            if (neuron.IsBiasConnected)
                            {
                                neuronPrev.Error += neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA);
                            }
                        }
                        else
                        {
                            neuronPrev.Error += neuron.Error * neuronPrev.WeightTo(neuron).Weight * neuronPrev.ActivationFunction.Derivative(neuronPrev.Activation, neuronPrev.ActivationFuncParamA);
                        }

                        neuron = neuron.Next;
                    }

                    neuronPrev = neuronPrev.Next;
                }

                layer = layer.Previous;
            }

            // update weights

            layer = lastLayer;
            while (layer != finalLayer)
            {
                var neuronPrev = layer.Previous.Neurons.First;
                while (neuronPrev != null)
                {
                    neuron = layer.Neurons.First;
                    while (neuron != null)
                    {
                        if (neuronPrev.Activation != 0)
                        {
                            if (neuronPrev.Activation == 1)
                            {
                                neuronPrev.WeightTo(neuron).Add(neuron.Error * LearningRate);
                            }
                            else
                            {
                                neuronPrev.WeightTo(neuron).Add(neuron.Error * neuronPrev.Activation * LearningRate);
                            }
                        }

                        neuron = neuron.Next;
                    }

                    neuronPrev = neuronPrev.Next;
                }

                layer = layer.Previous;
            }
        }

        public NetworkDataModel Merge(NetworkDataModel newNetworkModel)
        {
            newNetworkModel.Statistics = Statistics;
            newNetworkModel.DynamicStatistics = DynamicStatistics;

            foreach (var newLayerModel in newNetworkModel.Layers)
            {
                var layerModel = Layers.Find(l => l.VisualId == newLayerModel.VisualId);
                if (layerModel != null)
                {
                    foreach (var newNeuronModel in newLayerModel.Neurons)
                    {
                        var neuronModel = layerModel.Neurons.Find(n => n.VisualId == newNeuronModel.VisualId);
                        if (neuronModel == null)
                        {
                            double initValue = InitializeMode.Helper.Invoke(newNeuronModel.WeightsInitializer, newNeuronModel.WeightsInitializerParamA);
                            if (!InitializeMode.Helper.IsSkipValue(initValue))
                            {
                                foreach (var newWeightModel in newNeuronModel.Weights)
                                {
                                    newWeightModel.Weight = initValue;
                                }
                            }
                        }
                        else
                        {
                            foreach (var newWeightModel in newNeuronModel.Weights)
                            {
                                var weightModel = neuronModel.Weights.Find(w => w.Id == newWeightModel.Id);
                                if (weightModel != null)
                                {
                                    newWeightModel.Weight = weightModel.Weight;
                                }
                            }

                            if (newNeuronModel.IsBias)
                            {
                                double initValue = InitializeMode.Helper.Invoke(newNeuronModel.ActivationInitializer, newNeuronModel.ActivationInitializerParamA);
                                if (InitializeMode.Helper.IsSkipValue(initValue))
                                {
                                    newNeuronModel.Activation = neuronModel.Activation;
                                }
                            }
                        }
                    }
                }
            }

            return newNetworkModel;
        }

        public NetworkDataModel GetCopyForRender()
        {
            var layerModel = Layers.First;
            var layerModelCopy = _copy.Layers.First;

            while (layerModel != null)
            {
                var neuronModel = layerModel.Neurons.First;
                var neuronModelCopy = layerModelCopy.Neurons.First;

                while (neuronModel != null)
                {
                    neuronModelCopy.Activation = neuronModel.Activation;

                    if (neuronModel.Activation != 0 && layerModel != Layers.Last)
                    {
                        var weightModel = neuronModel.Weights.First;
                        var weightModelCopy = neuronModelCopy.Weights.First;

                        while (weightModel != null)
                        {
                            weightModelCopy.Weight = weightModel.Weight;

                            weightModel = weightModel.Next;
                            weightModelCopy = weightModelCopy.Next;
                        }
                    }

                    neuronModel = neuronModel.Next;
                    neuronModelCopy = neuronModelCopy.Next;
                }

                layerModel = layerModel.Next;
                layerModelCopy = layerModelCopy.Next;
            }

            return _copy;
        }
    }
}
