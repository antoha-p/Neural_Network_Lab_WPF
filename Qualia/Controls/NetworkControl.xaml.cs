﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Tools;

namespace Qualia.Controls
{
    sealed public partial class NetworkControl : System.Windows.Controls.UserControl
    {
        public readonly long Id;
        public Config Config;
        
        private readonly Action<Notification.ParameterChanged> OnNetworkUIChanged;
        private readonly List<IConfigParam> _configParams;
        private OutputLayerControl _outputLayer;

        public InputLayerControl InputLayer { get; private set; }

        public bool IsNetworkEnabled => CtlIsNetworkEnabled.IsOn;

        public NetworkControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
        {
            InitializeComponent();
            OnNetworkUIChanged = onNetworkUIChanged;

            Id = UniqId.GetNextId(id);
            Config = config.Extend(Id);

            _configParams = new()
            {
                CtlRandomizeModeParam,
                CtlRandomizeMode,
                CtlLearningRate,
                CtlIsNetworkEnabled,
                CtlCostFunction
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public void AddLayer()
        {
            AddLayer(Constants.UnknownId);
        }

        private void AddLayer(long layerId)
        {
            HiddenLayerControl ctlLayer = new(layerId, Config, OnNetworkUIChanged);

            ScrollViewer ctlScroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = ctlLayer
            };
            ctlScroll.ScrollChanged += ctlLayer.OnScrollChanged;

            TabItem tabItem = new()
            {
                Content = ctlScroll
            };

            CtlTabsLayers.Items.Insert(CtlTabsLayers.Items.Count - 1, tabItem);
            CtlTabsLayers.SelectedItem = tabItem;
            ResetLayersTabsNames();

            if (layerId == Constants.UnknownId)
            {
                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        public void ResetLayersTabsNames()
        {
            var ctlLayers = GetLayersControls();
            for (int i = 0; i < ctlLayers.Count; ++i)
            {
                if (ctlLayers[i].IsInput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Input ({ctlLayers[i].NeuronsCount})";
                }
                else if (ctlLayers[i].IsOutput)
                {
                    CtlTabsLayers.Tab(i).Header = $"Output ({ctlLayers[i].NeuronsCount})";
                }
                else
                {
                    CtlTabsLayers.Tab(i).Header = $"L{i} ({ctlLayers[i].NeuronsCount})";
                }
            }

            // The code below is needed to refresh Tabcontrol.
            // Without it newly added neuron control is not visible for hit test (some WPF issue).

            int selectedIndex = CtlTabsLayers.SelectedIndex;
            CtlTabsLayers.SelectedIndex = 0;
            CtlTabsLayers.SelectedIndex = selectedIndex;
        }

        public bool IsValid()
        {
            return _configParams.All(param => param.IsValid()) && GetLayersControls().All(control => control.IsValid());
        }

        public void SaveConfig()
        {
            Config.Set(Constants.Param.SelectedLayerIndex, CtlTabsLayers.SelectedIndex);
            Config.Set(Constants.Param.Color, $"{CtlColor.Foreground.GetColor().A},{CtlColor.Foreground.GetColor().R},{CtlColor.Foreground.GetColor().G},{CtlColor.Foreground.GetColor().B}");
            _configParams.ForEach(param => param.SaveConfig());

            var ctlLayers = GetLayersControls();
            ctlLayers.ForEach(ctlLayer => ctlLayer.SaveConfig());
            Config.Set(Constants.Param.Layers, ctlLayers.Select(ctlLayer => ctlLayer.Id));

            //

            ResetLayersTabsNames();
        }

        public void OnTaskChanged(INetworkTask networkTask)
        {
            InputLayer.OnTaskChanged(networkTask);
            _outputLayer.OnTaskChanged(networkTask);
        }

        public void VanishConfig()
        {
            Config.Remove(Constants.Param.SelectedLayerIndex);
            Config.Remove(Constants.Param.Color);

            _configParams.ForEach(param => param.VanishConfig());

            GetLayersControls().ForEach(ctlLayer => ctlLayer.VanishConfig());
            Config.Remove(Constants.Param.Layers);
        }

        private List<LayerBase> GetLayersControls()
        {
            List<LayerBase> ctlLayers = new(CtlTabsLayers.Items.Count);
            for (int i = 0; i < CtlTabsLayers.Items.Count; ++i)
            {
                ctlLayers.Add(CtlTabsLayers.Tab(i).FindVisualChildren<LayerBase>().First());
            }

            return ctlLayers;
        }

        private void LoadConfig()
        {
            RandomizeModeList.Helper.FillComboBox(CtlRandomizeMode, Config, nameof(RandomizeModeList.FlatRandom));
            CostFunctionList.Helper.FillComboBox(CtlCostFunction, Config, nameof(CostFunctionList.MSE));

            _configParams.ForEach(param => param.LoadConfig());

            var color = Config.GetArray(Constants.Param.Color, "255,100,100,100");
            CtlColor.Foreground = Tools.Draw.GetBrush(Color.FromArgb((byte)color[0],
                                                                     (byte)color[1],
                                                                     (byte)color[2],
                                                                     (byte)color[3]));

            //

            var layerIds = Config.GetArray(Constants.Param.Layers);
            var inputLayerId = layerIds.Length > 0 ? layerIds[0] : Constants.UnknownId;
            var outputLayerId = layerIds.Length > 0 ? layerIds[layerIds.Length - 1] : Constants.UnknownId;

            InputLayer = new(inputLayerId, Config, OnNetworkUIChanged);
            ScrollViewer ctlScroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = InputLayer
            };

            CtlTabInput.Content = ctlScroll;
            ctlScroll.ScrollChanged += InputLayer.OnScrollChanged;

            _outputLayer = new(outputLayerId, Config, OnNetworkUIChanged);
            ctlScroll = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _outputLayer
            };

            CtlTabOutput.Content = ctlScroll;
            ctlScroll.ScrollChanged += _outputLayer.OnScrollChanged;

            var lastLayerId = layerIds.Last();

            foreach (var layerId in layerIds)
            {
                if (layerId != layerIds[0] && layerId != lastLayerId)
                {
                    AddLayer(layerId);
                }
            }

            CtlTabsLayers.SelectedIndex = (int)Config.GetInt(Constants.Param.SelectedLayerIndex, 0).Value;
        }

        public int[] GetLayersSizes()
        {
            return GetLayersControls().Select(ctlLayer => ctlLayer.NeuronsCount).ToArray();
        }

        public LayerBase SelectedLayer => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First();

        public Type SelectedLayerType => CtlTabsLayers.SelectedTab().FindVisualChildren<LayerBase>().First().GetType();

        public bool IsSelectedLayerHidden => SelectedLayerType == typeof(HiddenLayerControl);

        private RandomizeMode RandomizeMode => RandomizeModeList.Helper.GetInstance(CtlRandomizeMode.SelectedItem.ToString());
        private double? RandomizerParam => CtlRandomizeModeParam.ValueOrNull;
        private double LearningRate => CtlLearningRate.Value;

        public void DeleteLayer()
        {
            if (MessageBoxResult.OK == System.Windows.MessageBox.Show($"Would you really like to delete layer L{CtlTabsLayers.SelectedIndex + 1}?",
                                                                      "Confirm",
                                                                      MessageBoxButton.OKCancel))
            {
                SelectedLayer.VanishConfig();
                CtlTabsLayers.Items.Remove(CtlTabsLayers.SelectedTab());
                ResetLayersTabsNames();
                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        unsafe public NetworkDataModel CreateNetworkDataModel(INetworkTask networkTask, bool isCopy)
        {
            ErrorMatrix matrix = null;
            if (networkTask != null)
            {
                matrix = new(networkTask.GetClasses());
                ErrorMatrix nextMatrix = new(networkTask.GetClasses());
                matrix.Next = nextMatrix;
                nextMatrix.Next = matrix;
            }

            NetworkDataModel networkModel = new(Id, GetLayersSizes())
            {
                ErrorMatrix = matrix,
                Classes = networkTask?.GetClasses(),
                IsEnabled = CtlIsNetworkEnabled.IsOn,
                Color = CtlColor.Foreground.GetColor(),
                RandomizeMode = RandomizeMode,
                RandomizerParam = RandomizerParam,
                LearningRate = LearningRate,
                InputInitial0 = ActivationFunctionList.Helper.GetInstance(InputLayer.ActivationFunc).Do(InputLayer.Initial0,
                                                                                                        InputLayer.ActivationFuncParam),

                InputInitial1 = ActivationFunctionList.Helper.GetInstance(InputLayer.ActivationFunc).Do(InputLayer.Initial1,
                                                                                                        InputLayer.ActivationFuncParam),

                CostFunction = CostFunctionList.Helper.GetInstance(CtlCostFunction.SelectedValue.ToString()),
                IsAdjustFirstLayerWeights = InputLayer.IsAdjustFirstLayerWeights
            };

            networkModel.ActivateNetwork();

            LayerDataModel prevLayerModel = null;

            var ctlLayers = GetLayersControls();
            for (int layerInd = 0; layerInd < ctlLayers.Count; ++layerInd)
            {
                if (layerInd > 0)
                {
                    prevLayerModel = networkModel.Layers[layerInd - 1];
                }

                networkModel.Layers[layerInd].VisualId = ctlLayers[layerInd].Id;

                var ctlNeurons = ctlLayers[layerInd].GetNeuronsControls().ToArray();

                for (int neuronInd = 0; neuronInd < ctlNeurons.Length; ++neuronInd)
                {
                    var neuronModel = networkModel.Layers[layerInd].Neurons[neuronInd];
                    neuronModel.VisualId = ctlNeurons[neuronInd].Id;
                    neuronModel.IsBias = ctlNeurons[neuronInd].IsBias;
                    neuronModel.IsBiasConnected = ctlNeurons[neuronInd].IsBiasConnected;

                    neuronModel.ActivationFunction = ActivationFunctionList.Helper.GetInstance(ctlNeurons[neuronInd].ActivationFunc);
                    neuronModel.ActivationFuncParam = ctlNeurons[neuronInd].ActivationFuncParam;


                    if (layerInd == 0 && !neuronModel.IsBias)
                    {
                        neuronModel.WeightsInitializer = InputLayer.WeightsInitializer;
                        neuronModel.WeightsInitializerParam = InputLayer.WeightsInitializerParam;

                        double initValue = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam);
                        if (!InitializeModeList.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam));
                        }
                    }
                    else
                    {
                        neuronModel.WeightsInitializer = ctlNeurons[neuronInd].WeightsInitializer;
                        neuronModel.WeightsInitializerParam = ctlNeurons[neuronInd].WeightsInitializerParam;

                        double initValue = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam);
                        if (!InitializeModeList.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Weights.ForEach(w => w.Weight = neuronModel.WeightsInitializer.Do(neuronModel.WeightsInitializerParam));
                        }
                    }

                    if (neuronModel.IsBias)
                    {
                        neuronModel.ActivationInitializer = ctlNeurons[neuronInd].ActivationInitializer;
                        neuronModel.ActivationInitializerParam = ctlNeurons[neuronInd].ActivationInitializerParam;
                        double initValue = ctlNeurons[neuronInd].ActivationInitializer.Do(ctlNeurons[neuronInd].ActivationInitializerParam);

                        if (!InitializeModeList.Helper.IsSkipValue(initValue))
                        {
                            neuronModel.Activation = initValue;
                        }
                    }
                    
                    if (!isCopy && prevLayerModel != null && prevLayerModel.Neurons.Count > 0)
                    {
                        neuronModel.WeightsToNextLayer = new(prevLayerModel.Neurons.Count);

                        var prevNeuronModel = prevLayerModel.Neurons.First;
                        while (prevNeuronModel != null)
                        {
                            if (!neuronModel.IsBias || (neuronModel.IsBiasConnected && prevNeuronModel.IsBias))
                            {
                                neuronModel.WeightsToNextLayer.Add(new(prevNeuronModel, prevNeuronModel.WeightTo(neuronModel)));
                            }

                            prevNeuronModel = prevNeuronModel.Next;
                        }
                    }
                }
            }

            var lastLayer = networkModel.Layers.Last;
            lastLayer.VisualId = Constants.OutputLayerId;
            {
                var ctlNeurons = _outputLayer.GetNeuronsControls().ToArray();
                for (int i = 0; i < ctlNeurons.Length; ++i)
                {
                    lastLayer.Neurons[i].VisualId = ctlNeurons[i].Id;
                }
            }

            if (!isCopy)
            {
                var copy = CreateNetworkDataModel(networkTask, true);
                networkModel.SetCopy(copy);
            }

            return networkModel;
        }

        private void CtlColor_Click(object sender, MouseButtonEventArgs e)
        {
            using ColorDialog colorDialog = new();

            var wpfColor = CtlColor.Foreground.GetColor();
            colorDialog.Color = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B); ;
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                CtlColor.Foreground = Draw.GetBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                OnNetworkUIChanged(Notification.ParameterChanged.Structure);
            }
        }

        private void CtlLayerContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            CtlMenuDeleteLayer.IsEnabled = IsSelectedLayerHidden;
        }

        private void CtlMenuAddLayer_Click(object sender, RoutedEventArgs e)
        {
            AddLayer();
        }

        private void CtlMenuDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            DeleteLayer();
        }

        private void CtlRandomizerButton_Click(object sender, RoutedEventArgs e)
        {
            RandomizerViewer viewer = new(RandomizeMode, RandomizerParam);
            viewer.Show();
        }
    }
}
