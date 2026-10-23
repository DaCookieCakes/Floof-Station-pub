cmd-subfloormode-desc = Like showsubfloor, but only reveals one kind of subfloor entity, and optionally one pipe layer.
cmd-subfloormode-help = Usage: {$command} [off | All | Wiring | Disposals | Piping [Primary | Secondary | Tertiary]]
    Run with no arguments to see the current setting.
cmd-subfloormode-current = Show-subfloor: {$enabled ->
    [true] on
   *[false] off
}, mode: {$mode}, pipe layer: {$layer}.
cmd-subfloormode-invalid-mode = Invalid mode. Valid modes: off, {$modes}.
cmd-subfloormode-invalid-layer = Invalid pipe layer. Valid layers: {$layers}.
cmd-subfloormode-layer-needs-piping = A pipe layer can only be given with the Piping mode.
cmd-subfloormode-hint-mode = <mode>
cmd-subfloormode-hint-layer = [pipe layer]
