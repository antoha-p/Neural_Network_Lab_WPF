﻿using System.Runtime.CompilerServices;
using Tools;

namespace Qualia
{
    public class NeuronDataModel : ListXNode<NeuronDataModel>
    {
        public int Id;
        public long VisualId;

        public double Activation;
        public double Error;

        public bool IsBias;
        public bool IsBiasConnected;

        public string ActivationInitializer;
        public double? ActivationInitializerParamA;

        public string WeightsInitializer;
        public double? WeightsInitializerParamA;

        public IActivationFunction ActivationFunction;
        public double? ActivationFuncParamA;

        public ListX<WeightDataModel> Weights;

        public double Target;

        public ListX<ForwardNeuron> ForwardHelper;

        public NeuronDataModel(int id, int weightsCount)
        {
            Weights = new ListX<WeightDataModel>(weightsCount);
            Id = id;
            Range.For(weightsCount, ind => Weights.Add(new WeightDataModel(ind)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AxW(NeuronDataModel neuronModel) => Activation * WeightTo(neuronModel).Weight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeightDataModel WeightTo(NeuronDataModel neuronModel) => Weights[neuronModel.Id];
    }

    public class WeightDataModel : ListXNode<WeightDataModel>
    {
        public int Id;
        public double Weight;

        public WeightDataModel(int id)
        {
            Id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(double weight) => Weight += weight;
    }
}
