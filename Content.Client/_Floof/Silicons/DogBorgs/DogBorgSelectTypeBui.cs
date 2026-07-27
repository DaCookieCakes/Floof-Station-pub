using Content.Shared._Floof.Silicons.DogBorgs.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Floof.Silicons.DogBorgs;

/// <summary>
///     User interface used by DogBorgs to select their type.
/// </summary>
/// /// <seealso cref="DogBorgSelectTypeMenu"/>
/// <seealso cref="DogBorgSwitchableTypeComponent"/>
/// <seealso cref="DogBorgSwitchableTypeUiKey"/>
[UsedImplicitly]
public sealed class DogBorgSelectTypeBui : BoundUserInterface
{
    [ViewVariables]
    private DogBorgSelectTypeMenu? _menu;

    public DogBorgSelectTypeBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DogBorgSelectTypeMenu>();
        _menu.ConfirmedBorgType += prototype => SendPredictedMessage(new DogBorgSelectTypeMessage(prototype));
    }
}
