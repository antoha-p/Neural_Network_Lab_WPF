﻿using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Qualia.Controls
{
    public partial class NeuronControl : NeuronBaseControl
    {
        private readonly List<IConfigParam> _configParams;

        public NeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationInitializeFunctionParam,
                CtlActivationInitializeFunction,
                CtlWeightsInitializeFunctionParam,
                CtlWeightsInitializeFunction,
                CtlIsBias,
                CtlIsBiasConnected,
                CtlActivationFunction,
                CtlActivationFunctionParam
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumberChanged(int number)
        {
            CtlNumber.Content = number.ToString();
        }

        private void CtlIsBias_CheckedChanged()
        {
            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlActivation.Height = CtlIsBias.IsOn ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
            OnChanged();
        }

        public override InitializeFunction ActivationInitializeFunction => (CtlIsBias.IsChecked == true ? InitializeFunction.GetInstance(CtlActivationInitializeFunction) : null);
        public override double ActivationInitializeFunctionParam => (CtlIsBias.IsChecked == true ? CtlActivationInitializeFunctionParam.Value : 1);
        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.GetInstance(CtlWeightsInitializeFunction);
        public override double WeightsInitializeFunctionParam => CtlWeightsInitializeFunctionParam.Value;
        public override bool IsBias => CtlIsBias.IsChecked == true;
        public override bool IsBiasConnected => CtlIsBiasConnected.IsChecked == true && IsBias;
        public override ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction);
        public override double ActivationFunctionParam => CtlActivationFunctionParam.Value;

        public void LoadConfig()
        {
            CtlWeightsInitializeFunction.Fill<InitializeFunction>(Config, nameof(InitializeFunction.None));
            CtlActivationInitializeFunction.Fill<InitializeFunction>(Config, nameof(InitializeFunction.Constant));
            CtlActivationFunction.Fill<ActivationFunction>(Config);

            _configParams.ForEach(param => param.LoadConfig());

            CtlIsBiasConnected.Visibility = CtlIsBias.IsOn ? Visibility.Visible : Visibility.Collapsed;
            CtlIsBiasConnected.IsOn &= CtlIsBias.IsOn;
            CtlActivation.Height = CtlIsBias.IsOn ? new(0, GridUnitType.Auto) : new(0, GridUnitType.Pixel);

            StateChanged();
        }

        public bool IsValidActivationIniterParam()
        {
            return !IsBias || Converter.TryTextToDouble(CtlActivationInitializeFunctionParam.Text, out _, 777);
        }

        public override bool IsValid()
        {
            return CtlWeightsInitializeFunctionParam.IsValid() && (!IsBias || CtlActivationInitializeFunctionParam.IsValid());
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());

            if (!CtlIsBias.IsOn)
            {
                CtlActivationInitializeFunction.VanishConfig();
                CtlActivationInitializeFunctionParam.VanishConfig();
            }
        }

        public override void VanishConfig()
        {
            _configParams.ForEach(param => param.VanishConfig());
        }
    }
}
