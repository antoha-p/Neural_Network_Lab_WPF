﻿using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Qualia.Controls
{
    public partial class MemoControl : BaseUserControl
    {
        public string Caption { get; set; } = "Caption";

        public string Text => CtlText.Text;

        public MemoControl()
        {
            InitializeComponent();

            DataContext = this;

            if (File.Exists(FileHelper.NotesPath))
            {
                CtlText.Text = File.ReadAllText(FileHelper.NotesPath);
            }
        }

        internal void Save(string fileName)
        {
            File.WriteAllText(FileHelper.NotesPath, Text);
        }
    }
}