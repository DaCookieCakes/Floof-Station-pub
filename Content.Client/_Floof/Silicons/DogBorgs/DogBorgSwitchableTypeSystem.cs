using Content.Shared._Floof.Silicons.DogBorgs;
using Content.Shared._Floof.Silicons.DogBorgs.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._Floof.Silicons.DogBorgs;

[UsedImplicitly]
public sealed partial class DogBorgSwitchableTypeSystem : SharedDogBorgSwitchableTypeSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DogBorgSwitchableTypeComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<DogBorgSwitchableTypeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.SelectedBorgType != null)
        {
            UpdateEntityAppearance(ent);
        }
    }
}
