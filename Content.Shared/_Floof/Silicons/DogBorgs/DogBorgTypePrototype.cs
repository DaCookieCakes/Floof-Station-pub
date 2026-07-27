using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Silicons.DogBorgs;

[Prototype]
public sealed partial class DogBorgTypePrototype : IPrototype
{
    [IdDataField]
    public required string ID { get; set;  }

    #region Description Info

    /// <summary>
    ///     The prototype displayed in the selection menu for this type.
    /// </summary>
    [DataField]
    public required EntProtoId DummyPrototype;

    #endregion


    #region Functionality Information

    /// <summary>
    ///     The amount of free module slots this borg type has.
    /// </summary>
    /// <remarks>
    ///     This count is on top of the modules specified in <see cref="DefaultModules"/>.
    /// </remarks>
    /// <seealso cref="BorgChassisComponent.ModuleCount"/>
    [DataField]
    public int ExtraModuleCount { get; set; } = 0;

    /// <summary>
    ///     The whitelist for borg modules that can be inserted into this borg type.
    /// </summary>
    /// <seealso cref="BorgChassisComponent.ModuleWhitelist"/>
    [DataField]
    public EntityWhitelist? ModuleWhitelist { get; set; }

    /// <summary>
    ///     Inventory template used by this borg.
    /// </summary>
    /// <remarks>
    ///     This template must be compatible with the normal borg templates,
    ///     so in practice it can only be used to differentiate the visual position of the slots on the character sprites.
    /// </remarks>
    /// <seealso cref="InventorySystem.SetTemplateId"/>
    [DataField]
    public ProtoId<InventoryTemplatePrototype> InventoryTemplateId { get; set; } = "borgShort";

    /// <summary>
    ///     Radio channels that this borg will gain access to from this module.
    /// </summary>
    /// <remarks>
    ///     These channels are provided on top of the ones specified in
    /// <see cref="BorgSwitchableTypeComponent.InherentRadioChannels"/>.
    /// </remarks>
    [DataField]
    public ProtoId<RadioChannelPrototype>[] RadioChannels = [];

    /// <summary>
    /// Borg module types that are always available to borgs of this type.
    /// </summary>
    /// <remarks>
    /// These modules still work like modules, although they cannot be removed from the borg.
    /// </remarks>
    /// <seealso cref="BorgModuleComponent.DefaultModule"/>
    [DataField]
    public EntProtoId[] DefaultModules = [];

    /// <summary>
    /// Additional components to add to the borg entity when this type is selected.
    /// </summary>
    [DataField]
    public ComponentRegistry? AddComponents { get; set; }

    #endregion


    #region Visual Information

    [DataField]
    public string SpriteBodyState = "robot";

    // Instead of five string fields, define full layer data per layer
    [DataField]
    public Dictionary<string, PrototypeLayerData> BodyMovementLayers = new();

    [DataField]
    public Dictionary<string, PrototypeLayerData> BodyNoMovementLayers = new();

    #endregion


    #region Minor Information

    /// <summary>
    /// String to use on petting success.
    /// </summary>
    /// <seealso cref="InteractionPopupComponent"/>
    [DataField]
    public string PetSuccessString { get; set; } = "petting-success-generic-cyborg";

    /// <summary>
    /// String to use on petting failure.
    /// </summary>
    /// <seealso cref="InteractionPopupComponent"/>
    [DataField]
    public string PetFailureString { get; set; } = "petting-failure-generic-cyborg";

    #endregion


    #region Sounds

    private static readonly SoundPathSpecifier DefaultFootsteps = new("/Audio/Effects/Footsteps/borgwalk1.ogg");

    /// <summary>
    ///     Sound specifier for footstep sounds created by this borg.
    /// </summary>
    [DataField]
    public SoundSpecifier FootstepCollection { get; set; } = DefaultFootsteps;

    #endregion
}
