using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Whim.TestUtils;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Xunit;
using static Whim.TestUtils.StoreTestUtils;

namespace Whim.Bar.Tests;

public class ActivateWindowCommandTests
{
	private static (ActivateWindowCommand Command, TaskbarWindowModel Model) CreateSut(
		IContext ctx,
		IMonitor monitor,
		IWindow window,
		bool isOnCurrentWorkspace,
		WorkspaceId workspaceId
	)
	{
		TaskbarWidgetViewModel viewModel = new(ctx, monitor);
		TaskbarWindowModel model = new(ctx, viewModel, window, isOnCurrentWorkspace, "workspace", workspaceId);
		ActivateWindowCommand command = new(ctx, viewModel, model);
		return (command, model);
	}

	[Theory, AutoSubstituteData]
	internal void CanExecute_AlwaysReturnsTrue(IContext ctx, IMonitor monitor)
	{
		// Given
		IWindow window = CreateWindow((HWND)1);
		(ActivateWindowCommand command, _) = CreateSut(ctx, monitor, window, true, default);

		// When / Then
		Assert.True(command.CanExecute(null));
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void Execute_CurrentWorkspace_DispatchesFocusWindowTransform(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateThreeWayMap(root, monitor, workspace, window);

		TaskbarWidgetViewModel viewModel = new(ctx, monitor);
		TaskbarWindowModel model = new(ctx, viewModel, window, isOnCurrentWorkspace: true, "workspace", workspace.Id);
		ActivateWindowCommand command = new(ctx, viewModel, model);

		// When
		command.Execute(null);

		// Then
		ctx.Store.Received(1).Dispatch(Arg.Is<FocusWindowTransform>(t => t.WindowHandle == window.Handle));
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void Execute_OtherWorkspace_DispatchesMoveWindowToWorkspaceTransform(
		IContext ctx,
		MutableRootSector root
	)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateMonitorWorkspaceMap(root, monitor, currentWorkspace);
		PopulateWindowWorkspaceMap(root, window, otherWorkspace);

		TaskbarWidgetViewModel viewModel = new(ctx, monitor);
		TaskbarWindowModel model = new(
			ctx,
			viewModel,
			window,
			isOnCurrentWorkspace: false,
			"other",
			otherWorkspace.Id
		);
		ActivateWindowCommand command = new(ctx, viewModel, model);

		// When
		command.Execute(null);

		// Then
		ctx.Store.Received(1)
			.Dispatch(
				Arg.Is<MoveWindowToWorkspaceTransform>(t =>
					t.TargetWorkspaceId == currentWorkspace.Id && t.WindowHandle == window.Handle
				)
			);
	}

	[Theory, AutoSubstituteData]
	internal void Execute_OtherWorkspace_NoWorkspaceForMonitor_DoesNotDispatch(IContext ctx, IMonitor monitor)
	{
		// Given - PickWorkspaceByMonitor will return an error (no workspace set up for monitor)
		IWindow window = CreateWindow((HWND)1);
		WorkspaceId otherId = new();

		TaskbarWidgetViewModel viewModel = new(ctx, monitor);
		TaskbarWindowModel model = new(ctx, viewModel, window, isOnCurrentWorkspace: false, "other", otherId);
		ActivateWindowCommand command = new(ctx, viewModel, model);

		// When
		command.Execute(null);

		// Then - no move dispatched when monitor has no workspace
		ctx.Store.DidNotReceive().Dispatch(Arg.Any<MoveWindowToWorkspaceTransform>());
	}
}
