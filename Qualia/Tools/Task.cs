﻿using Qualia;
using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tools
{
    public interface INetworkTask : IConfigValue
    {
        void Do(NetworkDataModel model);
        Control GetVisualControl();
        int GetInputCount();
        List<string> GetClasses();
        void ApplyChanges();
    }

    public interface INetworkTaskChanged
    {
        void TaskChanged();
        void TaskParameterChanged();
    }

    public static class NetworkTask
    {
        public class CountDotsAsymmetric : INetworkTask
        {
            public static INetworkTask Instance = new CountDotsAsymmetric();
            static CountDotsControl Control = new CountDotsControl();

            bool IsSymmetric;
            int MinNumber;
            int MaxNumber;

            public Control GetVisualControl()
            {
                return Control;
            }

            public void ApplyChanges()
            {
                IsSymmetric = Control.IsSymmetric;
                MinNumber = Control.MinNumber;
                MaxNumber = Control.MaxNumber;
            }

            public void Load(Config config)
            {
                Control.Load(config);
                ApplyChanges();
            }

            public void Save(Config config)
            {
                Control.Save(config);
            }

            public int GetInputCount()
            {
                return Control.InputCount;
            }

            public List<string> GetClasses()
            {
                var classes = new List<string>();
                for (int i = Control.MinNumber; i <= Control.MaxNumber; ++i)
                {
                    classes.Add(i.ToString());
                }
                return classes;
            }

            public void Do(NetworkDataModel model)
            {
                if (IsSymmetric)
                {

                }
                else
                {
                    var number = Rand.Flat.Next(MinNumber, MaxNumber + 1);
                    Range.For(number, i => model.Layers.First().Neurons.RandomElementTrimEnd(model.Layers.First().BiasCount).Activation = model.InputInitial1);
                    Range.For(model.Target.Length, i => model.Target[i] = (i == number - MinNumber) ? 1 : 0);
                }
            }

            public void Vanish(Config config)
            {
                Control.Vanish(config);
            }

            public bool IsValid()
            {
                return Control.IsValid();
            }

            public void SetChangeEvent(Action onChanged)
            {
                Control.SetChangeEvent(onChanged);
            }

            public void InvalidateValue()
            {
                throw new NotImplementedException();
            }
        }

        public static class Helper
        {
            public static string[] GetItems()
            {
                return typeof(NetworkTask).GetNestedTypes().Where(c => typeof(INetworkTask).IsAssignableFrom(c)).Select(c => c.Name).ToArray();
            }

            public static INetworkTask GetInstance(string name)
            {
                return (INetworkTask)typeof(NetworkTask).GetNestedTypes().Where(c => c.Name == name).First().GetField("Instance").GetValue(null);
            }

            public static void FillComboBox(ComboBox cb, Config config, string defaultValue)
            {
                Initializer.FillComboBox(typeof(NetworkTask.Helper), cb, config, cb.Name, defaultValue);
            }
        }
    }

    public static class NetworkTaskResult
    {
        public static class Helper
        {
            public static double Invoke(string name, double a)
            {
                var method = typeof(NetworkTaskResult).GetMethod(name);
                return (double)method.Invoke(null, new object[] { a });
            }
        }
    }
}
