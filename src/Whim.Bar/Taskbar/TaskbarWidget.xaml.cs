using System;
using Microsoft.UI.Xaml.Controls;

namespace Whim.Bar;

/// <summary>
/// Taskbar widget showing windows on the current workspace and other workspaces.
/// Clicking a current-workspace window focuses it; clicking an other-workspace window
/// moves it to the current workspace.
/// </summary>
public partial class TaskbarWidget : UserControl, IDisposable
{
	private readonly Microsoft.UI.Xaml.Window _window;
	private bool _disposedValue;

	/// <summary>
	/// The taskbar view model.
	/// </summary>
	internal TaskbarWidgetViewModel ViewModel { get; }

	internal TaskbarWidget(IContext context, IMonitor monitor, Microsoft.UI.Xaml.Window window)
	{
		_window = window;
		ViewModel = new TaskbarWidgetViewModel(context, monitor);
		window.Closed += Window_Closed;
		UIElementExtensions.InitializeComponent(this, "Whim.Bar", "Taskbar/TaskbarWidget");
	}

	private void Window_Closed(object? sender, Microsoft.UI.Xaml.WindowEventArgs e)
	{
		ViewModel.Dispose();
	}

	/// <summary>
	/// Create the taskbar widget bar component.
	/// </summary>
	public static BarComponent CreateComponent() => new TaskbarComponent();

	/// <inheritdoc/>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_window.Closed -= Window_Closed;
			}

			_disposedValue = true;
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// The bar component for the taskbar widget.
/// </summary>
public record TaskbarComponent : BarComponent
{
	/// <inheritdoc/>
	public override UserControl CreateWidget(IContext context, IMonitor monitor, Microsoft.UI.Xaml.Window window) =>
		new TaskbarWidget(context, monitor, window);
}
