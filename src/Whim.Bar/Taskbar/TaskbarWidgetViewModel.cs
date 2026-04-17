using System;
using System.Linq;

namespace Whim.Bar;

/// <summary>
/// View model for the taskbar widget. Tracks windows on the current workspace and
/// windows on all other workspaces.
/// </summary>
internal class TaskbarWidgetViewModel : IDisposable
{
	private readonly IContext _context;
	private bool _disposedValue;

	/// <summary>
	/// The monitor this taskbar widget is displayed on.
	/// </summary>
	public IMonitor Monitor { get; }

	/// <summary>
	/// Windows on the active workspace for this monitor.
	/// </summary>
	public VeryObservableCollection<TaskbarWindowModel> CurrentWorkspaceWindows { get; } = [];

	/// <summary>
	/// Windows on all other workspaces (not the active one for this monitor).
	/// </summary>
	public VeryObservableCollection<TaskbarWindowModel> OtherWorkspaceWindows { get; } = [];

	/// <summary>
	/// Creates a new <see cref="TaskbarWidgetViewModel"/>.
	/// </summary>
	public TaskbarWidgetViewModel(IContext context, IMonitor monitor)
	{
		_context = context;
		Monitor = monitor;

		_context.Store.MapEvents.WindowRouted += MapEvents_WindowRouted;
		_context.Store.MapEvents.MonitorWorkspaceChanged += MapEvents_MonitorWorkspaceChanged;
		_context.Store.WindowEvents.WindowMinimizeStarted += WindowEvents_WindowMinimizeStarted;
		_context.Store.WindowEvents.WindowMinimizeEnded += WindowEvents_WindowMinimizeEnded;

		UpdateWindowCollections();
	}

	private void UpdateWindowCollections()
	{
		CurrentWorkspaceWindows.Clear();
		OtherWorkspaceWindows.Clear();

		IWorkspace? currentWorkspace = _context
			.Store.Pick(Pickers.PickWorkspaceByMonitor(Monitor.Handle))
			.ValueOrDefault;

		foreach (IWorkspace workspace in _context.Store.Pick(Pickers.PickWorkspaces()))
		{
			bool isCurrentWorkspace = currentWorkspace?.Id == workspace.Id;
			string workspaceName = workspace.Name;

			foreach (
				IWindow window in _context
					.Store.Pick(Pickers.PickWorkspaceWindows(workspace.Id))
					.ValueOrDefault ?? []
			)
			{
				TaskbarWindowModel model = new(_context, this, window, isCurrentWorkspace, workspaceName);
				if (isCurrentWorkspace)
				{
					CurrentWorkspaceWindows.Add(model);
				}
				else
				{
					OtherWorkspaceWindows.Add(model);
				}
			}
		}
	}

	private void MapEvents_WindowRouted(object? sender, RouteEventArgs args)
	{
		IWorkspace? currentWorkspace = _context
			.Store.Pick(Pickers.PickWorkspaceByMonitor(Monitor.Handle))
			.ValueOrDefault;

		if (args.PreviousWorkspace != null)
		{
			TaskbarWindowModel? existing =
				CurrentWorkspaceWindows.FirstOrDefault(m => m.Window.Handle == args.Window.Handle)
				?? OtherWorkspaceWindows.FirstOrDefault(m => m.Window.Handle == args.Window.Handle);

			if (existing != null)
			{
				if (existing.IsOnCurrentWorkspace)
				{
					CurrentWorkspaceWindows.Remove(existing);
				}
				else
				{
					OtherWorkspaceWindows.Remove(existing);
				}
			}
		}

		if (args.CurrentWorkspace != null)
		{
			bool isCurrentWorkspace = currentWorkspace?.Id == args.CurrentWorkspace.Id;
			TaskbarWindowModel model = new(
				_context,
				this,
				args.Window,
				isCurrentWorkspace,
				args.CurrentWorkspace.Name
			);

			if (isCurrentWorkspace)
			{
				CurrentWorkspaceWindows.Add(model);
			}
			else
			{
				OtherWorkspaceWindows.Add(model);
			}
		}
	}

	private void MapEvents_MonitorWorkspaceChanged(object? sender, MonitorWorkspaceChangedEventArgs args)
	{
		if (args.Monitor.Handle != Monitor.Handle)
		{
			return;
		}

		UpdateWindowCollections();
	}

	private void WindowEvents_WindowMinimizeStarted(object? sender, WindowMinimizeStartedEventArgs args)
	{
		TaskbarWindowModel? model =
			CurrentWorkspaceWindows.FirstOrDefault(m => m.Window.Handle == args.Window.Handle)
			?? OtherWorkspaceWindows.FirstOrDefault(m => m.Window.Handle == args.Window.Handle);

		if (model != null)
		{
			model.IsMinimized = true;
		}
	}

	private void WindowEvents_WindowMinimizeEnded(object? sender, WindowMinimizeEndedEventArgs args)
	{
		TaskbarWindowModel? model =
			CurrentWorkspaceWindows.FirstOrDefault(m => m.Window.Handle == args.Window.Handle)
			?? OtherWorkspaceWindows.FirstOrDefault(m => m.Window.Handle == args.Window.Handle);

		if (model != null)
		{
			model.IsMinimized = false;
		}
	}

	/// <inheritdoc/>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_context.Store.MapEvents.WindowRouted -= MapEvents_WindowRouted;
				_context.Store.MapEvents.MonitorWorkspaceChanged -= MapEvents_MonitorWorkspaceChanged;
				_context.Store.WindowEvents.WindowMinimizeStarted -= WindowEvents_WindowMinimizeStarted;
				_context.Store.WindowEvents.WindowMinimizeEnded -= WindowEvents_WindowMinimizeEnded;
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
