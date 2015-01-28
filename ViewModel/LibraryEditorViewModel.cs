﻿using GalaSoft.MvvmLight;
using System;

namespace TheCreationist.App.ViewModel
{
    public class LibraryEditorViewModel : ViewModelBase, IDisposable
    {
        public LibraryEditorViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
        }

        public MainViewModel MainViewModel { get; set; }

        public LibraryViewModel LibraryViewModel { get; set; }

        public string LastEditedLibrary { get; set; }

        public void Dispose()
        {
            Logger.Log.Debug("Disposing");

            MainViewModel = null;

            LibraryViewModel = null;

            LastEditedLibrary = string.Empty;

            Logger.Log.Debug("Disposed");
        }
    }
}