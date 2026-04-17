using System;

namespace Whim.Bar;

/// <summary>
/// Command to activate a taskbar window. For windows on the current workspace, focuses
/// the window. For windows on other workspaces, moves them to the current workspace.
/// </summary>
internal class ActivateWindowCommand : System.Windows.Input.ICommand
{
	private readonly IContext _context;
	private readonly TaskbarWidgetViewModel _viewModel;
	private readonly TaskbarWindowModel _windowModel;

	/// <inheritdoc/>
	public event EventHandler? CanExecuteChanged;

	/// <summary>
	/// Creates a new <see cref="ActivateWindowCommand"/>.
	/// </summary>
	public ActivateWindowCommand(IContext context, TaskbarWidgetViewModel viewModel, TaskbarWindowModel windowModel)
	{
		_context = context;
		_viewModel = viewModel;
		_windowModel = windowModel;
	}

	/// <inheritdoc/>
	public bool CanExecute(object? parameter) => true;

	/// <inheritdoc/>
	public void Execute(object? parameter)
	{
		if (_windowModel.IsOnCurrentWorkspace)
		{
			_context.Store.Dispatch(new FocusWindowTransform(_windowModel.Window.Handle));
		}
		else
		{
			Result<IWorkspace> workspaceResult = _context.Store.Pick(
				Pickers.PickWorkspaceByMonitor(_viewModel.Monitor.Handle)
			);

			if (workspaceResult.TryGet(out IWorkspace workspace))
			{
				_context.Store.Dispatch(
					new MoveWindowToWorkspaceTransform(workspace.Id, _windowModel.Window.Handle)
				);
			}
		}
	}
}
