﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    public partial class NetworkPresenter : UserControl
    {
        private const int NEURON_MAX_DIST = 40;
        private const int HORIZONTAL_OFFSET = 10;
        private const int TOP_OFFSET = 10;
        private const int BOTTOM_OFFSET = 25;
        private const int NEURON_SIZE = 8;
        private const double NEURON_RADIUS = NEURON_SIZE / 2;
        private const int BIAS_SIZE = 14;
        private const double BIAS_RADIUS = BIAS_SIZE / 2;

        public long ResizeTicks;

        private readonly Dictionary<NeuronDataModel, Point> _coordinator = new Dictionary<NeuronDataModel, Point>();
        private readonly Dictionary<WeightDataModel, double> _prevWeights = new Dictionary<WeightDataModel, double>();

        private readonly Pen _penChange = Tools.Draw.GetPen(Colors.Lime);
        private readonly Pen _biasPen = Tools.Draw.GetPen(Colors.Orange);

        public NetworkPresenter()
        {
            InitializeComponent();
            
            _penChange.Freeze();
            _biasPen.Freeze();

            SizeChanged += NetworkPresenter_SizeChanged;
        }

        private void NetworkPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClearCache();
        }

        public int LayerDistance(NetworkDataModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return (int)(ActualWidth - 2 * HORIZONTAL_OFFSET) / (model.Layers.Count - 1);
        }

        private float VerticalDistance(int count)
        {
            return Math.Min(((float)ActualHeight - TOP_OFFSET - BOTTOM_OFFSET) / count, NEURON_MAX_DIST);
        }

        private int LayerX(NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            return HORIZONTAL_OFFSET + LayerDistance(networkModel) * layerModel.Id;
        }

        private new float MaxHeight(NetworkDataModel networkModel)
        {
            return networkModel.Layers.Max(layer => layer.NeuronsCount * VerticalDistance(layer.NeuronsCount));
        }

        private float VerticalShift(NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            return (MaxHeight(networkModel) - layerModel.NeuronsCount * VerticalDistance(layerModel.NeuronsCount)) / 2;
        }

        private void DrawLayersLinks(bool fullState, NetworkDataModel networkModel, LayerDataModel layerModel1, LayerDataModel layerModel2,
                                     bool isUseColorOfWeight, bool isShowOnlyChangedWeights, bool isHighlightChangedWeights, bool isShowOnlyUnchangedWeights)
        {
            double threshold = networkModel.Layers[0] == layerModel1 ? networkModel.InputThreshold : 0;

            NeuronDataModel prevNeuronModel = null;
            foreach (var neuronModel1 in layerModel1.Neurons)
            {
                if (!_coordinator.ContainsKey(neuronModel1))
                {
                    _coordinator.Add(neuronModel1, Points.Get(LayerX(networkModel, layerModel1), TOP_OFFSET + VerticalShift(networkModel, layerModel1) + neuronModel1.Id * VerticalDistance(layerModel1.NeuronsCount)));
                }

                // Skip intersected neurons on first layer to improove performance.
                if (!fullState && !neuronModel1.IsBias && networkModel.Layers[0] == layerModel1 && prevNeuronModel != null)
                {
                    if (_coordinator[neuronModel1].Y - _coordinator[prevNeuronModel].Y < NEURON_SIZE)
                    {
                        continue;
                    }
                }

                if (fullState || neuronModel1.IsBias || neuronModel1.Activation > threshold)
                {
                    foreach (var neuronModel2 in layerModel2.Neurons)
                    {
                        if (!neuronModel2.IsBias || (neuronModel2.IsBiasConnected && neuronModel1.IsBias))
                        {
                            if (fullState || ((neuronModel1.IsBias || neuronModel1.Activation > threshold) && neuronModel1.AxW(neuronModel2) != 0))
                            {
                                Pen pen = null;

                                bool isWeightChanged = false;
                                var weightModel = neuronModel1.WeightTo(neuronModel2);

                                if (_prevWeights.TryGetValue(weightModel, out double prevWeight))
                                {
                                    if (prevWeight != weightModel.Weight)
                                    {
                                        _prevWeights[weightModel] = weightModel.Weight;
                                        isWeightChanged = true;
                                    }
                                }
                                else
                                {
                                    prevWeight = 0;
                                    _prevWeights.Add(weightModel, weightModel.Weight);
                                    isWeightChanged = true;
                                }

                                if (isWeightChanged && isShowOnlyChangedWeights)
                                {
                                    double changeFraction = (prevWeight - weightModel.Weight) / prevWeight;
                                    if (changeFraction < 0.00001 && changeFraction > -0.00001)
                                    {
                                        isWeightChanged = false;
                                    }
                                }

                                if (isShowOnlyChangedWeights)
                                {
                                    if (isWeightChanged)
                                    {
                                        if (!isShowOnlyUnchangedWeights)
                                        {
                                            if (isHighlightChangedWeights)
                                            {
                                                pen = _penChange;
                                            }
                                            else
                                            {
                                                if (isUseColorOfWeight)
                                                {
                                                    pen = Tools.Draw.GetPen(weightModel.Weight, 1);
                                                }
                                                else
                                                {
                                                    pen = Tools.Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (isUseColorOfWeight)
                                        {
                                            pen = Tools.Draw.GetPen(weightModel.Weight, 1);
                                        }
                                        else
                                        {
                                            pen = Tools.Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                        }
                                    }
                                }
                                else // show all weights
                                {
                                    if (isShowOnlyUnchangedWeights)
                                    {
                                        if (!isWeightChanged)
                                        {
                                            if (isUseColorOfWeight)
                                            {
                                                pen = Tools.Draw.GetPen(weightModel.Weight, 1);
                                            }
                                            else
                                            {
                                                pen = Tools.Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (isWeightChanged)
                                        {
                                            if (isHighlightChangedWeights)
                                            {
                                                pen = _penChange;
                                            }
                                        }

                                        if (pen == null)
                                        {
                                            if (isUseColorOfWeight)
                                            {
                                                pen = Tools.Draw.GetPen(weightModel.Weight, 1);
                                            }
                                            else
                                            {
                                                pen = Tools.Draw.GetPen(neuronModel1.AxW(neuronModel2), 1);
                                            }
                                        }
                                    }
                                }

                                if (pen != null)
                                {
                                    if (!_coordinator.ContainsKey(neuronModel2))
                                    {
                                        _coordinator.Add(neuronModel2, Points.Get(LayerX(networkModel, layerModel2), TOP_OFFSET + VerticalShift(networkModel, layerModel2) + neuronModel2.Id * VerticalDistance(layerModel2.NeuronsCount)));
                                    }

                                    var point1 = _coordinator[neuronModel1];
                                    var point2 = _coordinator[neuronModel2];

                                    CtlPresenter.DrawLine(pen, ref point1, ref point2);
                                    prevNeuronModel = neuronModel1;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawLayerNeurons(bool fullState, NetworkDataModel networkModel, LayerDataModel layerModel)
        {
            double threshold = networkModel.Layers[0] == layerModel ? networkModel.InputThreshold : 0;

            NeuronDataModel prevNeuron = null;
            foreach (var neuronModel in layerModel.Neurons)
            {
                if (fullState || neuronModel.IsBias || neuronModel.Activation > threshold || layerModel.Id > 0)
                {                   
                    if (!_coordinator.ContainsKey(neuronModel))
                    {
                        _coordinator.Add(neuronModel,
                                         Points.Get(LayerX(networkModel, layerModel),
                                         TOP_OFFSET + VerticalShift(networkModel, layerModel) + neuronModel.Id * VerticalDistance(layerModel.NeuronsCount)));
                    }

                    // Skip intersected neurons on first layer to improove performance.
                    if (!fullState && !neuronModel.IsBias && networkModel.Layers[0] == layerModel && prevNeuron != null)
                    {
                        if (_coordinator[neuronModel].Y - _coordinator[prevNeuron].Y < NEURON_SIZE)
                        {
                            continue;
                        }
                    }
                    prevNeuron = neuronModel;

                    var pen = Tools.Draw.GetPen(neuronModel.Activation);
                    var brush = pen.Brush;

                    var centerPoint = _coordinator[neuronModel];

                    if (neuronModel.IsBias)
                    {
                        CtlPresenter.DrawEllipse(Brushes.Orange, _biasPen, ref centerPoint, BIAS_RADIUS, BIAS_RADIUS);
                    }

                    CtlPresenter.DrawEllipse(brush, pen, ref centerPoint, NEURON_RADIUS, NEURON_RADIUS);  
                }
            }
        }

        private void Draw(bool fullState, NetworkDataModel networkModel, bool isUseWeightsColors, bool isOnlyChangedWeights, bool isHighlightChangedWeights, bool isShowOnlyUnchangedWeights)
        {
            CtlPresenter.Clear();

            if (networkModel == null)
            {
                return;
            }

            //lock (Main.ApplyChangesLocker)
            {
                if (networkModel.Layers.Count > 0)
                {
                    var lastLayerModel = networkModel.Layers.Last;

                    foreach (var layerModel in networkModel.Layers)
                    {
                        if (layerModel == lastLayerModel)
                        {
                            break;
                        }

                        DrawLayersLinks(fullState, networkModel, layerModel, layerModel.Next, isUseWeightsColors, isOnlyChangedWeights, isHighlightChangedWeights, isShowOnlyUnchangedWeights);
                    }

                    foreach (var layerModel in networkModel.Layers)
                    {
                        DrawLayerNeurons(fullState, networkModel, layerModel);
                    }
                }
            }

            CtlPresenter.Update();
        }

        public void ClearCache()
        {
            _coordinator.Clear();
        }

        public void RenderStanding(NetworkDataModel networkModel)
        {
            ClearCache();
            Draw(false, networkModel, false, false, false, false);
        }

        public void RenderRunning(NetworkDataModel networkModel, bool isUseWeightsColors, bool isOnlyChangedWeights, bool isHighlightChangedWeights, bool isShowOnlyUnchangedWeights)
        {
            Draw(false, networkModel, isUseWeightsColors, isOnlyChangedWeights, isHighlightChangedWeights, isShowOnlyUnchangedWeights);
        }
    }
}
