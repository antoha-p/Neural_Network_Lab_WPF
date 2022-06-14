﻿using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class OutputNeuronControl : NeuronBase
    {
        private readonly List<IConfigParam> _configParams;

        public OutputNeuronControl(long id, Config config, Action<Notification.ParameterChanged> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            _configParams = new()
            {
                CtlActivationFunction,
                CtlActivationFunctionParam
            };

            _configParams.ForEach(param => param.SetConfig(Config));
            LoadConfig();

            _configParams.ForEach(param => param.SetChangeEvent(OnChanged));
        }

        public override InitializeFunction WeightsInitializeFunction => InitializeFunction.None.Instance;
        public override double? WeightsInitializeFunctionParam => null;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override ActivationFunction ActivationFunction => ActivationFunction.GetInstance(CtlActivationFunction.SelectedItem);
        public override double? ActivationFunctionParam => CtlActivationFunctionParam.ValueOrNull;

        public void LoadConfig()
        {
            Initializer.FillComboBox<ActivationFunction>(CtlActivationFunction, Config);
            _configParams.ForEach(param => param.LoadConfig());

            StateChanged();
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure);
        }

        public override void OrdinalNumberChanged(int number)
        {
            CtlNumber.Content = number.ToString();
        }

        public override bool IsValid()
        {
            return CtlActivationFunctionParam.IsValid();
        }

        public override void SaveConfig()
        {
            _configParams.ForEach(param => param.SaveConfig());
        }

        public override void VanishConfig()
        {
            _configParams.ForEach(param => param.VanishConfig());
        }
    }
}