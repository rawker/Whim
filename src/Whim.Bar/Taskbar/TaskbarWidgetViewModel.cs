using System;
using System.Collections.Generic;
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
	/// Fast lookup from window handle to its model, kept in sync with both collections.
	/// </summary>
	private readonly Dictionary<HWND, TaskbarWindowModel> _windowModelIndex = [];

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
		_context.Store.WorkspaceEvents.WorkspaceRenamed += WorkspaceEvents_WorkspaceRenamed;

		UpdateWindowCollections();
	}

	private void UpdateWindowCollections()
	{
		CurrentWorkspaceWindows.Clear();
		OtherWorkspaceWindows.Clear();
		_windowModelIndex.Clear();

		IWorkspace? currentWorkspace = _context
			.Store.Pick(Pickers.PickWorkspaceByMonitor(Monitor.Handle))
			.ValueOrDefault;

		foreach (IWorkspace workspace in _context.Store.Pick(Pickers.PickWorkspaces()))
		{
			bool isCurrentWorkspace = currentWorkspace?.Id == workspace.Id;

			foreach (
				IWindow window in _context
					.Store.Pick(Pickers.PickWorkspaceWindows(workspace.Id))
					.ValueOrDefault ?? []
			)
			{
				AddModel(new TaskbarWindowModel(_context, this, window, isCurrentWorkspace, workspace.Name, workspace.Id));
			}
		}
	}

	private void AddModel(TaskbarWindowModel model)
	{
		_windowModelIndex[model.Window.Handle] = model;

		if (model.IsOnCurrentWorkspace)
		{
			CurrentWorkspaceWindows.Add(model);
		}
		else
		{
			OtherWorkspaceWindows.Add(model);
		}
	}

	private void MapEvents_WindowRouted(object? sender, RouteEventArgs args)
	{
		IWorkspace? currentWorkspace = _context
			.Store.Pick(Pickers.PickWorkspaceByMonitor(Monitor.Handle))
			.ValueOrDefault;

		if (args.PreviousWorkspace != null)
		{
			if (_windowModelIndex.Remove(args.Window.Handle, out TaskbarWindowModel? existing))
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
			AddModel(
				new TaskbarWindowModel(
					_context,
					this,
					args.Window,
					isCurrentWorkspace,
					args.CurrentWorkspace.Name,
					args.CurrentWorkspace.Id
				)
			);
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
		if (_windowModelIndex.TryGetValue(args.Window.Handle, out TaskbarWindowModel? model))
		{
			model.IsMinimized = true;
		}
	}

	private void WindowEvents_WindowMinimizeEnded(object? sender, WindowMinimizeEndedEventArgs args)
	{
		if (_windowModelIndex.TryGetValue(args.Window.Handle, out TaskbarWindowModel? model))
		{
			model.IsMinimized = false;
		}
	}

	private void WorkspaceEvents_WorkspaceRenamed(object? sender, WorkspaceRenamedEventArgs e)
	{
		foreach (TaskbarWindowModel model in OtherWorkspaceWindows.Where(m => m.WorkspaceId == e.Workspace.Id))
		{
			model.WorkspaceName = e.Workspace.Name;
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
				_context.Store.WorkspaceEvents.WorkspaceRenamed -= WorkspaceEvents_WorkspaceRenamed;
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
