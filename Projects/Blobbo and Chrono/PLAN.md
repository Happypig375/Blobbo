# Blobbo and Chrono — music-shaped physics journey implementation plan

**Status:** implementation plan; M0 and M1 code-ready gates completed with evidence below; M1 human gate pending
**Current milestone:** M1 — body/control comparison and integrated prototype playtest (human gate pending)
**Plan authority:** this file defines the current product hypothesis, scope, order of implementation, and acceptance gates for `Projects/Blobbo and Chrono/`  
**Architecture authority:** `ARCHITECTURE.md` describes the system that is actually implemented; update it when implementation changes  
**Last reconciled:** 2026-09-12
**Base branch:** `blobbo`

---

## 1. Agent execution contract

This plan is written so an implementation agent can work without reconstructing the design from conversation history.

Before changing code:

1. Read the repository-root `AGENTS.md`, `Standard.md`, and `.agents/skills/nu-quickstart/SKILL.md`.
2. Read this file completely.
3. Read `ARCHITECTURE.md` and the files named under **Existing implementation**.
4. Work on the **current milestone only**. Do not start a later milestone because it appears easy or related.
5. Prefer the smallest typed, deterministic, inspectable system that can test the milestone's hypothesis.
6. Run the narrow project build first, then broader checks when practical.
7. Record executed validation and environmental blockers exactly. Never claim a build, test, profile, policy review, or playtest that did not happen.
8. At the end of a milestone PR, update:
   - the milestone checklist and evidence in this file;
   - `ARCHITECTURE.md` if runtime architecture changed;
   - the decision log when evidence resolves an open question.
9. Stop after the milestone's code-ready gate. Human playtest gates remain pending until humans actually test them.

Useful commands from the repository root:

```bash
dotnet tool restore
dotnet build "Projects/Blobbo Playground/Blobbo Playground.fsproj"
dotnet build "Projects/Blobbo and Chrono/Blobbo and Chrono.fsproj"
dotnet test Nu/Nu.Tests/Nu.Tests.fsproj
dotnet build Nu.slnx
```

Do not refactor the Nu engine unless a measured project blocker requires it. Keep generated binaries, caches, captures, model weights, and user media out of Git.

---

## 2. Product thesis

### One-sentence pitch

**Choose something to listen to, enter a deterministic physics world shaped by that media and a seed, then grab and fling Blobbo through readable moving patterns while Chrono keeps failures from interrupting the journey to the next song.**

### Functional fantasy

The player is not controlling an ordinary platform avatar. The player touches a living body of water and physically commits it to motion. Chrono reads echoes of the currently playing media, turns them into a changing world, and preserves Blobbo when the experiment goes wrong.

A provisional presentational fantasy is:

> Music and video leave physical echoes. Chrono crystallizes those echoes into a traversable dreamscape, and Blobbo splashes, stretches, absorbs, breaks, and rebounds through it.

Do not lock a large story or setting before the interaction works. The fantasy must first be visible through body deformation, world motion, material reactions, and time recovery.

### Target experience

The target session should feel like **something enjoyable to do while listening**, not a chart to memorize.

The intended experience has five pillars:

1. **Tactile commitment.** A short gesture produces a consequential physical journey.
2. **Readable patterns.** Obstacles repeat recognizable rules with musical and seeded variation.
3. **Listening continuity.** Ordinary mistakes do not restart the song or demand repeated grinding of the same track.
4. **Generative discovery.** A media source and seed produce a reproducible initial world that is still mutable once physics begins.
5. **Forgiving flow.** Chrono returns the player to meaningful action quickly without erasing the satisfaction of learning the controls.

### Working genre

- Primary storefront category: **2D physics platformer** or **physics action game**.
- Secondary descriptor: **music-shaped procedural journey**.
- It may borrow the cadence of an arcade maze and the responsiveness of a rhythm game, but it is not initially a lane-based rhythm game, precision platformer, or score-grind game.
- Core prototype is solo-first. Co-op remains a later branch.

---

## 3. What to borrow from Tomb of the Mask — and what not to copy

The relevant reference is its interaction cadence, not its tile grid or exact controls.

Borrow these principles:

- the player rapidly scans a readable local pattern;
- one simple gesture commits the character to meaningful travel;
- the character continues until a physical stopping or redirection event;
- obstacle families are learned once and then varied;
- a run has forward pressure and fast recovery;
- the mobile interaction is legible with very little UI.

Transform them for Blobbo:

```text
read local physics pattern
-> grab / pull / swipe Blobbo
-> release into continuous physical motion
-> bounce, deform, spin, absorb, shed, or collide
-> reach a stable contact or use Chrono recovery
-> choose the next commitment
```

Do **not** assume:

- a tile map;
- four cardinal directions only;
- instant wall-to-wall translation;
- a fixed lane;
- exact beat-timing judgments;
- lethal rising pressure as the only pacing tool;
- replaying the same level or song until perfected.

The first control study must compare direct grab-and-throw, pull-and-sling, and swipe/smack under identical conditions. Do not blend them before one dominant rule is understood.

---

## 4. Core interaction loop

### Moment-to-moment loop

1. **Read.** See the next few seconds of surfaces, moving bodies, enemies, safe regions, and collectible opportunities.
2. **Grip.** Touch Blobbo directly at a body region. The body visibly deforms around the contact.
3. **Aim and load.** Move the pointer or finger within a force/range constraint. Blobbo remains physical; the cursor is not a teleport handle.
4. **Commit.** Release or flick. Direction, strength, contact point, and recent gesture velocity produce translation, spin, and deformation.
5. **Ride the result.** Blobbo coasts, bounces, squeezes, sticks, slides, absorbs water, loses water, moves props, or interacts with predictable pattern actors.
6. **Recover or re-grip.** On a stable contact, grip again. On a bad outcome, Chrono recalls or rewinds enough state to restore flow.
7. **Continue.** Progress through the current media-shaped journey instead of restarting the whole track.

### Song/session loop

1. Choose a local media item, permitted media source, playlist, or deterministic feature fixture.
2. Choose or generate a seed.
3. Obtain a versioned `MediaFeatureTimeline`.
4. Generate a deterministic initial `WorldBlueprint` from media features, seed, difficulty envelope, grammar version, and analyzer version.
5. Instantiate the blueprint as mutable physics state.
6. Play through the media journey while the generator keeps a validated window ahead of the camera.
7. At the media end, present a brief summary and continue to the next item by default.
8. Replaying the same item is optional mastery, seed comparison, or sharing—not mandatory progression.

### Primary objective

For the first vertical slice, the primary objective is **stay with the journey until the media item ends**. The player may temporarily fall, collide, or lose optional value without losing access to the rest of the song.

Secondary objectives may include:

- collect echo droplets;
- retain water mass;
- discover an alternate route;
- keep a flow/combo state;
- use fewer Chrono recalls;
- finish near the front of the moving window.

Secondary objectives must not force ordinary players to replay a song before continuing.

---

## 5. Listening-first pacing and failure model

The design contains a tension that must be resolved explicitly:

- tight physics puzzles often wait for the player and rewind world time;
- a listening companion should keep the media flowing and encourage the next song.

### Default target: media-locked Journey mode

In Journey mode:

- media playback is the authoritative session clock;
- ordinary failure does not restart the song;
- the route/camera window advances with media time;
- the player is not graded on exact beat timing;
- Chrono returns Blobbo to a safe state aligned with the **current** media window;
- a gentle current, moving frame, or safe-anchor system prevents long-term desynchronization;
- falling behind costs optional mastery value, not access to the rest of the track.

Chrono's first production recovery should therefore be **Chrono Recall**, not a promise to reverse every causal state in the world:

```text
bad outcome
-> show a short reverse/echo effect
-> reconstruct Blobbo at the most recent valid anchor for the current media time
-> preserve media playback
-> resume control quickly
```

The exact visual can imply rewind while the implementation uses explicit safe anchors and typed state restoration.

### Comparison mode: player-paced physics puzzle

A player-paced mode may pause or rewind media and world state together. It is useful for testing complex authored puzzles and may become an optional challenge mode, but it is not the default listening-companion experience.

M4 must compare the two pacing models. Do not let the architecture assume both are identical.

---

## 6. Blobbo interaction model

### Control candidates

Implement these behind one typed configuration and use the same room, target, body, camera, and feedback:

| Mode | Input rule | Strength | Main risk |
|---|---|---|---|
| `GrabThrow` | A force-limited constraint follows the pointer; release uses recent pointer/body velocity | Most directly expresses picking up a living soft body | Can become unrestricted carrying |
| `PullSling` | Displacement from an anchor determines opposite launch | Clear direction and strength preview | Feels like an abstract slingshot |
| `SwipeSmack` | A short gesture applies a local impulse without sustained holding | Fast, mobile-friendly, close to “smack a slime” | Lower precision and accessibility |

A later hybrid is allowed only if tests identify one dominant mental model.

### Interaction invariants

- The player touches Blobbo, not an invisible proxy.
- Blobbo remains collidable while held.
- Pointer force, reach, and release speed are bounded and visible.
- The constraint cannot pull through solid geometry.
- Off-center contact can create understandable spin and shape response.
- Repeated equivalent gestures produce learnably similar broad outcomes.
- Assistance may clamp or bias outcomes, but must be inspectable and parameterized.
- Input samples and release calculation must be testable independently from rendering.

### Initial re-grip rule

For the first experiment, re-grip is allowed when either:

- Blobbo touches a valid surface and speed is below a configurable threshold; or
- Chrono Recall restores a valid grip state.

Mid-flight re-grip, air dashes, water jets, and multiple consecutive flicks are later mechanics.

### Water identity

After the base throw is fun, introduce one material relationship at a time:

1. free water is absorbed on contact;
2. authoritative water amount changes visible body area or mass;
3. one obstacle pattern responds to that change;
4. deliberate ejection or impact loss comes later;
5. phase changes, splitting, conductivity, and broad chemistry remain deferred.

Particles can visualize transfer, but a small typed water value must remain authoritative for save, replay, generation, and recovery.

---

## 7. Obstacles, enemies, and pattern grammar

### Design principle

The world is not a tile map. It is a continuous physics route assembled from reusable **pattern definitions** with explicit contracts.

A pattern is a structured generator, not a baked scene. It owns:

- normalized local geometry;
- dynamic actors and their periodic behavior;
- entry and exit envelopes;
- safe-space guarantees;
- supported difficulty range;
- required player abilities;
- feature parameters it accepts;
- deterministic variation parameters;
- culling and reset behavior;
- debug rendering.

### Initial pattern families

Implement only three active families plus one rest family before adding enemies:

1. **Open channel:** readable rest space and transition.
2. **Bumper weave:** static or elastic bodies that redirect Blobbo.
3. **Pulse gate:** moving doors/bars with generous periodic openings.
4. **Rotating arm:** predictable rotating body with visible phase.

Later pattern families:

- fans and directional currents;
- crushers with anticipation;
- breakable membranes;
- floating/deformable debris;
- moving platforms or rails;
- fluid pockets;
- physics enemies.

### Enemy definition

An initial enemy is a predictable physics actor, not a combat system. It should:

- follow one readable periodic or reactive rule;
- telegraph contact;
- alter Blobbo's trajectory, water, or route rather than trigger a long death sequence;
- be instantiated through a pattern definition;
- remain bounded and resettable;
- have a safe bypass in the default difficulty envelope.

Health bars, weapons, damage economies, loot tables, and bosses are outside the first vertical slice.

### Continuous route

Use a route parameter such as arc length `s` along a spline or piecewise curve. Pattern geometry is transformed into the local route frame rather than snapped to grid cells.

The generator should be able to curve, widen, narrow, rotate, and layer patterns while preserving entry/exit contracts. The first implementation may use a straight route with continuous coordinates; spline curvature is an extension, not a prerequisite for determinism tests.

---

## 8. Music/video relationship: shaped by media, not enslaved to beats

### This is not initially a conventional rhythm game

Do not implement:

- note lanes;
- “perfect/good/miss” timing windows;
- one obstacle per detected note;
- mandatory replay until a chart is mastered;
- raw music density directly becoming difficulty;
- a requirement that every song be fast.

Instead, media influences the world at several timescales:

| Timescale | Feature | World use |
|---|---|---|
| Whole item | duration, global energy distribution, broad style descriptors | route length, palette, overall pattern mix |
| Section | section boundary, average energy, texture change | biome/pattern-family transition, rest/action balance |
| Phrase | smoothed energy, onset density, bass/brightness | pattern intensity, opening size, motion amplitude, current strength |
| Beat/onset | beat phase or onset confidence | animation pulse, anticipation, particles, optional non-critical actor phase |
| Symbolic notes | pitch/instrument events when available | cosmetic motifs, enemy/obstacle variants, optional enrichment only |

Gameplay-critical geometry must remain inside a separately clamped difficulty envelope. A dense song can produce energetic presentation without becoming impossible; a slow song can still produce interesting spatial decisions.

### Feature processing rules

- Normalize features to documented ranges.
- Smooth noisy inputs and use hysteresis around thresholds.
- Quantize only where it improves pattern readability.
- Reject or soften low-confidence events.
- Cap simultaneous active demands.
- Insert rest/breather patterns independently of raw media density.
- Preserve at least one validated safe envelope.
- Keep music mapping data-driven and inspectable rather than scattering constants through gameplay code.

---

## 9. Deterministic generation model

### World key

The same inputs must reproduce the same initial world:

```text
GenerationKey =
    media fingerprint
  + user seed
  + feature schema version
  + analyzer identity/version
  + grammar version
  + difficulty profile
```

Do not use process-global randomness. Derive segment-local seeds from the generation key and stable segment identity so editing one early pattern does not unpredictably reroll the entire later world.

### Blueprint versus mutable state

Separate:

- `WorldBlueprint`: immutable generated initial geometry, pattern placements, parameters, and provenance;
- `WorldRuntimeState`: mutable Nu/Box2D bodies, triggers, collected items, breakage, water, and Chrono anchors;
- `WorldMutationLog`: optional compact record for debugging, replay, or persistence.

The generated world becomes editable through physics after instantiation. Regeneration with the same key restores the same **initial** world; it does not overwrite runtime mutations every frame.

### Proposed domain types

The exact F# names may change during M0/M3, but preserve these boundaries:

```fsharp
type MediaSourceKind =
    | SyntheticFixture
    | FeatureFile
    | LocalOwnedMedia
    | YouTubeEmbed
    | LiveCaptureResearch

type FeatureFrame =
    { TimeSeconds : double
      Energy : single
      OnsetStrength : single
      BeatPhase : single option
      TempoBpm : single option
      Bass : single
      Mid : single
      Treble : single
      Brightness : single }

type MediaSection =
    { StartSeconds : double
      EndSeconds : double
      Intensity : single
      Label : string option }

type MediaFeatureTimeline =
    { SchemaVersion : int
      MediaFingerprint : string
      DurationSeconds : double
      Frames : FeatureFrame array
      Sections : MediaSection array
      SymbolicEvents : SymbolicEvent array
      Provenance : AnalysisProvenance }

type GenerationKey =
    { MediaFingerprint : string
      UserSeed : uint64
      FeatureSchemaVersion : int
      AnalyzerVersion : string
      GrammarVersion : int
      DifficultyId : string }
```

Requirements:

- timestamps are monotonic;
- normalized values are clamped to `[0, 1]` where applicable;
- serialization is versioned and round-trippable;
- feature files contain no raw media;
- provenance records source kind, analyzer, versions, and licensing notes;
- fixture timelines live in the repository and power deterministic tests.

### Pattern contracts

A pattern definition should expose enough metadata for generation and validation:

```fsharp
type PatternContract =
    { Id : string
      Length : single
      EntryEnvelope : Envelope
      ExitEnvelope : Envelope
      DifficultyRange : Range<single>
      RequiredAbilities : Set<Ability>
      SafePathPolicy : SafePathPolicy
      ActiveBodyBudget : int
      JointBudget : int }
```

Do not start with a general-purpose grammar language. Use typed F# pattern generators first, then extract data when repeated authoring needs become clear.

### Validation strategy

Start with conservative structural guarantees:

- adjacent entry/exit envelopes overlap;
- no static geometry blocks the declared safe envelope;
- moving actors expose a safe phase/window within the difficulty contract;
- active-body and joint budgets are bounded;
- patterns behind the retention window are culled;
- every segment has a deterministic reset/anchor state.

Later, add a reference controller or deterministic Monte Carlo simulation. Do not claim generated worlds are solvable merely because generation completed.

---

## 10. Media source architecture

Generation must depend on a `MediaFeatureTimeline`, not directly on YouTube, MuScriptor, browser capture, or one analyzer. This preserves testability and allows the source to change without rewriting gameplay.

### Source priority

1. **Synthetic fixture timeline — required first.** No audio dependency; deterministic unit and integration tests.
2. **Feature-file timeline — required second.** Load a versioned JSON timeline produced externally.
3. **Local owned/licensed media — first real-media implementation.** Analyze before playback or far enough ahead for safe generation.
4. **Visible YouTube embed — feasibility adapter.** Use supported player controls, state, time, playlist, and events.
5. **Live tab capture — development-only research adapter.** Never make it a release dependency without explicit policy/legal approval.

### Local analysis

Broad features are sufficient for the first media-shaped world:

- onset strength;
- beat/downbeat candidates;
- tempo;
- smoothed energy;
- broad frequency-band energy;
- brightness/spectral centroid;
- section boundaries and silence.

Use an external analyzer process if that keeps the game executable small and the analysis pipeline inspectable. Its output boundary is the feature JSON schema. Do not make MuScriptor a prerequisite for basic geometry.

### MuScriptor

MuScriptor can be an optional enrichment adapter for instrument/pitch events. Architectural rules:

- no model weights in the repository;
- no runtime hard dependency in the core game;
- no direct `SymbolicEvent -> Obstacle` mapping;
- symbolic output is merged, de-duplicated, confidence-filtered, and passed through the same difficulty/pattern constraints;
- record analyzer/model version and licence in provenance;
- allow replacing the adapter without changing `WorldBlueprint` or gameplay types.

### YouTube target and compliance boundary

The product fantasy includes arbitrary YouTube videos, primarily music. Treat this as a target adapter with explicit constraints, not as permission to extract arbitrary media.

For a release candidate using YouTube:

- use a visible supported YouTube embedded player;
- preserve required player controls, branding, ads, metadata, and playback behavior;
- identify the API client correctly;
- do not place gameplay overlays over the embedded player;
- do not download, cache, redistribute, or expose separated audio/video;
- do not implement background playback outside permitted behavior;
- keep game rendering beside or in a separate approved surface from the player;
- obtain a compliance review/audit when the implementation is concrete.

The official IFrame API can control playback, queue items, seek, and report player state/time. It does **not** provide raw audio features. Therefore `YouTubeEmbed` must initially support one of these feature paths:

1. a permitted precomputed `MediaFeatureTimeline` supplied independently;
2. seed/duration/metadata-driven fallback generation with non-critical visual reaction only;
3. a separately approved live-analysis path.

Chrome `tabCapture` requires explicit user invocation and captured audio must be routed back to remain audible. Because real-time isolation/analysis of YouTube audio may conflict with YouTube policy, keep `LiveCaptureResearch` behind a development-only adapter and do not ship it until the usage is reviewed and approved.

Never build server-side YouTube download/extraction into this project.

Relevant current primary references:

- YouTube IFrame Player API: https://developers.google.com/youtube/iframe_api_reference
- YouTube developer-policy guidance: https://developers.google.com/youtube/terms/developer-policies-guide
- YouTube embedded-player minimum functionality: https://developers.google.com/youtube/terms/required-minimum-functionality
- Chrome tab capture: https://developer.chrome.com/docs/extensions/reference/api/tabCapture
- MuScriptor: https://github.com/muscriptor/muscriptor

---

## 11. Business model and licensing architecture

### Product hypothesis

A **free core + paid add-ons** model may fit the listening-companion direction:

- free core: Blobbo interaction, Journey mode, a useful set of patterns, local/allowed media sources;
- possible paid add-ons: authored campaigns, biome/pattern packs, cosmetic bodies, advanced challenge modes, creator/editor tools, curated licensed music experiences, or substantial expansion content.

This is a business hypothesis, not a licence conclusion.

### CC BY-NC 4.0 constraint

MuScriptor's repository code is MIT, while its published model weights are CC BY-NC 4.0. A zero-price executable or free-core funnel is **not automatically** guaranteed to be non-commercial merely because the user pays elsewhere.

Before public commercial deployment of MuScriptor-powered functionality, choose one:

1. obtain explicit permission or a commercial/dual licence from the rights holder;
2. obtain qualified legal advice that the concrete architecture is permissible;
3. replace MuScriptor with a commercially permitted analyzer;
4. keep the MuScriptor-dependent tool as a genuinely separate non-commercial experiment.

Until then:

- MuScriptor may be used for internal prototyping under its terms;
- do not redistribute weights;
- preserve attribution and provenance;
- keep the feature extractor replaceable;
- do not make the entire world format depend on MuScriptor-specific tokens.

The commercialization decision is a later gate, not a reason to prevent ordinary prototype research now.

---

## 12. Existing implementation and intended reuse

### `Projects/Blobbo Playground`

Relevant experiments:

- `Blobbo.fs`: deformable Blobbo, water absorption, contour rendering, and a 32-node complete joint graph.
- `Scene01_BlobboThrow.fs`: direct cursor/body interaction using a zero-length distance joint.
- `Rewindable.fs` and Scene 02: property-history rewind and collision-linked propagation.
- Scene 04: velocity-following eyes and trails.
- Scene 05: water/vapour/machine-state experiments.

Use the playground for control and physics experiments. Do not promote every prototype abstraction into production.

Specific correction:

- the current 32 contour nodes form 496 contour-to-contour joints plus 32 center spokes, or 528 distance joints before contacts;
- benchmark a much smaller ring/pressure body and a hybrid stable-hull/visual-body baseline;
- replace the zero-length generic distance joint with a force-limited mouse-joint-like or custom soft grab;
- keep direct body manipulation rather than replacing it with a generic center impulse before it is tested fairly.

### `Projects/Blobbo and Chrono`

The current `Architecture.fs` provides useful pieces:

- an external browser bridge boundary;
- a source sample clock;
- bounded audio ring buffers;
- overlapping inference windows;
- an inference interface;
- a worker/snapshot separation.

It also contains assumptions that must not become product architecture by accident:

- symbolic events currently become obstacles one-for-one;
- `NullInference` is the only inference adapter;
- obstacle history grows without a spatial retention policy;
- the playback buffer has capacity but no complete synchronized-output contract;
- the rendered game is still a status shell.

Preserve the sample-clock and adapter ideas. Replace direct event-to-obstacle conversion with the feature timeline, pattern grammar, validation, and bounded world-window design.

### Proposed production file boundaries

Exact names may be adjusted to match repository style, but maintain these responsibilities and F# compilation order:

```text
Domain.fs                 shared identifiers, timeline, generation, journey state
MediaFeatures.fs          schema validation, serialization, fixture/provider interfaces
MediaSources.fs           source adapters; no gameplay decisions
Generation.fs             deterministic route and pattern selection
PatternLibrary.fs         typed continuous-physics pattern generators
GenerationValidation.fs   contracts, budgets, diagnostics, later reference simulation
Blobbo.fs                 production body, water, and grip behavior
Chrono.fs                 anchors, recall, optional rewind modes
Journey.fs                media clock, segment window, queue, transitions
DebugViews.fs             seed/features/pattern/safe-envelope/performance overlays
Gameplay.fs               Nu scene composition and user-facing loop
```

Do not split files merely to match this list before code exists. Extract boundaries when a milestone needs them.

---

## 13. Milestones

## M0 — baseline, instrumentation, and experiment harness

**Hypothesis:** the existing prototypes can be measured consistently enough to support control and body comparisons.

### Deliverables

- [x] Confirm the current `blobbo` branch builds for both Blobbo projects, or record exact blockers.
- [x] Add a typed experiment configuration for Scene 01 without changing its default behavior.
- [x] Add debug telemetry for:
  - frame and physics-step timing where Nu exposes it;
  - body, joint, and particle counts;
  - pointer position/velocity;
  - grab force/extension;
  - Blobbo center velocity, angular motion, and settle time;
  - water content;
  - reset count.
- [x] Add an instant deterministic reset of Scene 01.
- [x] Add a ten-minute soak procedure and document observed growth or instability.
- [x] Record baseline body/joint counts, including the complete-graph Blobbo.
- [x] Add no media analyzer, generator, new enemy, story, or production art.

### Code-ready gate

- Both narrow project build commands were run or blockers are documented.
- Telemetry is visible or exportable without allocating unbounded history.
- Reset returns the same configured initial state.
- The default Scene 01 behavior remains available for comparison.
- This file contains an evidence entry with commands and results.

### Human gate

None. M0 is instrumentation only.

### Reproducible ten-minute soak procedure

Use the Debug/default build on a fixed machine and keep the default Scene 01 fixture unchanged.
Start the playground, select Scene 01, and record the visible M0 telemetry at time zero. Leave the
simulation advancing for ten minutes; every 60 seconds record frame/physics milliseconds, logical
body/joint counts, fluid count, Blobbo velocity/angular motion, water, and reset count. During the run,
perform at least ten ordinary grabs/releases and three UI or `R` resets, recording the first and last
snapshot after each reset. Do not emit fluid manually. A valid observation has bounded counts (fluid
must remain at or below the emitter cap of 20,000), identical post-reset fixture values, and no
unbounded telemetry history. The displayed topology is a logical fixture estimate; Nu's public API
does not expose total Box2D object counts. Grab force is intentionally reported as unavailable.
Record crashes, hangs, physics explosions, or count drift as failures;
do not mark this procedure observed until a human or later validator executes it.

### Baseline topology and pre-change evidence

The baseline topology is 33 Blobbo rigid bodies and 528 Blobbo joints. The full Scene 01 logical
topology has 100 rigid bodies and 656 joints before grabbing, and adds one body plus one joint while
grabbing. It starts with 0 fluid particles and its fluid emitter cap is 20,000. These are expected
logical counts; total Box2D object counts are not exposed by the current public Nu API.

Pre-change Debug/default evidence (2026-08-30): DESKTOP-33M8GHE; Windows 10 Home 10.0.19045;
Intel i5-8400 (6 logical), 7.8 GB RAM, Intel UHD 630; .NET SDK 10.0.302. `dotnet build
"Projects/Blobbo Playground/Blobbo Playground.fsproj"` exited 1 in 00:09:56.51 with 18 FS0039
errors. `dotnet build "Projects/Blobbo and Chrono/Blobbo and Chrono.fsproj"` exited 1 in
00:00:36.14 with 1 FS0039 error. The causes were the renamed advancing and runtime-resolution APIs;
the working tree was clean afterward. Post-change build and soak observations are recorded in the
evidence log.

---

## M1 — Blobbo body and control comparison

**Hypothesis:** direct bodily manipulation can be both satisfying and learnably controllable.

### Deliverables

- [x] Implement a simplified physical ring/pressure Blobbo candidate.
- [x] Implement a stable-hull/visual-deformation baseline if practical.
- [x] Implement `GrabThrow`, `PullSling`, and `SwipeSmack` behind the same configuration.
- [x] Use a force-limited, collision-respecting grab rather than teleporting a body.
- [x] Add one empty toy room and one generous target room.
- [x] Record input and outcome traces for deterministic comparison.
- [x] Add provisional touch input abstraction even if the first executable target is mouse.

### Code-ready gate

- [x] Every control mode can be selected without recompiling.
- [x] Equivalent recorded inputs can be replayed for tuning.
- [x] No mode can drag through solid walls or apply unbounded speed.
- [x] The simplified body materially reduces measured constraint cost.
- [x] The target room reports attempts and landing outcome.

### Human gate

With at least five unfamiliar testers:

- most discover the interaction after one visual prompt;
- most voluntarily repeat it in the empty room;
- after three attempts, most can predict broad direction and relative strength;
- one control mode has a clear qualitative or measured advantage.

**Recorded result:** 0 / 5 unfamiliar testers; this gate remains pending and no preferred control
mode has been selected.

**Protocol:** use `../Blobbo Playground/Playtest/M1 Facilitator Guide.md` and its two anonymous CSV
templates. The guide fixes the prompt, timing, counterbalanced control order, prediction observation,
and pass criteria before results are collected.

Do not mark the milestone fully complete until the human gate is recorded.

### 2026-09-12 scope amendment — integrated prototype playtest

The project owner requests that the existing behavioral prototypes also be available together so outsiders
can report interactions between mechanics. Preserve the isolated M1 comparison and Scenes 01–05, and make
them reachable from one accessible playtest. Combine relevant mechanics in interaction scenarios, such as
M1 gestures manipulating the Blobbo that meets water, balloons, machine responses, and props. Room switching
is welcome; not every prototype must occupy one physical room. Include the existing local prop rewind,
math-body, eye, and trail experiments with clear controls. This permits composing existing experiments now;
it does not promote them into production or advance the media, generation, or Chrono milestones.

Acceptance evidence must cover dragging before contact, during movement/holding, and after release;
collision and deformation; combined water/machine responses; rendering after resize; bounded histories and
particles; pause, reset, scene switching, and clean exit. Publish one self-contained participant package
with descriptive navigation, instructions, and a feedback template. Local prop rewind must be labelled as
such: it does not restore Blobbo's manual contour bodies, absorbed water, or the whole room.

Keep integrated-playtest feedback separate from controlled M1 preference data. Human gates remain pending
until unfamiliar testers actually run the protocol; agent interaction records implementation evidence.

---

## M2 — Chrono recovery without media

**Hypothesis:** recovery can preserve experimentation without requiring a universal rewind engine.

### Deliverables

- [ ] Define explicit safe anchors and the minimum typed Blobbo/room snapshot.
- [ ] Implement instant Chrono Recall.
- [ ] Implement a bounded short scrub/rewind comparison only if it can reuse typed snapshots.
- [ ] Add visual/audio placeholders for history, recall, and resume.
- [ ] Keep the generic collision-cascading rewind experiment separate.
- [ ] Measure history memory and restore cost.

### Code-ready gate

- Recall restores a valid grip-ready state deterministically.
- History and anchors are bounded.
- Restore handles sleeping/waking and invalid overlap deliberately.
- A failed target attempt returns to controllable play with one input.

### Human gate

- players understand recovery after one demonstration;
- recovery is preferred to a distant restart;
- players can state what they adjust on the next attempt;
- ordinary recovery feels fast enough to preserve flow.

---

## M3 — deterministic feature timeline and continuous pattern generator

**Hypothesis:** a structured feature timeline plus seed can generate varied, inspectable, non-tile physics worlds.

### Deliverables

- [ ] Add versioned `MediaFeatureTimeline` and `GenerationKey` types.
- [ ] Add synthetic fixture timelines for quiet, slow, energetic, and section-changing media.
- [ ] Add JSON round-trip and schema validation.
- [ ] Add deterministic segment-local random derivation.
- [ ] Generate a straight continuous route first.
- [ ] Implement open channel, bumper weave, pulse gate, and rotating arm patterns.
- [ ] Add entry/exit envelope and budget validation.
- [ ] Add debug views for feature values, pattern IDs, seeds, and safe envelopes.
- [ ] Cull segments outside the retained window.

### Code-ready gate

- same key and fixture produce byte-equivalent blueprint serialization;
- a different user seed changes variation without violating contracts;
- feature values are clamped and smoothed through one inspectable mapping layer;
- generator rejects an intentionally invalid pattern composition;
- active runtime state remains bounded during a long synthetic timeline.

No real audio or YouTube integration is required for M3.

---

## M4 — synthetic Journey-mode vertical loop

**Hypothesis:** Blobbo control, continuous patterns, Chrono recovery, and an advancing media clock form a coherent game rather than disconnected technologies.

### Deliverables

- [ ] Integrate the selected M1 body/control into the generated route.
- [ ] Drive the route window with a synthetic media clock.
- [ ] Implement the first media-locked pacing candidate: moving camera/current plus safe anchors.
- [ ] Keep a player-paced comparison behind configuration.
- [ ] Add optional echo collectibles or water-retention feedback.
- [ ] Add one predictable physics enemy only after obstacle patterns remain readable.
- [ ] Add a five-minute synthetic timeline with section transitions.
- [ ] Add session diagnostics: distance/window position, recalls, water, pattern, feature values, frame/physics time.

### Code-ready gate

- a five-minute fixture completes without a hard restart;
- Blobbo cannot be left permanently outside the current window;
- Chrono recovery aligns with the current journey segment;
- generated segments remain within budgets and cull correctly;
- player-paced and media-locked modes are architecturally distinct.

### Human gate

- players understand the local objective without a long explanation;
- the advancing world does not force rhythm-style precision;
- ordinary collisions invite recovery rather than quitting;
- testers can listen while playing without feeling that every beat demands input;
- the team selects or revises the pacing model based on evidence.

---

## M5 — local owned-media analysis and synchronization

**Hypothesis:** broad audio features can shape a playable world without note-perfect transcription.

### Deliverables

- [ ] Implement a local/owned media analysis path or an external analyzer that emits the feature schema.
- [ ] Support energy, onset, tempo/beat candidates, broad bands/brightness, silence, and section boundaries.
- [ ] Pre-analyze the whole item or maintain sufficient generation lookahead.
- [ ] Synchronize playback, feature time, world time, and diagnostics to one authoritative clock.
- [ ] Test at least three materially different owned/licensed tracks.
- [ ] Provide a no-audio deterministic test mode in CI.
- [ ] Do not require MuScriptor.

### Code-ready gate

- feature timelines are reproducible for fixed analyzer/version/configuration;
- playback and world diagnostics expose measurable drift;
- pause, seek, end, and source change clear or rebuild stale future segments;
- quiet and dense tracks remain inside the same declared difficulty profile;
- raw media and generated caches are not committed.

### Human gate

- players identify at least one world/music relationship without being told;
- a matched randomized mapping is judged weaker than the feature-shaped mapping;
- slower/non-percussive media remains worthwhile;
- media remains enjoyable rather than being obscured by game audio or constant demands.

---

## M6 — queue, next-song flow, and session continuity

**Hypothesis:** the product is stronger as a journey across media than as a mastery loop around one chart.

### Deliverables

- [ ] Add a queue abstraction independent from the media provider.
- [ ] Transition from one completed item/blueprint to the next without returning to a title screen.
- [ ] Default the end screen to continue; make replay optional.
- [ ] Preserve only appropriate session state across items: chosen difficulty, cosmetics, aggregate echoes, and explicit settings.
- [ ] Generate a new world key from the next media fingerprint and seed policy.
- [ ] Add a concise per-item summary without star-ranking pressure.
- [ ] Support a three-item local/fixture session.

### Code-ready gate

- three items run sequentially without unbounded state growth;
- seek/end/error transitions cannot leave stale patterns active;
- replaying uses an explicit same-seed or new-seed choice;
- session persistence is typed and versioned.

### Human gate

- testers commonly choose the next item rather than immediately replaying;
- the summary does not imply that repetition is mandatory;
- transition time and friction do not break the listening session.

---

## M7 — YouTube embed feasibility and policy gate

**Hypothesis:** a visible, policy-compliant YouTube player can coexist with the Journey loop and permitted feature sources.

### Deliverables

- [ ] Implement a `YouTubeEmbed` adapter using the official IFrame Player API in an appropriate browser/WebView surface.
- [ ] Preserve player visibility, controls, metadata, ads, branding, client identity, and minimum-size requirements.
- [ ] Keep gameplay outside the player's visual bounds.
- [ ] Synchronize game state to player ready/play/pause/buffer/seek/end/error events.
- [ ] Implement playlist/next-video behavior through supported APIs.
- [ ] Add a fallback world using seed, duration, and a permitted feature timeline when raw audio features are unavailable.
- [ ] Document a concrete compliance review against current YouTube policies.
- [ ] Keep live tab capture in a separate development adapter and disabled in release builds.

### Code-ready gate

- a public embeddable test video can play, pause, seek, end, and advance while the game reacts to state/time;
- the player is not covered or modified by game UI;
- no YouTube audio/video is downloaded, cached, redistributed, or exposed;
- the release build contains no hidden capture path;
- policy uncertainties are written as blockers rather than assumed away.

### Release gate

Do not ship arbitrary-YouTube audio analysis until the concrete approach has explicit policy/legal approval. If approval is unavailable, ship local/licensed media and a compliant YouTube fallback rather than bypassing platform rules.

---

## M8 — optional MuScriptor enrichment and commercialization decision

**Hypothesis:** symbolic transcription adds enough value beyond broad features to justify its compute and licensing complexity.

### Deliverables

- [ ] Implement MuScriptor behind the feature-provider boundary, preferably as an external tool/process.
- [ ] Merge overlapping windows and de-duplicate events.
- [ ] Record confidence/provenance and avoid gameplay-critical use of uncertain labels.
- [ ] Compare broad-feature generation against broad features plus symbolic enrichment.
- [ ] Measure latency, compute, memory, and player-visible benefit.
- [ ] Record the chosen licence/commercialization path.

### Gate

Promote MuScriptor only if:

- testers perceive a stronger relationship to the media;
- it does not create unsolvable density or fragile synchronization;
- the selected distribution model is legally and commercially supportable;
- the game remains fully functional with the adapter removed or replaced.

---

## M9 — vertical-slice decision

The project is ready for content production only when:

- [ ] one control model is selected and human-validated;
- [ ] the body runs within measured budgets;
- [ ] Chrono recovery preserves listening continuity;
- [ ] deterministic generation produces safe, varied continuous patterns;
- [ ] three different media items produce recognizably related but not impossible worlds;
- [ ] a three-item queue encourages continuation;
- [ ] a silent 5–10 second clip communicates direct body manipulation and music-shaped world motion;
- [ ] a 20-minute session has no unbounded history, obstacle accumulation, or progressive performance collapse;
- [ ] the media source and model licensing path is explicit;
- [ ] the next ten content ideas mostly reuse existing systems rather than requiring new engines.

At M9, choose among:

- continue into a free-core music journey;
- produce an authored Blobbo campaign using the validated interaction;
- separate the music-reactive technology into its own product;
- revise or stop the direction based on evidence.

---

## 14. Testing, diagnostics, and reproducibility

### Required automated tests

Add project-level tests using the repository's existing test conventions rather than introducing a new framework without need.

Cover:

- feature timeline validation and serialization round-trip;
- monotonic timestamps and normalized ranges;
- generation-key stability;
- same-key deterministic blueprint output;
- segment-local RNG independence;
- pattern entry/exit compatibility;
- budget enforcement and culling;
- invalid safe-envelope rejection;
- media state transitions: play, pause, seek, end, source change;
- anchor/snapshot restore;
- queue transition and stale-state clearing.

Physics feel itself requires human testing, but input calculation, clamping, state transitions, serialization, and generation can be tested exactly.

### Required debug views

The running game should be able to show:

- media source, fingerprint, time, and duration;
- user seed and complete generation key;
- analyzer/grammar/schema versions;
- current section and normalized feature bars;
- current and upcoming pattern IDs;
- safe envelopes and anchor locations;
- Blobbo water, center velocity, angular velocity, grip mode, and grab force;
- active body/joint/particle counts;
- frame/physics/generation timing;
- recalls, resets, and generation validation failures;
- playback/world drift.

Debug views are part of the product-development system, not disposable logging.

### Performance policy

Use provisional budgets until M0 establishes a baseline:

- target a stable 60 Hz presentation where supported;
- keep median frame time comfortably below 16.7 ms on the developer target machine;
- report 95th/99th percentile spikes rather than only averages;
- bound every queue, history, cache, and active world window;
- generate ahead off the critical render path;
- degrade presentation before violating gameplay safety;
- do not increase Blobbo node/joint count or gameplay particles without measured justification.

Record the exact machine and build configuration with performance evidence.

---

## 15. Accessibility and attention budget

Because the product is intended to accompany listening:

- do not require continuous visual fixation for every beat;
- telegraph moving hazards with shape, motion, and sound;
- provide reduced-motion and reduced-flash options;
- never encode critical state through color or pitch alone;
- support adjustable Chrono assistance and journey pressure;
- allow mouse and touch remapping where platform APIs permit;
- provide pause/exit behavior that does not corrupt the media/world clock;
- keep game effects below the media mix by default and expose separate volume controls;
- use forgiving default targets and reserve precision for optional mastery.

---

## 16. Explicit non-goals for the current plan

Do not implement during M0–M6 unless a milestone is formally revised:

- tile-based world generation;
- conventional four-direction swipe-only movement;
- note-lane rhythm scoring;
- mandatory replay/grind of one song;
- online multiplayer;
- mandatory co-op;
- combat progression, health builds, weapons, bosses, or loot;
- open world;
- large narrative campaign;
- level editor or public sharing backend;
- arbitrary local object rewind as the default recovery system;
- full water phase chemistry;
- math-expression gameplay;
- server-side YouTube downloading or audio extraction;
- runtime remote generative-AI content;
- final art production before the vertical-slice gate.

---

## 17. Open decisions

Do not resolve these by preference alone; attach evidence.

| Decision | Current hypothesis | Required evidence |
|---|---|---|
| Primary gesture | direct `GrabThrow` | M1 comparison and human prediction data |
| Body model | simplified physical ring, with hybrid baseline | M0/M1 profile plus interaction differences |
| Journey pacing | media-locked moving window | M4 listening/attention tests |
| Chrono recovery | safe-anchor recall with rewind presentation | M2/M4 recovery and continuity data |
| First water effect | visible area or mass, tested separately | M4 material-readability test |
| Generator route | continuous straight route, later spline | M3 contract/variety evidence |
| Media mapping | broad features shape pattern parameters | M5 randomized-control comparison |
| YouTube analysis | compliance-gated; no assumed raw audio access | M7 concrete policy review |
| MuScriptor | optional enrichment, not dependency | M8 benefit/cost/licence evidence |
| Business model | free core + paid add-ons is plausible | retention, scope, licence, and market evidence |
| Setting | media-echo dreamscape is provisional | vertical-slice footage and player interpretation |

---

## 18. Decision log

### 2026-08-30

- The plan combines the direct-body Blobbo interaction with the arbitrary-media world-generation direction rather than treating them as unrelated projects.
- The relevant Tomb of the Mask lesson is its read/commit/travel/contact cadence and reusable obstacle patterns, not its tiles or cardinal-only swipe control.
- The generated world is continuous physics geometry assembled from typed pattern generators.
- Media features and seed jointly determine a reproducible initial world; physics can mutate it afterward.
- Journey mode prioritizes reaching the next song over replaying one chart for perfection.
- Broad audio features precede symbolic transcription.
- YouTube is a target adapter with a policy gate, not the foundational dependency of the generator.
- MuScriptor licensing is a commercialization constraint, not a prototype blocker; free-core plus paid add-ons remains a hypothesis requiring explicit permission or legal review.

---

## 19. Evidence log

### 2026-08-31 — M0 code-ready validation

- **Commit:** final M0 working tree based on merge commit `b77108ff86`; see the containing commit in
  Git history.
- **Machine / OS / build configuration:** DESKTOP-33M8GHE; Windows 10 Home 10.0.19045; Intel
  i5-8400 (6 logical), 7.8 GB RAM, Intel UHD 630; .NET SDK 10.0.302; Debug/default.
- **Commands run:** root `PropagateDefaultAssets.Windows.bat`; `dotnet clean` and `dotnet build` for
  `Projects/Blobbo Playground/Blobbo Playground.fsproj`; `dotnet build` for
  `Projects/Blobbo and Chrono/Blobbo and Chrono.fsproj`; repeated narrow builds after runtime fixes.
- **Results:** both narrow projects built with zero warnings and zero errors. The propagated project
  default-asset directories each matched all 187 canonical files by relative name and SHA-256 hash.
  The playground launched through its title screen into Scene 01 without the earlier missing-shader
  software exception. The asset graph now includes the valid empty `Gui` package required by the
  title screen.
- **Measurements:** the visible baseline remained 100 logical bodies / 656 logical joints and the
  held state remained 101 / 657. Ten grab/release cycles and at least three deterministic resets were
  exercised; the final run reached reset count 7 and returned to 100 / 656 with zero fluid particles.
  Settling completed in observed runs (one final sample reported 4.75 seconds). Scene 01 remained
  responsive for more than ten minutes. Process samples were 76.7 / 68.6 / 63.7 MB working set,
  1221.5 MB private bytes at every sample, and 725 / 738 / 740 handles; no monotonic memory growth,
  crash, hang, physics explosion, or topology drift was observed. The process samples were taken at
  start, +45 seconds, and +90 seconds while the scene itself remained active beyond ten minutes, so
  this observation did not capture every telemetry field at each 60-second interval prescribed by
  the reusable soak procedure.
- **Human test protocol and participant count:** none; M0 has no human gate.
- **Observed failures:** stale project default assets initially caused a missing
  `FilterBilateralDownSample4d.vert` exception; root propagation and a clean rebuild fixed it. Manual
  water-balloon bodies initially logged a missing `AwakeTimeStamp` extension property; declaring the
  matching non-persistent engine property fixed the ownership contract. Nu does not expose total
  Box2D body/joint counts or the mouse-joint force through the public API, so telemetry labels these
  logical estimates and unavailable values explicitly.
- **Decision changed or retained:** retain the existing Scene 01 fixture and use this bounded,
  screen-owned telemetry/reset harness as the M1 comparison baseline. Do not begin M1 in this change.

### 2026-08-31 — M1 code-ready validation

- **Commit:** final M1 working tree based on merge commit `c4b3d4a865`; see the containing commit in
  Git history.
- **Machine / OS / build configuration:** DESKTOP-33M8GHE; Windows 10 Home 10.0.19045; Intel
  i5-8400 (6 logical), 7.8 GB RAM, Intel UHD 630; .NET SDK 10.0.302; Debug, Gaia, and Release/win-x64.
- **Commands run:** root `PropagateDefaultAssets.Windows.bat`; narrow Playground and production-game
  builds; the five-test M1 project; `--verify-m1`; Gaia and Gaia-configuration builds; and
  `dotnet publish "Projects/Blobbo Playground/Blobbo Playground.fsproj" -c Release -r win-x64
  --self-contained false -p:IsPublishing=true`.
- **Results:** the narrow Playground, `Blobbo and Chrono`, Gaia-configuration, and Gaia-host builds
  passed with zero warnings and zero errors. All 5 M1 tests and all 16 executable verifier checks
  passed. The deterministic verifier trace checksum was `5013E3E41D876A49`. The framework-dependent
  win-x64 test package contains 196 physical asset files plus `Launch M1 Test.cmd`; launching it opens
  directly into the M1 comparison screen.
- **Measurements:** the legacy graph uses 33 logical bodies / 528 logical joints, the simplified ring
  uses 13 / 24, and the stable hull uses 1 / 0. The ring therefore reduces the measured joint count by
  95.5% relative to the legacy graph. All three control modes report bounded force, impulse, and speed.
- **Interactive checks:** `GrabThrow`, `PullSling`, and `SwipeSmack`; ring, stable-hull, and legacy body
  selection; both toy and target rooms; target outcomes; reset; viewport resizing; and deterministic
  trace replay were exercised. In the final published build, a 15-sample drag replay advanced telemetry
  to 2 attempts / 1 reset / 1 replay. The M1 dispatcher was also loaded in Gaia, simulation was started,
  Pull was selected, and a drag recorded a 15-sample trace and one attempt.
- **Human test protocol and participant count:** none; 0 / 5 unfamiliar testers. The M1 human gate is
  still pending.
- **Observed failures:** publishing without `IsPublishing=true` omitted piped assets; the corrected
  command stages them. A custom output path exposed Nu's publish-path concatenation assumption, so the
  standard publish output is copied into `bin/M1Test` for the test export. Physics initially registered
  before the ImSim spawn position settled, and paused replay initially consumed input; an applied-position
  synchronization check and advancing-time gate fixed those defects before final interaction testing.
- **Decision changed or retained:** the M1 code-ready gate passes. Keep all three candidates and controls
  available for the human comparison, run at least five unfamiliar testers next, and do not begin M2 or
  choose a preferred control until that evidence is recorded.

### 2026-08-31 — M1 participant handoff and Box2D merge follow-up

- **Base:** pushed M1 commit `57656a4fbf`; see the containing follow-up commit in Git history.
- **Commands run:** the Nu, Playground, production-game, and Gaia builds; the three focused
  `Box2dNetPhysicsEngineTests`; the five-test M1 project; `--verify-m1`; a Release/win-x64
  self-contained publish; and the verifier again after extracting the participant ZIP.
- **Results:** every build passed with zero warnings and zero errors. The focused Box2D tests passed
  3 / 3, the M1 tests passed 5 / 5, and both verifier runs passed 16 / 16 with trace
  `5013E3E41D876A49`.
- **Participant build:** `bin/Blobbo-M1-Playtest-win-x64.zip` is a 90,122,689-byte self-contained
  direct-launch package with 238 entries, including 196 physical assets and the facilitator guide plus
  both anonymous CSV templates. It contains no `Log.txt`, `imgui.ini`, or `ShaderCache` entries. Its
  SHA-256 is `BF7E1C841DEF3AE3CF8D0F831C7D377B5EF83C0008BDCF7DDE7B6CEA551A5D1A`.
- **Interactive checks:** the packaged launcher opened directly in the M1 lab; drag, trace replay,
  target-room selection, Pull selection, miss reporting, and Quit were exercised. In Scene 01, emitted
  water popped both balloons, Blobbo absorbed to its 32-unit cap and visibly expanded, the expanded body
  remained stable through a drag, and Reset restored 100 bodies / 656 joints with zero water and fluid.
- **Human test protocol and participant count:** the fixed facilitator protocol and counterbalanced CSV
  templates are ready; 0 / 5 unfamiliar testers have run them, so the human gate remains pending.
- **Observed failures:** a fresh merged build exposed four non-exhaustive Box2D message handlers lost
  while adapting Blobbo PR #2 to the Box2D.NET update. Runtime damping, sphere-shape, and distance-joint
  handling were restored against the current direct APIs and covered by focused tests; upstream PR
  #1437 remains unchanged. A clean Nu.Tests output removed a stale Box2D.NET 3.1.1.557 assembly before
  the focused tests passed against 3.1.654. An earlier unfiltered Nu.Tests run passed 214 assertions,
  then Windows Vulkan teardown raised a `vkDestroyImageView` access violation and an "unknown software
  exception" popup; the focused headless tests and application checks did not reproduce it.
- **Decision changed or retained:** distribute the self-contained participant build and collect the five
  unfamiliar-tester records. Keep M1 pending and do not select a body or control until that evidence is
  scored.

### 2026-08-31 — M1 recorded interactive package validation

- **Commands run:** `dotnet test "Projects/Blobbo Playground/Tests/Blobbo Playground.Tests.fsproj"
  --no-build --no-restore`; the narrow Playground build; the Debug and freshly extracted Release
  executables with `--verify-m1`; and the same self-contained publish command recorded above.
  Window-targeted `ffmpeg` capture used `gdigrab` at 30 fps while Orca dispatched window-local clicks
  and drags; `ffprobe` measured each recording and representative frame sheets were visually inspected.
- **Results:** the build passed with zero warnings and zero errors, all 5 tests passed, and both verifier
  runs passed all 16 checks with trace `5013E3E41D876A49`.
- **Recorded interactive checks:** four 640x360 recordings contain 9,206 frames over 324.18 seconds.
  They show body and room selection; live Grab, Pull, and Swipe contact feedback; bounded force or
  impulse; release trajectories and target outcomes; deterministic replay motion; reset; and Quit.
  Inspected gesture sequences show Grab reaching its bounded 2,400 force, Pull and Swipe updating the
  attempt and trace telemetry, and the body remaining stable without clipping.
- **Gaia dispatcher check:** the Playground Gaia-configuration build passed with zero warnings and zero
  errors. Two clean 1280x720 Gaia recordings contain 1,175 frames over 40 seconds and 1,605 frames over
  55 seconds. They separately show M1 loading and a drag producing a 24-sample trace and one attempt,
  then M1 Quit returning to the Playground title while Gaia remained alive. A broader `dotnet build
  "Nu/Nu.Gaia/Nu.Gaia.fsproj" -f net10.0 --no-restore` attempt ended with a bare `Stack overflow.` after
  Nu.Pipe and remains an environmental blocker.
- **Observed failure and fix:** recording the previous package exposed that direct-launch Quit returned
  to the hidden title state instead of ending the process. Direct unaccompanied M1 mode now calls
  `World.exit`; ordinary Playground and Gaia use still returns to the title. In the republished package,
  the recorded Quit click ended both the game and command launcher, the 15-line runtime log contained no
  error, exception, fatal, panic, or failed entries, and no exception popup was present.
- **Participant build:** `bin/Blobbo-M1-Playtest-win-x64.zip` is a 90,122,655-byte self-contained archive
  with 238 entries and 196 physical assets. It contains no `Log.txt`, `imgui.ini`, or `ShaderCache`
  entries. Its SHA-256 is `EC1F8D0ABA142BAFA93DDE13D4AA6CC8E705BBA9531871331BF59648311C9F11`.
- **Human test protocol and participant count:** none; 0 / 5 unfamiliar testers. Recorded agent-driven
  interaction validates the executable behavior but does not satisfy or change the M1 human gate.

### 2026-09-01 — M1 gesture-feedback visual refinement

- **Commands run:** `dotnet test "Projects/Blobbo Playground/Tests/Blobbo Playground.Tests.fsproj"
  --no-restore`; `dotnet build "Projects/Blobbo Playground/Blobbo Playground.fsproj" --no-restore`;
  the Debug executable with `--verify-m1`; window-targeted Orca interaction; `ffmpeg` recording and
  representative-frame extraction; and a current-session runtime-log scan.
- **Results:** all 6 focused tests passed, the Playground build passed with zero warnings and zero
  errors, and all 16 executable verifier checks passed with trace `5013E3E41D876A49`. The runtime
  session exited cleanly and contained no error, exception, fatal, panic, or failed entries.
- **Recorded interactive checks:** inspected before, during, and after frames for Grab, Pull, and Swipe.
  Active gestures now replace the ambiguous `READY` label with mode-specific aiming text. Swipe adds a
  bounded orange direction-and-strength arrow that clears on release; Grab retains its force tether and
  halo, and Pull retains its sling trajectory and halo. Release frames show `IN FLIGHT`, followed by the
  resulting miss state in the exercised Swipe sequence.
- **Body visual comparison:** inspected Legacy, Ring, and Hull frames at the same scene scale. All three
  candidates now share readable eyes and motion-oriented pupils; the eyes rotate with each physical
  center and remain centered on the rendered contour, including the legacy deformable graph.
- **Human test protocol and participant count:** none; 0 / 5 unfamiliar testers. This recorded visual
  refinement validates implementation behavior but does not satisfy or change the M1 human gate.

---

### 2026-09-12 — final pinned-upstream and participant-build validation

- **Revisions:** `upstream/master` / `MERGE_HEAD` at `3c2e2a3a0a7d7dff71162d6ff1200ea47e4b1a99`; `origin/blobbo`
  remains `33fb2dd796`. The late upstream swapchain-limit repair treats Vulkan `maxImageCount = 0` as
  unbounded and is distinct from the local empty-texture lifetime repair.
- **Automated results:** 13 selected Nu tests passed; a fresh Playground run passed 34 tests with 0
  failures/skips and normal native cleanup (31 pure, 3 integration); the M1 verifier passed 16 / 16 with
  trace `5013E3E41D876A49`. Production built cleanly, and the Playground self-contained Release publish
  completed with 196 physical assets.
  Logs are under `C:/Users/hadri/AppData/Local/Temp/blobbo-integration-20260912/`.
- **Project interaction scope:** all 9 body/control combinations passed live gestures on the fresh 29-test
  build; invalid starts, playfield exit, and mid-gesture pause were exercised. Repeat input uses a fresh
  body anchor and bounds/contact cancellation. Fresh candidate GUI checks passed Scene01 pause/drag,
  Scene02 cyan historical target and preview flow, Scene04 inset eyes/trails, Scene05 thermal pause,
  Combined water/heat/steam/fan/rewind, and Repeat completion/cancellation. Scene03's final
  `3x + 4x + 6 → 6 + 7x` remained fully visible above the footer and stable in later frames;
  extracted-package verification passed, and both fresh-extraction launchers completed their GUI checks.
- **Recorded matrix evidence:** `final-m1-matrix.mp4` is H.264 1280×720, 180.000 seconds, 5,060 frames,
  13,968,680 bytes, covering the 9 combinations on the 29-test build before the later Repeat, Scene01,
  and Scene03 updates. Its nominal 30 fps is not treated as a measured stable rate.
- **Window issue:** actual maximize/fullscreen produced stale or duplicated rendering. Debug PID 10632 was
  force-stopped after close was ignored, so that run is not clean-exit evidence and the root cause remains
  unestablished. The fixed-size path remained stable; minimize/restore recreated the surface correctly.
- **Review package:** `Blobbo-Playtest-20260912-Windows-x64-review-candidate.zip` is 90,178,719 bytes with
  SHA-256 `4FD80E7F37D55FAC1807F599022AF37AC5A818ADDF236DB1EC539B1C309E68AD`. Its extracted executable
  hashes to `88AB977D262A808E1F6114CA818C4C7770A0BC38FD8DD5CA192CD14FB42FC8AB`, matching the published
  binary. The extracted package contains 196 physical assets with 0 missing, 0 mismatches, no junctions,
  and no `Log.txt`, `imgui.ini`, or `ShaderCache` entries. Extracted `--verify-m1` passes 16 / 16 with
  trace `5013E3E41D876A49`; launchers from a fresh ZIP extraction passed. The M1 launcher created the
  expected 640×360 client (PID 37144), and Combined created the expected 1280×720 client (PID 45600).
  Menu/Exit returned command exit 0; both runtimes had stderr 0 and normal Vulkan surface destruction.
- **Human gates:** Nu source human review is unrecorded and remains required before opening an upstream PR;
  ordinary fork commit/push is not blocked by that upstream gate. Separately,
  0 / 5 unfamiliar testers have run the M1 protocol. Agent validation satisfies neither gate, so M1 remains
  pending and no body/control selection is promoted. Remaining technical limitations are fixed-size
  desktop validation only: maximize/fullscreen stale rendering remains unresolved, and mobile/touch,
  DPI, multi-monitor, and other-GPU paths were not tested.

## 20. Next implementation task

Complete and validate the integrated prototype playtest under the 2026-09-12 M1 scope amendment, then
collect outsider feedback on combined mechanics alongside the controlled M1 human gate. Record discovery,
voluntary repetition, directional/strength prediction after three attempts, control preference, and the
specific mechanic combinations that help or interfere. Do not begin M2, media, generation, enemies, story,
or production art until the applicable evidence is recorded.
