﻿using Qualia.Controls;
using System;

namespace Qualia.Tools
{
    public static class PresenterProvider
    {
        unsafe public static ISelectableItem GetPresenter(ActivationFunction instance, string name)
        {
            var control = new FunctionPresenter(name);
            control.Loaded += (sender, e) =>
            {
                control.CtlDescription.Text = name;
                control.DrawBase();
                control.DrawFunction(x => instance.Do(x, 1), in ColorsX.Red, 0);
                control.DrawFunction(x => instance.Derivative(x, instance.Do(x, 1), 1), in ColorsX.Blue, 1);
            };

            return control;
        }

        unsafe public static ISelectableItem GetPresenter(RandomizeFunction instance, string name)
        {
            var control = new FunctionPresenter(name);
            control.Loaded += (sender, e) =>
            {
                control.DrawBase();
                //control.DrawFunction(x => instance.Do(x, 1), in ColorsX.Red);
            };

            return control;
        }

        public static ISelectableItem GetDefaultSelectableItemPresenter(string name)
        {
            return new DefaultSelectableItemPresenter(name);
        }
    }
}
