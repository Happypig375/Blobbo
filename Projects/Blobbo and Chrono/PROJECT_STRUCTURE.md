# Blobbo project structure, promotion, and Nu-native authoring

**Status:** implementation constraint; read together with `PLAN.md`  
**Applies to:** `Projects/Blobbo Playground/`, `Projects/Blobbo and Chrono/`, and any future Blobbo-specific test or tooling projects  
**Last reconciled:** 2026-09-12

This document defines where work belongs, how an experiment becomes production code, and how Nu-native serialized entity trees such as `.nugroup` fit the deterministic media-shaped world architecture.

---

## 1. Non-negotiable project roles

## `Projects/Blobbo Playground/` — executable gameplay laboratory

`Blobbo Playground` is a runnable experiment harness for **isolated comparisons and combined interaction experiments**. It is not the actual game, a content branch of the game, or the authoritative home of product architecture.

Use it for questions such as:

- Which Blobbo body topology gives the best balance of feel, stability, and cost?
- Does `GrabThrow`, `PullSling`, or `SwipeSmack` produce the clearest mental model?
- Does a candidate Chrono recovery interaction preserve flow?
- Is water absorption readable and useful?
- Does one obstacle actor telegraph its behavior clearly?
- Which visual, audio, or camera treatment makes a physical event understandable?

Each experiment should be small enough to state as:

```text
hypothesis
+ controlled scene
+ configurable variants
+ instrumentation
+ deterministic reset or fixture
+ code-ready evidence
+ human evidence when feel is involved
-> promote, revise, defer, or reject
```

Playground code may be deliberately temporary. It may use debug UI, placeholder assets, exaggerated parameters, and side-by-side variants. It does not need save compatibility, campaign integration, shipping menus, stable public APIs, or final presentation.

However, experiments must still be inspectable and bounded. A disposable prototype is not permission for unbounded histories, hidden global state, irreproducible randomness, or conclusions unsupported by evidence.

Under the project owner's 2026-09-12 request, make the existing mechanics available in one accessible
playtest, with room switching and purposeful combinations that expose interactions between dragging, body
deformation, water, machines, props, and presentation. Do not force every prototype into one physical room.
Retain the isolated baselines and label combined experiments separately. An integrated-playtest observation
can identify interference or a useful interaction; it does not by itself select a body/control candidate
or prove whole-room rewind. The participant package should make both kinds of experiment reachable.

### Playground dependency rule

The actual game must never:

- add a project reference to `Blobbo Playground`;
- load assets directly from `Blobbo Playground`;
- depend on playground-only dispatchers, addresses, scene names, configuration, or global state;
- treat the existence of an experiment as proof that it belongs in the product.

After promotion, dependency may point in the opposite direction: the playground may reference a small production/core component so that an experiment scene remains a regression or tuning harness. Do not create that dependency until the behavior has actually been promoted.

## `Projects/Blobbo and Chrono/` — actual shipping game

`Blobbo and Chrono` is the authoritative product project. It owns the production implementation of:

- the player-facing game loop and journey/session state;
- Blobbo and Chrono behavior that passed promotion gates;
- media-feature domain types and provider boundaries;
- deterministic generation, pattern contracts, and validation;
- production assets and Nu-native authored templates;
- save data, settings, progression, queues, and versioning;
- shipping platform adapters and approved external dependencies;
- runtime budgets, diagnostics, accessibility, and failure handling;
- product-facing audio, rendering, UI, and content.

Production code should contain only selected behavior. It should not accumulate every experimental body, control model, rewind interpretation, analyzer, or obstacle family behind permanent switches.

A production feature needs:

- a typed contract;
- bounded state and explicit invariants;
- appropriate executable tests or validation;
- production ownership of its assets;
- documentation in `ARCHITECTURE.md`;
- evidence that it satisfies the applicable `PLAN.md` gate.

## Automated tests — separate from the playground

`Blobbo Playground` should remain an executable graphical/physics program. Turning the entire playground into a unit-test project would lose the interactive observation needed for game feel, rendering, input, and whole-scene physics behavior.

Automated tests supplement the playground; they do not replace it.

Use automated tests for deterministic, checkable semantics such as:

- pointer sample filtering and release-vector calculation;
- seed derivation and random-stream stability;
- feature timeline validation and serialization round trips;
- pattern selection and parameter mapping;
- entry/exit envelope compatibility;
- body/joint budgets;
- typed snapshot and Chrono anchor restoration;
- queue transitions and stale-state invalidation;
- fixed fixtures producing stable blueprints.

Do **not** create a test project merely to satisfy this document during M0. Introduce a Blobbo-specific test project when enough pure production logic exists to justify one, expected by M3 at the latest.

Preferred eventual direction:

```text
Blobbo and Chrono.Core or another smallest justified production library
    <- Blobbo and Chrono actual game
    <- Blobbo and Chrono.Tests
    <- optional playground regression harness after promotion
```

Do not pre-emptively split a core library before code requires it. If the game executable can be referenced cleanly by the chosen F# test setup, a separate core library may be unnecessary. The important rule is that tests reference production logic and the production game never references tests or the playground.

Human feel tests remain necessary for control, readability, fatigue, delight, and listening continuity even when all deterministic tests pass.

## Non-gameplay research tools

Not every experiment belongs in `Blobbo Playground`.

Use a dedicated small tool or adapter project when the subject is primarily:

- offline media analysis;
- model inference;
- browser or WebView integration;
- feature-file inspection;
- generator fuzzing or batch validation;
- asset conversion;
- compliance-gated platform research.

Keep such tools behind the same versioned data boundaries used by the game. Do not make the playground a miscellaneous container for every research task.

---

## 2. Promotion workflow

An experiment becomes production code through an explicit promotion, not by gradually importing the playground into the game.

1. **State the hypothesis.** Identify the player behavior or technical invariant being tested.
2. **Build the smallest controlled experiment.** Prefer one scene and one comparison axis.
3. **Preserve the baseline.** Variants must be selectable without rewriting the scene between runs.
4. **Collect code-ready evidence.** Builds, deterministic fixtures, telemetry, budgets, and failure cases.
5. **Collect human evidence when required.** Do not infer feel from implementation quality.
6. **Record the decision in `PLAN.md`.** Promote, revise, defer, reject, or keep as separate R&D.
7. **Distill the minimum selected behavior.** Move or reimplement only what earned promotion.
8. **Give production ownership to the actual game.** Rename namespaces, remove debug assumptions, add typed contracts, tests, production assets, and documentation.
9. **Retain or delete the experiment deliberately.** Keep it as a regression/tuning scene only when that has continuing value.

Do not copy a whole experimental subsystem merely because extracting the selected behavior is inconvenient.

### Promotion evidence is not transitive

Examples:

- A pleasant grab control does not validate the existing 528-joint body.
- A stable soft body does not validate a music-shaped journey.
- A deterministic generator does not prove its output is playable.
- A functioning `.nugroup` load does not prove the group satisfies pattern safety contracts.
- A browser API demonstration does not prove release-policy compliance.

Promote each claim at the level where evidence exists.

---

## 3. Milestone ownership

| Plan milestone | Primary project | Boundary |
|---|---|---|
| **M0 — baseline and instrumentation** | `Blobbo Playground`, plus build checks of both projects | Do not move prototypes into the game |
| **M1 — body and control comparison** | `Blobbo Playground` | Select behavior through code and human gates |
| **M2 — Chrono recovery comparison** | Primarily `Blobbo Playground` | Prototype typed anchors; promote only the selected recovery contract |
| **M3 — feature timeline and generator** | `Blobbo and Chrono`, with automated tests | This is product-domain architecture, not a feel sandbox |
| **M4 — synthetic Journey loop** | `Blobbo and Chrono` | First integration of promoted interaction with production generation |
| **M5 — owned-media analysis** | Actual game plus a dedicated analyzer/tool if useful | Analyzer output is a versioned feature file or provider contract |
| **M6 — queue and continuity** | `Blobbo and Chrono` | Product session behavior |
| **M7 — YouTube feasibility** | Actual game adapter and possibly a separate integration spike | Release path remains policy-gated |
| **M8 — MuScriptor enrichment** | Replaceable adapter/tool | Model-specific tokens never become the world format |
| **M9 — scope decision** | `Blobbo and Chrono` | Decide production, revision, or separation from accumulated evidence |

A later milestone may create a small playground regression scene, but the actual implementation and state model belong to the actual game.

---

## 4. Nu-native serialized entity trees

Nu already provides native serialized entity-tree formats. The current game loads `Assets/Gameplay/Scene.nugroup` through `World.beginGroupFromFile`; the file is an S-expression tree containing dispatchers, properties, and child entities.

This is relevant and should be used where it improves authoring, inspection, and reuse. It should not replace the typed generation and validation layers.

### Appropriate `.nugroup` uses

Use an authored `.nugroup` when the important source is primarily an entity hierarchy that benefits from Nu-native editing and serialization, for example:

- a fixed test room;
- a visual or audio rig;
- a reusable obstacle/enemy archetype;
- a hand-authored pattern template;
- static local geometry and dressing;
- named attachment points, anchors, or marker entities;
- a production scene shell;
- a deterministic fixture for load/instantiation validation.

A `.nugroup` can preserve editable entity-tree source instead of burying every scene detail in F# declarations.

### Inappropriate `.nugroup` responsibilities

Do not make a `.nugroup` file the sole authority for:

- `MediaFeatureTimeline` or analyzer provenance;
- the generation key and stable seed derivation;
- pattern selection across a media journey;
- difficulty mapping and clamping;
- entry/exit compatibility or safe-path policy;
- solvability claims;
- long-running segment culling;
- the runtime mutation log;
- media queue/session state;
- complete save-game semantics without verified round-trip support;
- model-specific symbolic output.

These require typed, versioned, testable domain contracts.

### Recommended hybrid representation

```text
Nu-native authored source
    .nugroup entity template and production assets

+ typed pattern contract
    identity, length, entry/exit envelopes, abilities,
    safe-space policy, budgets, accepted parameters

+ deterministic generator
    media features + seed + versions -> pattern choice,
    placement, parameters, and typed overrides

= immutable WorldBlueprint

-> Nu instantiation
    load template or create generated entities,
    apply typed deterministic overrides

-> mutable WorldRuntimeState
    physics, breakage, collections, water, triggers,
    Chrono anchors, culling, and session progress
```

The generator should reference stable pattern/template identities, not concatenate S-expression strings.

### Template versus generated pattern decision

Use a `.nugroup` template when:

- the entity topology is mostly fixed;
- human spatial composition and dressing matter;
- deterministic variation can be expressed as typed transforms or property overrides;
- the template is useful to inspect or edit as an entity tree.

Use an F# pattern generator when:

- geometry count or topology is derived from media features or seed;
- collision shapes, joints, or actors must be constructed algorithmically;
- a template would require a large number of hidden optional entities;
- structural validation needs direct typed access during construction.

Use both when appropriate: a typed generator may place and parameterize a `.nugroup` archetype, then generate additional geometry around it.

### Contract metadata

Do not infer safety and generation contracts from arbitrary scene-tree structure at runtime.

Keep authoritative `PatternContract` metadata in typed F# or a separately versioned data schema. A `.nugroup` may contain stable named marker entities for entry, exit, anchor, or visual guides, but the loader must validate those markers against the typed contract and report actionable errors.

Do not rely on fragile entity-order assumptions or unversioned magic names.

### Playground and production assets

Playground `.nugroup` files belong to the experiment that owns them. The production game must not load those files across the project boundary.

When an authored group is promoted:

- copy or re-author it under `Projects/Blobbo and Chrono/Assets/`;
- remove experiment-only entities and debug configuration;
- give it a stable production identity;
- validate required dispatchers, properties, markers, and assets;
- document its relation to the typed pattern or scene contract.

### Generated-world editability

“Editable after generation” initially means two things:

1. the generated initial world becomes mutable through authoritative runtime physics and interactions; and
2. the same generation key can reproduce the same initial blueprint.

An optional developer export to `.nugroup` may later make a generated segment inspectable or editable in Nu tooling. Treat that as a separate authoring-tool feature. It is not the canonical save or generation format until all of these are proven:

- export/import round trip;
- stable entity identities;
- migration across schema and grammar versions;
- preservation of required custom state;
- clear behavior for runtime-only physics and generated actors;
- deterministic regeneration versus local manual overrides.

Do not commit a generated `.nugroup` for every media item or seed. Preserve the generator, templates, versions, seed, feature timeline or fingerprint, and explicit overrides instead.

---

## 5. Dependency and ownership rules

Allowed eventual dependency direction:

```text
Nu
  <- smallest justified Blobbo production/core code
       <- Blobbo and Chrono actual game
       <- Blobbo and Chrono automated tests
       <- optional Playground regression scene after promotion
```

Disallowed:

```text
Blobbo and Chrono actual game
    -> Blobbo Playground

production save / generation format
    -> experimental scene address or model token

WorldBlueprint
    -> YouTube, MuScriptor, browser-capture, or one analyzer API
```

Rules:

- No circular project references.
- No production asset path points into the playground.
- No experimental namespace appears in serialized production state.
- No model or media provider owns gameplay semantics.
- No generated scene is accepted without bounded runtime ownership and validation.
- No manual `.nugroup` text generation when a typed Nu/F# API is available.
- No automated test is presented as evidence of player delight or comprehensibility.

---

## 6. Agent checklist for placing new work

Before adding a file or subsystem, answer:

1. Is this testing one uncertain gameplay element? Put it in the playground.
2. Is this deterministic product-domain logic selected by the plan? Put it in the actual game or the smallest justified production library.
3. Is this an automated assertion over production semantics? Put it in a test project.
4. Is this an analyzer, converter, browser spike, or batch validator? Consider a dedicated tool/adapter project.
5. Is this primarily an authored Nu entity tree? Consider `.nugroup` plus a typed contract.
6. Is its topology or behavior generated from features and seed? Use typed F# generation, optionally instantiating authored groups.
7. Would the actual game need to reference the playground? Redesign the boundary before proceeding.

When uncertain, prefer an isolated experiment first, but do not leave successful product behavior permanently owned by the playground.