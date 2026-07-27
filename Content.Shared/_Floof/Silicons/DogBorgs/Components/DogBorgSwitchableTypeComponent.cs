using Content.Shared.Actions;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Silicons.DogBorgs.Components;

/// <summary>
///     Component for dogborgs that can switch their "type" after being created.
/// </summary>
/// <para>
///     Available types are specified with <see cref="DogBorgTypePrototype"/>s.
/// </para>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class DogBorgSwitchableTypeComponent : Component
{
    /// <summary>
    ///     Action entity used by players to select their type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SelectTypeAction;

    /// <summary>
    ///     The currently selected borg type, if any.
    /// </summary>
    /// <remarks>
    ///     This can be set in a prototype to immediately apply a borg type, and not have switching support.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public ProtoId<DogBorgTypePrototype>? SelectedBorgType;

    /// <summary>
    ///     Radio channels that the borg will always have. These are added on top of the selected type's radio channels.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>[] InherentRadioChannels = [];
}

/// <summary>
///     Action event used to open the selection menu of a <see cref="DogBorgSwitchableTypeComponent"/>.
/// </summary>
public sealed partial class DogBorgToggleSelectTypeEvent : InstantActionEvent;

/// <summary>
///     UI message used by a borg to select their type with <see cref="DogBorgSwitchableTypeComponent"/>.
/// </summary>
/// <param name="prototype">The borg type prototype that the user selected.</param>
[Serializable, NetSerializable]
public sealed class DogBorgSelectTypeMessage(ProtoId<DogBorgTypePrototype> prototype) : BoundUserInterfaceMessage
{
    public ProtoId<DogBorgTypePrototype> Prototype = prototype;
}

/// <summary>
///     UI key used by the selection menu for <see cref="DogBorgSwitchableTypeComponent"/>.
/// </summary>
[NetSerializable, Serializable]
public enum DogBorgSwitchableTypeUiKey : byte
{
    SelectDogBorgType,
}
