using NSubstitute;
using Whim.TestUtils;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Xunit;
using static Whim.TestUtils.StoreTestUtils;

namespace Whim.Bar.Tests;

public class TaskbarWidgetViewModelTests
{
	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void Ctor_PopulatesCurrentAndOtherWorkspaceWindows(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();

		IWindow currentWindow = CreateWindow((HWND)1);
		currentWindow.Title.Returns("Current Window");
		IWindow otherWindow = CreateWindow((HWND)2);
		otherWindow.Title.Returns("Other Window");

		PopulateThreeWayMap(root, monitor, currentWorkspace, currentWindow);
		PopulateWindowWorkspaceMap(root, otherWindow, otherWorkspace);
		AddWorkspacesToStore(root, otherWorkspace);

		// When
		TaskbarWidgetViewModel sut = new(ctx, monitor);

		// Then
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.Equal(currentWindow, sut.CurrentWorkspaceWindows[0].Window);
		Assert.True(sut.CurrentWorkspaceWindows[0].IsOnCurrentWorkspace);

		Assert.Single(sut.OtherWorkspaceWindows);
		Assert.Equal(otherWindow, sut.OtherWorkspaceWindows[0].Window);
		Assert.False(sut.OtherWorkspaceWindows[0].IsOnCurrentWorkspace);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void Ctor_NoWindows_EmptyCollections(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace = CreateWorkspace();
		PopulateMonitorWorkspaceMap(root, monitor, workspace);

		// When
		TaskbarWidgetViewModel sut = new(ctx, monitor);

		// Then
		Assert.Empty(sut.CurrentWorkspaceWindows);
		Assert.Empty(sut.OtherWorkspaceWindows);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowRouted_WindowAdded_ToCurrentWorkspace(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace = CreateWorkspace();
		PopulateMonitorWorkspaceMap(root, monitor, workspace);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Empty(sut.CurrentWorkspaceWindows);

		IWindow window = CreateWindow((HWND)1);
		workspace = PopulateWindowWorkspaceMap(root, window, workspace);

		// When
		root.MapSector.QueueEvent(RouteEventArgs.WindowAdded(window, workspace));
		root.DispatchEvents();

		// Then
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.Equal(window, sut.CurrentWorkspaceWindows[0].Window);
		Assert.True(sut.CurrentWorkspaceWindows[0].IsOnCurrentWorkspace);
		Assert.Empty(sut.OtherWorkspaceWindows);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowRouted_WindowAdded_ToOtherWorkspace(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();
		PopulateMonitorWorkspaceMap(root, monitor, currentWorkspace);
		AddWorkspacesToStore(root, otherWorkspace);

		TaskbarWidgetViewModel sut = new(ctx, monitor);

		IWindow window = CreateWindow((HWND)1);
		PopulateWindowWorkspaceMap(root, window, otherWorkspace);

		// When
		root.MapSector.QueueEvent(RouteEventArgs.WindowAdded(window, otherWorkspace));
		root.DispatchEvents();

		// Then
		Assert.Empty(sut.CurrentWorkspaceWindows);
		Assert.Single(sut.OtherWorkspaceWindows);
		Assert.Equal(window, sut.OtherWorkspaceWindows[0].Window);
		Assert.False(sut.OtherWorkspaceWindows[0].IsOnCurrentWorkspace);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowRouted_WindowRemoved_FromCurrentWorkspace(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateThreeWayMap(root, monitor, workspace, window);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Single(sut.CurrentWorkspaceWindows);

		// When
		root.MapSector.QueueEvent(RouteEventArgs.WindowRemoved(window, workspace));
		root.DispatchEvents();

		// Then
		Assert.Empty(sut.CurrentWorkspaceWindows);
		Assert.Empty(sut.OtherWorkspaceWindows);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowRouted_WindowRemoved_FromOtherWorkspace(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateMonitorWorkspaceMap(root, monitor, currentWorkspace);
		PopulateWindowWorkspaceMap(root, window, otherWorkspace);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Single(sut.OtherWorkspaceWindows);

		// When
		root.MapSector.QueueEvent(RouteEventArgs.WindowRemoved(window, otherWorkspace));
		root.DispatchEvents();

		// Then
		Assert.Empty(sut.CurrentWorkspaceWindows);
		Assert.Empty(sut.OtherWorkspaceWindows);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowRouted_WindowMoved_CurrentToOther(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateThreeWayMap(root, monitor, currentWorkspace, window);
		AddWorkspacesToStore(root, otherWorkspace);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.Empty(sut.OtherWorkspaceWindows);

		// When
		root.MapSector.QueueEvent(RouteEventArgs.WindowMoved(window, currentWorkspace, otherWorkspace));
		root.DispatchEvents();

		// Then
		Assert.Empty(sut.CurrentWorkspaceWindows);
		Assert.Single(sut.OtherWorkspaceWindows);
		Assert.False(sut.OtherWorkspaceWindows[0].IsOnCurrentWorkspace);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowRouted_WindowMoved_OtherToCurrent(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateMonitorWorkspaceMap(root, monitor, currentWorkspace);
		PopulateWindowWorkspaceMap(root, window, otherWorkspace);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Empty(sut.CurrentWorkspaceWindows);
		Assert.Single(sut.OtherWorkspaceWindows);

		// When - window is moved to current workspace (update the map so pickers work)
		root.MapSector.WindowWorkspaceMap = root.MapSector.WindowWorkspaceMap.SetItem(window.Handle, currentWorkspace.Id);
		root.MapSector.QueueEvent(RouteEventArgs.WindowMoved(window, otherWorkspace, currentWorkspace));
		root.DispatchEvents();

		// Then
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.True(sut.CurrentWorkspaceWindows[0].IsOnCurrentWorkspace);
		Assert.Empty(sut.OtherWorkspaceWindows);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void MonitorWorkspaceChanged_WrongMonitor_NoRebuild(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor1 = CreateMonitor((HMONITOR)100);
		IMonitor monitor2 = CreateMonitor((HMONITOR)200);
		Workspace workspace1 = CreateWorkspace();
		Workspace workspace2 = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateThreeWayMap(root, monitor1, workspace1, window);
		PopulateMonitorWorkspaceMap(root, monitor2, workspace2);

		TaskbarWidgetViewModel sut = new(ctx, monitor1);
		Assert.Single(sut.CurrentWorkspaceWindows);

		// When - workspace changes on a different monitor
		root.MapSector.QueueEvent(
			new MonitorWorkspaceChangedEventArgs() { Monitor = monitor2, CurrentWorkspace = workspace2 }
		);
		root.DispatchEvents();

		// Then - collections unchanged
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.Empty(sut.OtherWorkspaceWindows);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void MonitorWorkspaceChanged_CorrectMonitor_Rebuilds(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace1 = CreateWorkspace();
		Workspace workspace2 = CreateWorkspace();
		IWindow window1 = CreateWindow((HWND)1);
		IWindow window2 = CreateWindow((HWND)2);

		PopulateThreeWayMap(root, monitor, workspace1, window1);
		PopulateWindowWorkspaceMap(root, window2, workspace2);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.Single(sut.OtherWorkspaceWindows);

		// When - switch workspace on this monitor
		root.MapSector.MonitorWorkspaceMap = root.MapSector.MonitorWorkspaceMap.SetItem(monitor.Handle, workspace2.Id);
		root.MapSector.QueueEvent(
			new MonitorWorkspaceChangedEventArgs() { Monitor = monitor, CurrentWorkspace = workspace2 }
		);
		root.DispatchEvents();

		// Then - collections rebuilt: window2 is now current, window1 is other
		Assert.Single(sut.CurrentWorkspaceWindows);
		Assert.Equal(window2, sut.CurrentWorkspaceWindows[0].Window);
		Assert.True(sut.CurrentWorkspaceWindows[0].IsOnCurrentWorkspace);

		Assert.Single(sut.OtherWorkspaceWindows);
		Assert.Equal(window1, sut.OtherWorkspaceWindows[0].Window);
		Assert.False(sut.OtherWorkspaceWindows[0].IsOnCurrentWorkspace);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowMinimizeStarted_UpdatesIsMinimized(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);
		window.IsMinimized.Returns(false);

		PopulateThreeWayMap(root, monitor, workspace, window);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.False(sut.CurrentWorkspaceWindows[0].IsMinimized);

		// When
		root.WindowSector.QueueEvent(new WindowMinimizeStartedEventArgs() { Window = window });
		root.DispatchEvents();

		// Then
		Assert.True(sut.CurrentWorkspaceWindows[0].IsMinimized);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WindowMinimizeEnded_UpdatesIsMinimized(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace workspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);
		window.IsMinimized.Returns(true);

		PopulateThreeWayMap(root, monitor, workspace, window);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.True(sut.CurrentWorkspaceWindows[0].IsMinimized);

		// When
		root.WindowSector.QueueEvent(new WindowMinimizeEndedEventArgs() { Window = window });
		root.DispatchEvents();

		// Then
		Assert.False(sut.CurrentWorkspaceWindows[0].IsMinimized);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WorkspaceRenamed_UpdatesWorkspaceNameOnOtherWorkspaceWindows(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		Workspace otherWorkspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateMonitorWorkspaceMap(root, monitor, currentWorkspace);
		PopulateWindowWorkspaceMap(root, window, otherWorkspace);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Single(sut.OtherWorkspaceWindows);

		// When
		otherWorkspace = otherWorkspace with { Name = "Renamed" };
		root.WorkspaceSector.Workspaces = root.WorkspaceSector.Workspaces.SetItem(otherWorkspace.Id, otherWorkspace);
		root.WorkspaceSector.QueueEvent(
			new WorkspaceRenamedEventArgs() { Workspace = otherWorkspace, PreviousName = "Old Name" }
		);
		root.DispatchEvents();

		// Then
		Assert.Equal("Renamed", sut.OtherWorkspaceWindows[0].WorkspaceName);
	}

	[Theory, AutoSubstituteData<StoreCustomization>]
	internal void WorkspaceRenamed_DoesNotAffectCurrentWorkspaceWindows(IContext ctx, MutableRootSector root)
	{
		// Given
		IMonitor monitor = CreateMonitor((HMONITOR)100);
		Workspace currentWorkspace = CreateWorkspace();
		IWindow window = CreateWindow((HWND)1);

		PopulateThreeWayMap(root, monitor, currentWorkspace, window);

		TaskbarWidgetViewModel sut = new(ctx, monitor);
		Assert.Single(sut.CurrentWorkspaceWindows);

		// When - renaming the current workspace should not affect CurrentWorkspaceWindows models
		currentWorkspace = currentWorkspace with { Name = "Renamed" };
		root.WorkspaceSector.Workspaces = root.WorkspaceSector.Workspaces.SetItem(
			currentWorkspace.Id,
			currentWorkspace
		);
		root.WorkspaceSector.QueueEvent(
			new WorkspaceRenamedEventArgs() { Workspace = currentWorkspace, PreviousName = "Old Name" }
		);

		CustomAssert.DoesNotPropertyChange(
			h => sut.CurrentWorkspaceWindows[0].PropertyChanged += h,
			h => sut.CurrentWorkspaceWindows[0].PropertyChanged -= h,
			root.DispatchEvents
		);
	}

	[Theory, AutoSubstituteData]
	public void Dispose_UnsubscribesAllEvents(IContext ctx, IMonitor monitor)
	{
		// Given
		TaskbarWidgetViewModel sut = new(ctx, monitor);

		// When
		sut.Dispose();

		// Then
		ctx.Store.MapEvents.Received(1).WindowRouted += Arg.Any<EventHandler<RouteEventArgs>>();
		ctx.Store.MapEvents.Received(1).MonitorWorkspaceChanged +=
			Arg.Any<EventHandler<MonitorWorkspaceChangedEventArgs>>();
		ctx.Store.WindowEvents.Received(1).WindowMinimizeStarted +=
			Arg.Any<EventHandler<WindowMinimizeStartedEventArgs>>();
		ctx.Store.WindowEvents.Received(1).WindowMinimizeEnded +=
			Arg.Any<EventHandler<WindowMinimizeEndedEventArgs>>();
		ctx.Store.WorkspaceEvents.Received(1).WorkspaceRenamed +=
			Arg.Any<EventHandler<WorkspaceRenamedEventArgs>>();

		ctx.Store.MapEvents.Received(1).WindowRouted -= Arg.Any<EventHandler<RouteEventArgs>>();
		ctx.Store.MapEvents.Received(1).MonitorWorkspaceChanged -=
			Arg.Any<EventHandler<MonitorWorkspaceChangedEventArgs>>();
		ctx.Store.WindowEvents.Received(1).WindowMinimizeStarted -=
			Arg.Any<EventHandler<WindowMinimizeStartedEventArgs>>();
		ctx.Store.WindowEvents.Received(1).WindowMinimizeEnded -=
			Arg.Any<EventHandler<WindowMinimizeEndedEventArgs>>();
		ctx.Store.WorkspaceEvents.Received(1).WorkspaceRenamed -=
			Arg.Any<EventHandler<WorkspaceRenamedEventArgs>>();
	}
}
