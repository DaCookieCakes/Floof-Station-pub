using Content.Client.Gameplay;
using Content.Client.Sandbox;
using Content.Shared._Floof.Decals;
using Content.Shared.Decals;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Floof.Decals.UI;

public sealed partial class FloofDecalPlacerUIController : UIController, IOnStateExited<GameplayState>, IOnSystemChanged<SandboxSystem>
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [UISystemDependency] private readonly SandboxSystem _sandbox = default!;

    private FloofDecalPlacerWindow? _window;

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
            _window.Close();
        else if (_sandbox.SandboxAllowed)
            _window.Open();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window == null)
            return;

        _window.Close();
        _window = null;
    }

    public void OnSystemLoaded(SandboxSystem system)
    {
        _sandbox.SandboxDisabled += CloseWindow;
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
    }

    public void OnSystemUnloaded(SandboxSystem system)
    {
        _sandbox.SandboxDisabled -= CloseWindow;
        _prototypes.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<DecalPrototype>() || obj.WasModified<DecalCategoryPrototype>())
            ReloadPrototypes();
    }

    private void ReloadPrototypes()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Populate(
            _prototypes.EnumeratePrototypes<DecalPrototype>(),
            _prototypes.EnumeratePrototypes<DecalCategoryPrototype>());
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<FloofDecalPlacerWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        ReloadPrototypes();
    }

    private void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Close();
    }
}
