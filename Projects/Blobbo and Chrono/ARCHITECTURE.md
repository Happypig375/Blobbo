# Blobbo and Chrono architecture

> This file describes the architecture that is **actually implemented in the shipping-game project**. `PLAN.md` defines the target product and milestone order; `EXPERIENCE_AND_MEDIA.md` is its scoped 2026-09-11 experience/media amendment. `PROJECT_STRUCTURE.md` defines ownership, promotion, tests, and Nu-native authoring boundaries.

## Project boundary

`Projects/Blobbo and Chrono/` is the actual game. It owns production gameplay, domain state, generated-world architecture, shipping assets, saves, platform adapters, and behavior that has passed promotion gates.

`Projects/Blobbo Playground/` is a separate executable laboratory for isolated comparisons and combined
interaction experiments under the 2026-09-12 plan amendment. The actual game must not reference its
assembly or load its assets. A selected experiment is moved, distilled, or reimplemented under production
ownership before the game uses it.

Automated deterministic tests should eventually live in a separate Blobbo-specific test project referencing the smallest justified production code. The playground remains interactive because control feel, rendering, and whole-scene physics cannot be reduced to unit tests.

## Nu-native authoring and serialized groups

Nu's `.nugroup` format is relevant as an authored entity-tree source. The current gameplay screen loads `Assets/Gameplay/Scene.nugroup` through `World.beginGroupFromFile`; the file is an S-expression tree of dispatchers, properties, and child entities.

Use `.nugroup` for stable authored composition such as:

- scene shells;
- fixed rooms and deterministic fixtures;
- visual/audio rigs;
- reusable obstacle or enemy archetypes;
- hand-authored pattern templates;
- production dressing and named marker entities.

Do not make serialized groups the sole authority for media features, generation keys, pattern selection, difficulty mapping, safe-path contracts, solvability, culling, runtime mutations, or save semantics. Those remain typed and versioned.

The intended hybrid is:

```text
.nugroup template or typed generated entities
+ typed PatternContract
+ deterministic media/seed parameters
-> immutable WorldBlueprint
-> Nu entity instantiation and typed overrides
-> mutable WorldRuntimeState
```

A pattern may be fully generated in F#, fully authored as a `.nugroup` template with typed overrides, or combine the two. Do not construct S-expression source through ad-hoc string concatenation. Contract metadata remains explicit; any named markers inside a group must be validated against it.

An optional future developer export may serialize a generated segment for inspection or Nu/Gaia editing. Such an export is not the canonical generation or save format until round-trip identity, migrations, custom state, and runtime-only physics behavior are verified.

## Currently implemented integration shell

Nu remains the host for the window, render loop, and lifecycle. `CompositionRoot` is a runnable integration shell started and stopped by `Program`; gameplay installs a shared instance, pumps browser audio without blocking, and renders latest snapshot status.

* **Browser.** The interface is designed for an external browser process/extension and transparent top-level overlay on desktop, or an owned browser and composited surfaces on mobile. These are intended platform routes, not implemented adapters. Both are represented by the narrow `IBrowserBridge` contract and `BrowserEvent` values. Platform window handles and IPC stay outside this project.
* **Audio.** `AudioIngress.Submit` accepts mono 48 kHz analysis samples, copies them into bounded, preallocated analysis and playback rings, and advances an authoritative `int64` source-sample clock. Each slot carries its absolute start position and valid count; producers drop new blocks under backpressure and consumers detect discontinuities. Playback timing uses the same mono clock; a future stereo adapter may interleave playback samples. `CompositionRoot.TryReadPlayback` exposes an allocation-free, non-blocking count/absolute-position drain for the private delayed-playback ring.
* **MuScriptor seam.** `MuScriptorCoordinator` consumes independent five-second windows with 300 ms overlap and emits sample-positioned symbolic events. Defaults are `prelude_forcing=false`, beam 1, batch 1. `ISymbolicInference` is a placeholder seam for a future model and has no runtime dependency today.
* **Simulation shell.** `SimulationWorker` currently translates symbolic events into immutable render snapshots. Rendering reads the latest snapshot without taking a lock.
* **Scene composition.** `Gameplay.fs` loads the Nu-native `Assets/Gameplay/Scene.nugroup`, renders a temporary static model and live read-only diagnostic text, and does not yet contain the production Blobbo journey loop.

`NullBrowserBridge` and `NullInference` make the shell build and run offline. Failure and overload are isolated to the corresponding bounded queue; platform browser/audio/model adapters, click-through focus policy, and native compositing remain to be implemented per platform. The current desktop shell uses an ordinary opaque window. On 2026-09-12, interactive validation found that the prematurely enabled transparent/always-on-top flags exposed unrelated desktop content through UI text. Those flags are deferred until a platform compositor and focus policy can be validated together; this does not implement or reject the intended browser-overlay route. Actual maximize/fullscreen interaction also showed stale or duplicated rendering; the root cause is not established, so the participant path remains fixed-size while that window path is reviewed. Start/stop operations are idempotent and serialized; worker shutdown joins fully. A future inference adapter that cannot be interrupted must be treated as a non-restartable fault.

### 2026-09-11 source-review clarification

At baseline commit `7d7840461cbd1118da52c4e6f02cfbc6a82474a9`, the composition root defaults to `NullBrowserBridge`; the existence of browser events, overlay flags and PCM rings does not demonstrate a functioning desktop extension or mobile browser fork. The PCM copying and delayed-output seams also make a future implementation more than a bookmark manager. A complete synchronised playback scheduler, provider-specific processing permissions and a release capability boundary are not implemented or validated by this documentation pass. See `EXPERIENCE_AND_MEDIA.md` for the proposal and review requirements; no runtime behaviour changed here.

## Known prototype assumptions to replace through the plan

The current shell is not yet the product architecture in several respects:

- symbolic events become obstacles too directly;
- no versioned `MediaFeatureTimeline` is implemented;
- no deterministic `GenerationKey` or `WorldBlueprint` exists;
- obstacle history has no complete spatial retention/culling contract;
- playback capacity does not implement a complete synchronized-output policy;
- no pattern library, contracts, safe envelopes, or generation validator exists;
- no production Blobbo or Chrono behavior is integrated;
- no queue/next-item journey state exists;
- no production distinction exists yet between media-locked Chrono Recall and player-paced rewind.

Implement these replacements only in the milestone order in `PLAN.md`. Update this file when each boundary becomes real; do not describe planned types as implemented before they exist.
