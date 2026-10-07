
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Alpitronic.TestSystem.WinUI.ViewModels;

namespace Alpitronic.TestSystem.WinUI;

public sealed partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow() => InitializeComponent();

    public void BindViewModel(MainViewModel viewModel)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Title = _viewModel.AppTitle;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.AppTitle) && _viewModel is not null)
            Title = _viewModel.AppTitle;
    }
}
