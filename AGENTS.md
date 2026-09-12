# Repository agent instructions

## Scope and precedence

These instructions apply repository-wide. A nested `AGENTS.md` or `AGENTS.override.md` takes
precedence in its directory. `Standard.md` is the repository's F# code standard.

## Branch ownership and upstream review

Agent instructions, skills, and context remain on the user's fork branch, `blobbo`, and must not be
included in an upstream PR.
Eye field-of-view or eye-margin changes intended for upstream target `mveb/eye-margin`; other
upstream-eligible changes target `master`. Split mixed files by behavior rather than routing a whole
file to one upstream branch. Human review of code and upstream-divergent comments is required before
opening any upstream PR, including a draft; independent agent review is not human approval. Ordinary
commits and pushes to the user's `blobbo` fork do not require that upstream-PR gate. Do not infer that
human approval has been granted.

The canonical skill root is `.agents/skills/`. Treat references to `.github/skills/` as stale and
repair them when editing the containing file.

Before changing code or assets:

1. Read `Standard.md`, the closest `AGENTS.md`, and the skills routed below.
2. Inspect the exact implementation, project file, assets, tests, and nearby call sites at the checked-out
   revision. Find at least one current analogue before inventing a Nu pattern.
3. Decide the smallest coherent layer that owns the change. Avoid compatibility shims, forwarding aliases,
   extra constructors, and cross-project dependencies that only make the immediate edit convenient.
4. State the observable behavior or invariant to preserve, and choose validation before implementation.
5. Preserve unrelated changes. Use a separate worktree when branch work and local changes would otherwise
   interfere.

## Skill routing

- `.agents/skills/nu-quickstart/SKILL.md`: unfamiliar facets, dispatchers, ImSim declarations, events, GUI,
  and body joints.
- `.agents/skills/nu-runtime-behavior/SKILL.md`: physics, World construction, lifecycle, native runtime
  integration, or Nu-level tests.
- `.agents/skills/nu-maintainer-workflow/SKILL.md`: Nu engine changes, upstream investigation, cleanup,
  review preparation, or maintainer-sensitive diffs.
- `.agents/skills/nu-assets-and-gaia/SKILL.md`: asset graph, default assets, shaders, `.nugroup` authoring,
  Gaia loading, and code reload.
- Under `Projects/Blobbo and Chrono/`, also read its `AGENTS.md`, `PLAN.md`, `PROJECT_STRUCTURE.md`, and
  `ARCHITECTURE.md` in the order specified there.

Put reusable procedural guidance in the narrowest applicable skill. Put project/product invariants in the
closest `AGENTS.md`. Record source-backed Nu maintainer observations in
`.agents/context/nu-maintainer-evidence.md`; do not turn one incident into a universal rule.

## Evidence and API authority

Use this order when sources disagree:

1. The checked-out source, project files, assets, compiler behavior, and executable tests.
2. Current upstream Nu source at an explicitly recorded commit.
3. Maintained upstream wiki pages and primary dependency documentation.
4. Nu issues, PR reviews, discussions, and commits for rationale and maintainer preference.
5. `Happypig375/nu-chat-analysis`, DeepWiki, generated reports, and other secondary indexes for discovery.

History explains intent but can contain obsolete APIs, temporary workarounds, mistakes, and later-reverted
choices. Secondary analysis is hypothesis-rich paraphrase, not API authority. Distinguish observed fact,
maintainer statement, project decision, and inference. Re-check volatile claims such as package versions,
backend choices, signatures, file paths, platform workarounds, and initialization behavior against current
source.

When researching upstream:

- pin the upstream commit or date used;
- inspect the final merged code as well as the proposal and review;
- compare a contribution with the maintainer's post-merge cleanup when available;
- search related call sites, tests, sample projects, project references, and default assets;
- record uncertainty or absence instead of fabricating a convention.

## Repository map and commands

- Nu engine: `Nu/Nu/` (`Nu.fsproj`)
- Math support (and copied C# algorithms): `Nu/Nu.Math/` (C#)
- Nu engine tests: `Nu/Nu.Tests/`
- World editor: `Nu/Nu.Gaia/`
- Asset processor: `Nu/Nu.Pipe/`
- Games and samples: `Projects/`
- Blobbo production game: `Projects/Blobbo and Chrono/`
- Blobbo experiment harness: `Projects/Blobbo Playground/`

Useful commands from the repository root:

```bash
dotnet tool restore
dotnet build "Projects/Blobbo and Chrono/Blobbo and Chrono.fsproj"
dotnet build "Projects/Blobbo Playground/Blobbo Playground.fsproj"
dotnet build Nu/Nu.Gaia/Nu.Gaia.fsproj -f net10.0
dotnet run --project Nu/Nu.Gaia
dotnet test Nu/Nu.Tests/Nu.Tests.fsproj
dotnet build Nu.slnx
```

Do not assume every command is valid for every SDK or platform. Record the exact command, target framework,
result, and environmental blocker.

## Implementation discipline

Follow `Standard.md`; in particular, preserve functional-first design, type inference, exhaustiveness,
currying, debuggability, and warnings-as-errors behavior.

Nu's history and maintainer reviews reinforce these practices:

- Use `UpperCamelCase` for genuine constants. Put a shared physical or protocol value in the closest existing
  constants module instead of duplicating a magic value.
- Keep namespace, `open`, module, and type structure compact and consistent with adjacent files. Preserve
  F# compile order deliberately and remove redundant project references.
- Prefer Nu helpers and established domain vocabulary, such as `v2`, `v3`, `v2Zero`, and `v3Zero`, over
  equivalent lower-level constructions (`Vector2`, `Vector3`) when the surrounding code does.
- Inspect dispatcher and facet defaults before declaring properties. Omit values that merely repeat defaults
  unless the declaration documents or protects an intentional override.
- Remove dead code, stale TODOs, obsolete compatibility aliases, and one-use forwarding bindings. However,
  retain local bindings for intermediate values that are semantically interesting, reused, or useful in a
  debugger. Cleanup is contextual, not a command to minimize line count.
- Choose names that expose lifecycle and intent (`finalizeFrame`, not `last`; domain identity, not a generic
  boolean). Keep public field and union-case names sufficiently distinctive for inference.
- Comments should preserve non-obvious contracts, why a lower-level path is necessary, or why an apparently
  simpler alternative is wrong. Do not narrate syntax. Keep ordinary inline comments concise and aligned
  with nearby Nu style; use complete documentation comments for public contracts.
- Preserve intentional case-first ordering, stepped indentation, Lisp-style bracing, tuple parentheses, and
  other `Standard.md` conventions even when automated formatters suggest a different house style.
- End C-style files with a newline. Nu's maintainer convention intentionally leaves non-C-style (e.g. F# and Markdown) files
  without a terminal newline.

A cleanup may change naming, comments, whitespace, dependency edges, and source order, but it must not hide a
behavior change. Split unrelated semantic changes when they need independent review or validation.

## Nu integration boundaries

Use the highest Nu abstraction that owns the behavior:

- game behavior through World, dispatchers, facets, entities, signals, and asset tags;
- backend APIs directly only when the behavior is genuinely backend-specific or Nu exposes no suitable path;
- mutable native state and I/O at explicit edges, with immutable typed domain state inside;
- `.nugroup` for authored entity trees, not as the sole authority for versioned generation, validation,
  provenance, save semantics, or long-running mutable state.

Do not add a test-only public constructor, persistent initialization flag, compatibility module, or duplicate
property merely to bypass an existing lifecycle. If a direct-backend test is necessary, place a nearby note
explaining why a World-level test would not exercise the supported capability.

## Assets, Gaia, and default propagation

Use asset tags and `AssetGraph.nuag`; do not introduce runtime file-relative asset lookup. Treat source order,
asset packages, project output, default assets, and runtime loading as one integration surface.

`Nu/Nu.Gaia/Assets/Default/` is the canonical default-asset source. After relevant upstream changes, run the
matching propagation script from the repository root:

```bash
PropagateDefaultAssets.Windows.bat
./PropagateDefaultAssets.Linux.sh
```

Review propagated changes as source, rebuild the affected project, load it in Gaia when applicable, exercise
code reload, and launch the runtime path. A compile does not prove that shaders, packages, native libraries,
or serialized entity trees load.

## Validation

Run the narrowest useful check first, then expand according to the dependency radius.

- Pure/domain change: focused tests, serialization or deterministic fixtures, then the owning project build.
- Nu dispatcher/facet/property change: owning project build, relevant tests, Gaia load/reload, and an
  interaction that reaches the changed behavior.
- Physics/backend change: focused backend tests when justified, Nu integration tests, and every affected
  sample or game path. Preserve behavior manually, not only compilation.
- Asset/shader/default change: asset propagation when required, build, Gaia/runtime load, and representative
  visual inspection.
- Native/platform change: capture the first native error before cleanup, distinguish initialization from
  later loading/teardown failures, and test on the affected platform when available.

Assert observable outcomes and invariants, not only object construction. Do not reinterpret a process crash
after reported assertions as a clean pass; report passed assertions and the teardown failure separately and
file or link a defect when it may indicate a real bug.

Interactive validation is required for game feel and for upstream changes that affect samples. Record the
scene, controls exercised, representative frames or observations, runtime logs, and anything not tested.
Never claim a build, test, policy review, profile, or playtest that was not performed.

## Upstream contribution shape

For changes intended for `bryanedds/Nu`:

- make the smallest reviewable coherent change and avoid drive-by formatting;
- use the project's naming, whitespace, comments, EOF, currying, helper, and project-file conventions before
  requesting review;
- include the actual bug or invariant, current-source evidence, automated checks, manual sample coverage, and
  known blockers;
- keep package migrations direct when practical instead of preserving obsolete aliases;
- inspect and learn from maintainer cleanup after merge.

## AI-assisted production

Prefer editable, structured, inspectable, and reproducible source for durable work: code, typed data, scene
graphs, shaders, rigs, semantic events, generators, constraints, seeds, and validation. Directly generated
media is suitable for exploration or clearly tracked placeholders. Durable player-facing output must retain
the source, parameters, provenance, licensing, and local overrides needed for intentional revision.

Do not add runtime remote inference or allow unconstrained generated text or media to mutate authoritative
game state without an explicit architectural decision. AI-assisted code and content require validation
proportionate to their risk.

When a recurring correction reveals durable guidance, update the closest `AGENTS.md`, skill, or evidence note
in the same change.