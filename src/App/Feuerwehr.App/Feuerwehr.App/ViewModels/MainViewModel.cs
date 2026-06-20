using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Tiling;
using Mapsui.Projections;
using Mapsui.Layers;
using Mapsui.Styles;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace Feuerwehr.App.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private int _selectedPageIndex;

        [ObservableProperty]
        private bool _isDrawerOpened;

        public string AppVersion => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "1.0.0";

        public string? Copyright => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?
            .Copyright;

        public string? Description => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyDescriptionAttribute>()?
            .Description;

        public string? ProductTitle => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product;

        public MainViewModel()
        { }

    }
}
