using System.ComponentModel;

namespace Whim.Bar;

/// <summary>
/// Model for a single window shown in the taskbar widget.
/// </summary>
internal class TaskbarWindowModel : INotifyPropertyChanged
{
	/// <summary>
	/// The window represented by this model.
	/// </summary>
	public IWindow Window { get; }

	/// <summary>
	/// The ID of the workspace this window belongs to.
	/// </summary>
	public WorkspaceId WorkspaceId { get; }

	/// <summary>
	/// The title of the window.
	/// </summary>
	public string Title => Window.Title;

	private bool _isMinimized;

	/// <summary>
	/// Whether the window is currently minimized.
	/// </summary>
	public bool IsMinimized
	{
		get => _isMinimized;
		set
		{
			if (_isMinimized != value)
			{
				_isMinimized = value;
				OnPropertyChanged(nameof(IsMinimized));
			}
		}
	}

	private bool _isOnCurrentWorkspace;

	/// <summary>
	/// Whether this window is on the active workspace for this monitor.
	/// </summary>
	public bool IsOnCurrentWorkspace
	{
		get => _isOnCurrentWorkspace;
		set
		{
			if (_isOnCurrentWorkspace != value)
			{
				_isOnCurrentWorkspace = value;
				OnPropertyChanged(nameof(IsOnCurrentWorkspace));
			}
		}
	}

	private string _workspaceName;

	/// <summary>
	/// The name of the workspace the window belongs to.
	/// </summary>
	public string WorkspaceName
	{
		get => _workspaceName;
		set
		{
			if (_workspaceName != value)
			{
				_workspaceName = value;
				OnPropertyChanged(nameof(WorkspaceName));
			}
		}
	}

	/// <summary>
	/// Command to focus this window (if on current workspace) or move it to the current workspace.
	/// </summary>
	public System.Windows.Input.ICommand ActivateWindowCommand { get; }

	/// <summary>
	/// Creates a new <see cref="TaskbarWindowModel"/>.
	/// </summary>
	public TaskbarWindowModel(
		IContext context,
		TaskbarWidgetViewModel viewModel,
		IWindow window,
		bool isOnCurrentWorkspace,
		string workspaceName,
		WorkspaceId workspaceId
	)
	{
		Window = window;
		WorkspaceId = workspaceId;
		_isMinimized = window.IsMinimized;
		_isOnCurrentWorkspace = isOnCurrentWorkspace;
		_workspaceName = workspaceName;
		ActivateWindowCommand = new ActivateWindowCommand(context, viewModel, this);
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <inheritdoc/>
	protected virtual void OnPropertyChanged(string? propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
