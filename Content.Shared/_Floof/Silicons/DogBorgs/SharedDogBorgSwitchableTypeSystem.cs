using Content.Shared._Floof.Silicons.DogBorgs.Components;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Silicons.DogBorgs;

/// <summary>
///     Implements dogborg type switching.
/// </summary>
public abstract partial class SharedDogBorgSwitchableTypeSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private EntityManager _entMan = default!;
    [Dependency] private InteractionPopupSystem _interactionPopup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    private static readonly EntProtoId ActionId = "ActionSelectDogBorgType";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DogBorgSwitchableTypeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DogBorgSwitchableTypeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DogBorgSwitchableTypeComponent, DogBorgToggleSelectTypeEvent>(OnSelectDogBorgTypeAction);

        Subs.BuiEvents<DogBorgSwitchableTypeComponent>(DogBorgSwitchableTypeUiKey.SelectDogBorgType,
            sub =>
            {
                sub.Event<DogBorgSelectTypeMessage>(SelectTypeMessageHandler);
            });
    }

    private void SelectTypeMessageHandler(Entity<DogBorgSwitchableTypeComponent> ent, ref DogBorgSelectTypeMessage args)
    {
        if (ent.Comp.SelectedBorgType != null)
            return;

        if (!ProtoMan.HasIndex(args.Prototype))
            return;

        SelectBorgModule(ent, args.Prototype);
    }

    private void OnSelectDogBorgTypeAction(Entity<DogBorgSwitchableTypeComponent> ent, ref DogBorgToggleSelectTypeEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;

        _userInterface.TryToggleUi((ent.Owner, null),
            DogBorgSwitchableTypeUiKey.SelectDogBorgType,
            actor.PlayerSession);
    }

    private void OnShutdown(Entity<DogBorgSwitchableTypeComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.SelectTypeAction);
    }

    private void OnMapInit(Entity<DogBorgSwitchableTypeComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.SelectTypeAction, ActionId);
        Dirty(ent);

        if (ent.Comp.SelectedBorgType != null)
            SelectBorgModule(ent, ent.Comp.SelectedBorgType.Value);
    }

    protected virtual void SelectBorgModule(Entity<DogBorgSwitchableTypeComponent> ent, ProtoId<DogBorgTypePrototype> borgType)
    {
        ent.Comp.SelectedBorgType = borgType;

        _actions.RemoveAction(ent.Owner, ent.Comp.SelectTypeAction);
        ent.Comp.SelectTypeAction = null;
        Dirty(ent);

        _userInterface.CloseUi((ent.Owner, null), DogBorgSwitchableTypeUiKey.SelectDogBorgType);

        UpdateEntityAppearance(ent);
    }

    protected void UpdateEntityAppearance(Entity<DogBorgSwitchableTypeComponent> ent)
    {
        if (!ProtoMan.Resolve(ent.Comp.SelectedBorgType, out var proto))
            return;

        UpdateEntityAppearance(ent, proto);
    }

    protected virtual void UpdateEntityAppearance(Entity<DogBorgSwitchableTypeComponent> ent, DogBorgTypePrototype prototype)
    {
        if (TryComp(ent, out InteractionPopupComponent? popup))
        {
            _interactionPopup.SetInteractSuccessString((ent.Owner, popup), prototype.PetSuccessString);
            _interactionPopup.SetInteractFailureString((ent.Owner, popup), prototype.PetFailureString);
        }

        if (TryComp(ent, out FootstepModifierComponent? footstepModifier))
        {
            footstepModifier.FootstepSoundCollection = prototype.FootstepCollection;
        }


        if (!TryComp(ent, out SpriteMovementComponent? spriteMovement))
        {
            spriteMovement = _entMan.AddComponent<SpriteMovementComponent>(ent.Owner);
        }

        spriteMovement.MovementLayers.Clear();
        spriteMovement.NoMovementLayers.Clear();

        // MOVEMENT STATES //
        foreach (var (key, data) in prototype.BodyMovementLayers)
        {
            spriteMovement.MovementLayers[key] = data;
        }

        // IDLE STATES //
        foreach (var (key, data) in prototype.BodyNoMovementLayers)
        {
            spriteMovement.NoMovementLayers[key] = data;
        }

        Dirty(ent, spriteMovement);
    }
}
