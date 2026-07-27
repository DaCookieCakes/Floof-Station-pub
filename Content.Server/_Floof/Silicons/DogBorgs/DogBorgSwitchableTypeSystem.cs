using Content.Server.Inventory;
using Content.Server.Silicons.Borgs;
using Content.Shared._Floof.Silicons.DogBorgs;
using Content.Shared._Floof.Silicons.DogBorgs.Components;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Floof.Silicons.DogBorgs;

[UsedImplicitly]
public sealed partial class DogBorgSwitchableTypeSystem : SharedDogBorgSwitchableTypeSystem
{
    [Dependency] private BorgSystem _borg = default!;
    [Dependency] private ServerInventorySystem _inventory = default!;

    protected override void SelectBorgModule(Entity<DogBorgSwitchableTypeComponent> ent, ProtoId<DogBorgTypePrototype> borgType)
    {
        var prototype = ProtoMan.Index(borgType);

        // Assign radio channels
        string[] radioChannels = [.. ent.Comp.InherentRadioChannels, .. prototype.RadioChannels];
        if (TryComp(ent, out IntrinsicRadioTransmitterComponent? transmitter))
            transmitter.Channels = [.. radioChannels];

        if (TryComp(ent, out ActiveRadioComponent? activeRadio))
            activeRadio.Channels = [.. radioChannels];

        // Borg transponder for the robotics console
        if (TryComp(ent, out BorgTransponderComponent? transponder))
        {
            _borg.SetTransponderSprite(
                (ent.Owner, transponder),
                new SpriteSpecifier.Rsi(new ResPath("_Floof/Mobs/Silicon/chassis.rsi"), prototype.SpriteBodyState));

            _borg.SetTransponderName(
                (ent.Owner, transponder),
                Loc.GetString($"borg-type-{borgType}-transponder"));
        }

        // Configure modules
        if (TryComp(ent, out BorgChassisComponent? chassis))
        {
            var chassisEnt = (ent.Owner, chassis);
            _borg.SetMaxModules(
                chassisEnt,
                prototype.ExtraModuleCount + prototype.DefaultModules.Length);

            _borg.SetModuleWhitelist(chassisEnt, prototype.ModuleWhitelist);

            foreach (var module in prototype.DefaultModules)
            {
                var moduleEntity = Spawn(module);
                var borgModule = Comp<BorgModuleComponent>(moduleEntity);
                _borg.SetBorgModuleDefault((moduleEntity, borgModule), true);
                _borg.InsertModule(chassisEnt, moduleEntity);
            }
        }

        // Configure special components
        if (ProtoMan.Resolve(ent.Comp.SelectedBorgType, out var previousPrototype))
        {
            if (previousPrototype.AddComponents is { } removeComponents)
                EntityManager.RemoveComponents(ent, removeComponents);
        }

        if (prototype.AddComponents is { } addComponents)
        {
            EntityManager.AddComponents(ent, addComponents);
        }

        // Configure inventory template (used for hat spacing)
        if (TryComp(ent, out InventoryComponent? inventory))
        {
            _inventory.SetTemplateId((ent.Owner, inventory), prototype.InventoryTemplateId);
        }

        base.SelectBorgModule(ent, borgType);
    }
}
