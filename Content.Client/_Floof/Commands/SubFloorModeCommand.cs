using Content.Client._Floof.SubFloor;
using Content.Client.SubFloor;
using Content.Shared.Atmos.Components;
using Robust.Shared.Console;

namespace Content.Client._Floof.Commands;

/// <summary>
///     Like showsubfloor, but only reveals one kind of subfloor entity (and optionally one pipe layer).
///     Usage: subfloormode [off | All | Wiring | Disposals | Piping [Primary | Secondary | Tertiary]]
/// </summary>
internal sealed partial class SubFloorModeCommand : LocalizedEntityCommands
{
    [Dependency] private SubFloorHideSystem _subfloor = default!;

    public override string Command => "subfloormode";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // "subfloormode Piping Secondary" -> args = ["Piping", "Secondary"], so args.Length == 2

        // No arguments: just report what's currently set.
        if (args.Length == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-subfloormode-current",
                ("enabled", _subfloor.ShowAll),
                ("mode", _subfloor.ShowAllMode),
                ("layer", _subfloor.ShowAllPipeLayer?.ToString() ?? "any")));
            return;
        }

        if (args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            _subfloor.ShowAll = false;
            return;
        }

        // First argument: the filter mode. ignoreCase so "piping" works too.
        if (!Enum.TryParse<SubFloorFilterMode>(args[0], ignoreCase: true, out var mode))
        {
            shell.WriteError(Loc.GetString("cmd-subfloormode-invalid-mode",
                ("modes", string.Join(", ", Enum.GetNames<SubFloorFilterMode>()))));
            return;
        }

        // Optional second argument: the pipe layer. Only makes sense for piping.
        AtmosPipeLayer? layer = null;
        if (args.Length == 2)
        {
            if (mode != SubFloorFilterMode.Piping)
            {
                shell.WriteError(Loc.GetString("cmd-subfloormode-layer-needs-piping"));
                return;
            }

            if (!Enum.TryParse<AtmosPipeLayer>(args[1], ignoreCase: true, out var parsedLayer))
            {
                shell.WriteError(Loc.GetString("cmd-subfloormode-invalid-layer",
                    ("layers", string.Join(", ", Enum.GetNames<AtmosPipeLayer>()))));
                return;
            }

            layer = parsedLayer;
        }

        _subfloor.ShowAllMode = mode;
        _subfloor.ShowAllPipeLayer = layer;
        _subfloor.ShowAll = true;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // args.Length is which argument the player is currently typing (1 = first).
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(["off", .. Enum.GetNames<SubFloorFilterMode>()],
                Loc.GetString("cmd-subfloormode-hint-mode")),
            2 when args[0].Equals(nameof(SubFloorFilterMode.Piping), StringComparison.OrdinalIgnoreCase)
                => CompletionResult.FromHintOptions(Enum.GetNames<AtmosPipeLayer>(),
                    Loc.GetString("cmd-subfloormode-hint-layer")),
            _ => CompletionResult.Empty,
        };
    }
}
