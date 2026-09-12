# Nu integration review — 2026-09-12

Status: pre-publication review snapshot; upstream review remains pending. Human review of Nu-owned code
and upstream-divergent comments is required before opening an upstream PR (including a draft).

## Revisions and scope

- Blobbo instructions: `origin/blobbo` at `33fb2dd796`, fast-forwarded from `7d7840461c`.
- Nu baseline: `upstream/master` / `MERGE_HEAD` at `3c2e2a3a0a7d7dff71162d6ff1200ea47e4b1a99`.
  The initial integration used `bd6356828703443d71c0e90dbd32c2bf9bfe9468`; a remote recheck during
  testing found one further upstream commit, whose three-line swapchain-limit repair is now included
  exactly. The pending merge parent and local `master` were advanced without committing or discarding
  the local work; the previous index, tracked diff, and affected files were backed up first.
- At this review snapshot, local `master` matched that upstream revision. All four merge conflicts were
  resolved; the merge into `blobbo` was awaiting fork publication.
- Backup: `backup/blobbo-pre-sync-20260912` at `33fb2dd796`.
- Product scope: one accessible participant playtest with room switching, focused comparisons, and useful
  combinations of existing mechanics. Not every prototype must occupy one physical room. Production does
  not acquire a dependency on Playground, and the controlled M1 human gate remains pending.

The current Playground implementation also includes the reviewed project-only interaction repairs:
M1Blobbo remembers the position produced by `SetPerimeter`, so ordinary physics motion no longer rebuilds
Ring/Hull at their spawn position each frame; only external repositioning or fixture changes reset that
position. Pointer history is sampled in simulation time with an 80 ms cap; Repeat input anchors its stroke
to a fresh body, cancelling when contact is not acquired or the translated stroke leaves the playfield.
These preserve the comparison fixture rather than changing Nu behavior.

## Engine decisions requiring human review

The upstream merge retains Blobbo's existing configurable virtual resolution and viewing-margin behavior
while adopting upstream's new rendering/surface lifecycle and SSAO default. Four conflict resolutions are
in `Constants.fs`, `Viewport.fs`, `WorldConfigure.fs`, and `WorldModule2.fs`. Retaining this existing branch
behavior is a merge decision, not a claim that upstream adopted these APIs.

Review the full resulting delta against the pinned upstream, not just this turn's edits. It includes
inherited viewport/input/render coordinate changes, runtime distance-joint APIs and tests, and related
sample configuration. The first independent semantic review found no blocking merge omission, but called
out two inherited policies for human attention: resize can alter display scalar/fullscreen/window size;
resolution rollback restores native and managed state without another synchronous viewport reconciliation.

### Empty-texture lifetime repair

The pre-fix graphical World tests aborted with native access violation `0xC0000005` in
`vkDestroyImageView`, reached through `TextureInternal.destroy` on renderer cleanup. Upstream's
`Hl.initEmptyTexture` only assigns an empty cache, but both renderer cleanup paths destroy the cached
texture without clearing that cache. A later World can therefore retain a destroyed texture from the
previous Vulkan device. This is a source-backed diagnosis, strengthened by the regression result below.

The draft clears `Hl.EmptyTextureOpt_` immediately after empty-texture destruction in both inline and
threaded renderer cleanup. It preserves the existing one-live-renderer/global-cache architecture; it does
not introduce support for simultaneous renderer contexts. The lifecycle decision and its two rationale
comments require human review.

### Swapchain surface-limit repair

The late upstream pin also contains a distinct three-line swapchain repair: Vulkan reports
`maxImageCount = 0` as no upper limit, so the requested image count must not be rejected against zero.
This is separate from the cached empty-texture lifetime crash and is retained as upstream code, not a local
diagnosis. See [the upstream commit](https://github.com/bryanedds/Nu/commit/3c2e2a3a0a7d7dff71162d6ff1200ea47e4b1a99) and
the [Vulkan surface-capabilities reference](https://docs.vulkan.org/refpages/latest/refpages/source/VkSurfaceCapabilitiesKHR.html).

### Shutdown before the first frame

The production asset-load failure also exposed an independent shutdown race: the renderer's wait loop
can exit because termination was requested, then unconditionally read an empty submission option before
its existing termination guard. The draft moves the guard ahead of submission destructuring, without
adding lifecycle state. A new World-level integration test constructs a real accompanied World and cleans
it up without submitting a frame. It passes alongside the two existing graphical lifecycle tests.

History and external evidence:

- No matching Nu issue or pull request was found in the searches performed; this is not proof none exists.
- [Commit `0c81c30`](https://github.com/bryanedds/Nu/commit/0c81c30d548b6adc1c9b67274b3012cb46ef0060)
  introduced the guarded `initEmptyTexture` on 2026-08-19, replacing unconditional cache assignment. Its
  message is “More code reorg.”, not an explicit investigation of this crash. Later Vulkan refactors retain
  the pattern; their proximity is not evidence they diagnosed it.
- [Pinned upstream initialization](https://github.com/bryanedds/Nu/blob/3c2e2a3a0a7d7dff71162d6ff1200ea47e4b1a99/Nu/Nu/Vulkan/VulkanHl.fs#L375)
  and [renderer lifecycle](https://github.com/bryanedds/Nu/blob/3c2e2a3a0a7d7dff71162d6ff1200ea47e4b1a99/Nu/Nu/Render/RendererProcess.fs)
  still contain the relevant pattern.
- [Vulkan object lifetime](https://docs.vulkan.org/spec/latest/chapters/fundamentals.html#fundamentals-objectmodel-lifetime)
  forbids access after destruction and restricts device-created objects to that device.
- [`vkDestroyImageView`](https://docs.vulkan.org/refpages/latest/refpages/source/vkDestroyImageView.html)
  requires a valid image-view handle and its originating device (`imageView-parameter` and
  `imageView-parent` VUIDs). Its separate `01026` rule requires completed GPU use; waiting for idle does not
  make an already-destroyed handle valid.

## Comment audit

The independent comment review compared the working tree with the pinned upstream. Retained new comments
correct the configured/base/current viewport bounds distinction, minimized-window resynchronization, the
units of ToyBox's viewing margin, and the renderer's empty-texture lifetime. These are contract/rationale
corrections, not stylistic rewrites. Inherited comments explaining branch-only APIs and behavior remain
part of the human review scope.

Removed from this turn: a purely editorial distance-joint wording change, a test comment repeating the
test's name, and accidental end-of-file/encoding changes. The existing awkward distance-joint wording is
left unchanged; the reviewer classified it as non-blocking and advised against opportunistic cleanup.
The final independent working-tree re-audit found no surviving comment-edit artefact and no new P0/P1
finding. It confirmed the restored viewport wrapper documentation and upstream EOF convention. An agent's
approval does not satisfy the human-review gate. Recheck the final staged delta before committing.
The remaining 14 source-file comment differences are retained because they explain contracts or rationale;
they are material review items rather than editing artefacts. The removed items were artefacts or purely
editorial changes.

The exact 14 outside-project source files with code-comment differences against the pinned upstream are:

- `Nu/Nu.Tests/Box2dNetPhysicsEngineTests.fs`
- `Nu/Nu/Core/Constants.fs`
- `Nu/Nu/Core/Globals.fs`
- `Nu/Nu/Physics/PhysicsEngine.fs`
- `Nu/Nu/Render/RendererProcess.fs`
- `Nu/Nu/Sdl/SdlDeps.fs`
- `Nu/Nu/Transform/Viewport.fs`
- `Nu/Nu/World/WorldModule.fs`
- `Nu/Nu/World/WorldModule2.fs`
- `Nu/Nu/World/WorldModuleGame.fs`
- `Nu/Nu/World/WorldPhysics.fs`
- `Nu/Nu/World/WorldTypes.fs`
- `Projects/Mobile/Program.fs`
- `Projects/Sand Box 2d/ToyBox.fs`

### Human decision checklist

- Review the entire Nu delta, including inherited distance-joint and viewport APIs, plus the four merge
  resolutions and both renderer lifetime repairs.
- Accept or revise the existing one-live-renderer/global empty-texture ownership assumption.
- Decide the public viewport policy: an oversized `Eye2dSize` is not a zoom operation and can exceed the
  window/display-scalar projection invariant. The prototype uses the supported canonical 640×360 size;
  no new fractional-scaling architecture is proposed here.
- Review native-window/display-resolution changes and rollback behavior described above.
- Review all 14 comment-bearing files. The inherited tracked Sand Box `.fsproj.user` is retained rather
  than deleting user-owned state; decide separately whether to untrack it while preserving the local file.

No human approval has been recorded for an upstream PR. This does not block ordinary commit/push to the
user's `blobbo` fork; upstream PR opening remains gated.

The refreshed human-review artifact covers 44 outside-project files (1,530 insertions, 138 deletions)
against pinned upstream `3c2e2a3`; its SHA-256 is
`8FA510A5A9A1E500F481521BD3983C0435CB0600653684A689ADBA565765B465`.
This is an integration inventory for the fork and is not an upstream-ready PR patch because it includes
fork-only instructions and context. The 14 source-comment files remain independently reviewable before
any upstream PR.
If an upstream contribution is later prepared, eye field-of-view / eye-margin hunks target
`mveb/eye-margin`; other eligible Nu hunks target `master`, with mixed files split by behavior.

## Validation recorded so far

Environment: Windows, .NET SDK `10.0.302`, `net10.0`, Intel UHD Graphics 630 / Vulkan driver `1.3.215(0)`.
Commands use process-local TEMP/TMP and remove stale `alf-h-fixture-*` PATH entries; no global environment
or certificate-verification policy was changed. Git HTTPS worked with per-command Windows Schannel.
The initial results below used `bd63568`; fresh checks after the late `3c2e2a3` update are recorded
separately. That upstream swapchain fix is not an investigation of the cached empty-texture lifetime bug.

| Check | Observed result |
| --- | --- |
| Nu build | Passed, 0 warnings/errors. |
| Focused World/Box2D/fluid tests excluding Integration | 18 passed, normal exit. |
| World graphical lifecycle tests before cache reset | Aborted with native access violation; not a pass. |
| Same graphical lifecycle tests after cache reset | 2 passed, 0 failed/skipped, normal completion; includes three successive World lifecycles. |
| Graphical lifecycle tests after both renderer fixes | 3 passed, normal completion; includes cleanup before the first frame. |
| Playground focused NUnit tests before layout revision | 20 passed; one new FS0667 annotation warning identified for correction. |
| Default asset propagation | Ran Windows propagation; all 187 canonical default files match in each Blobbo project. |
| Production project build | Passed, 0 warnings/errors. |
| First production launch | Failed on old serialized `DirectionalLight`; secondary renderer termination exception also recorded. |
| Production rebuild after scene migration | Passed, 0 warnings/errors. Actual Play → rendered 3D model → Quit → Title → Exit succeeds; stderr empty and Vulkan surface teardown logged. |
| Gaia owning build (`-p:BuildProjectReferences=false`) | Passed, 0 warnings/errors. Playground loaded; asset reload, menu/Combined selection, and a real drag worked. Code reload compiled and updated with no warnings. |
| First Combined desktop frame | Failed visual check: a 960×540 eye at scalar 2 clipped against the 1280×720 window. Corrected to canonical 640×360. |
| Canonical-layout Playground build | Passed, 0 warnings/errors; 22 focused NUnit tests passed independently. |
| M1 headless verifier | All 16 checks passed, trace `5013E3E41D876A49`; separate from the 22 NUnit tests. |
| Desktop drag/contact checks | Real Grab before/during/after captures show deformation, collision, release and fan rotation. Heater ON/OFF changes visible. Remaining mode/body matrix still in progress. |
| Thermal visual iteration | Found status text over Blobbo and pause status over cooler. Project-only label/status repair built and visually checked in Gaia. |
| Steam cycle | Vent-above-perimeter repair validated live in Gaia: water absorbed, body shrank on heat, steam rose, 33+ particles condensed and fell. |
| Latest pure-test rerun | Fresh test-project build passed all 23 M1 tests. An earlier no-build rerun used the stale 22-test binary and is not evidence for the new vent test. |
| Rewind regression | First run aborted on missing test-output shaders; asset delivery repaired. A later explicit attachment check identified an unregistered game facet in the test harness, not a demonstrated ManualMotion failure. Explicit game-assembly registration repaired setup. Fresh complete suite: 24 passed, 0 failed/skipped, normal exit, including the World-level rewind regression. |
| Production presentation | Transparent/always-on-top shell exposed desktop content through UI glyphs. Deferred those unimplemented-compositor flags; ordinary opaque window, readable live status panel, Play/3D scene/Quit/Exit visually validated with empty stderr and normal surface teardown. |
| Final pinned-upstream focused tests | 13 selected Nu tests passed after updating to `3c2e2a3`; `nu-swapchain-focused.log`. The swapchain-limit repair is distinct from the empty-texture cache-lifetime repair. |
| Fresh Playground tests | 34 passed, 0 failed/skipped, normal native cleanup; 31 pure and 3 integration tests. See `final-repeat-alltests.log`. |
| M1 verifier | 16 / 16 passed, trace `5013E3E41D876A49`; `final-repeat-verifier.log`. |
| Playground Release publish | Self-contained Release publish completed successfully with 196 physical assets, exit 0; `final-release-publish.log`. |
| Final candidate GUI | Scene01 pause/drag, Scene02 cyan preview/commit/pause flow, Scene03 `3x + 4x + 6 → 6 + 7x` containment, Scene04 eyes/trails, Scene05 pause/thermal, Combined water/heat/steam/fan/rewind, and Repeat-input cancellation/commit were exercised with readable visuals and no runtime errors in `final-candidate-runtime.log`. |
| Gesture matrix recording | `final-m1-matrix.mp4`: H.264 1280×720, 180.000 seconds, 5,060 frames, 13,968,680 bytes. It records the 9 body/control combinations on the 29-test build before the later Repeat, Scene01, and Scene03 updates; nominal 30 fps is not asserted because the frame count is 5,060 rather than 5,400. |
| Window lifecycle | Fixed-size 1302×776 window (1280×720 client) remained stable; minimize/restore recreated the surface and rendered correctly. Actual maximize/fullscreen showed stale/duplicated rendering; Debug PID `10632` was force-stopped after close was ignored, so that run is not clean-exit evidence and the root cause remains unestablished. |
| Review candidate package | `Blobbo-Playtest-20260912-Windows-x64-review-candidate.zip`, 90,178,719 bytes, 241 entries, SHA-256 `4FD80E7F37D55FAC1807F599022AF37AC5A818ADDF236DB1EC539B1C309E68AD`. Both launchers passed from fresh ZIP extraction with startup scripts invoked from a temporary working directory; Menu/Exit returned command exit 0. The extracted package has 196 assets (0 missing/mismatched, no junctions, no `Log.txt`, `imgui.ini`, or `ShaderCache`); its executable SHA-256 `88AB977D262A808E1F6114CA818C4C7770A0BC38FD8DD5CA192CD14FB42FC8AB` matches the published binary. |
| Extracted launcher GUI | M1 opened at the expected 640×360 client with Ring/Toy; Grab moved the body before/during/after and incremented one attempt, and Swipe showed its arrow and released at 51 units/s. Combined opened at the expected 1280×720 client; Legacy, water/heat/steam/fan/rewind were visible, a real drag deformed/moved Blobbo, and the left balloon popped (13 fluid particles captured). Re-entering Combined reset balloons intact; a second attempt did not pop, and B was only idempotently checked while intact. Both runtimes had stderr 0, no error/warning/duplicate/sentinel/exception entries, and normal Vulkan surface destruction. |

The production-owned scene has been migrated to `[DirectionalLight 0]`, matching the current ImSim game
template and preserving zero forward offset. The final candidate GUI checks covered Scene01, Scene02,
Scene03, Scene04, Scene05, and Combined; Scene03's combined result remained fully visible above the
footer and stable in later frames. The extracted verifier passed. Both extracted launchers were then exercised successfully from a fresh extraction, with Menu/Exit
returning command exit 0 and normal Vulkan surface destruction. The review ZIP is retained at
`Projects/Blobbo Playground/bin/Playtest/2026-09-12/Blobbo-Playtest-20260912-Windows-x64-review-candidate.zip`.
The Combined layout retains the
canonical 640×360 eye; fractional scaling is not being added to Nu merely to accommodate this experiment.
The first Playground instance was stopped after a close request did not exit; this is not clean-exit
evidence. A later instance exited through Menu/Exit with normal surface teardown. Fresh Combined rewind
testing found no duplicate local-rewind-prop body, sentinel, or runtime error.
Gaia also logged an entity-sentinel warning on the old rewind fixture before repair. Its subsequent code
reload succeeded, and it exited through its confirmation dialog with normal surface teardown. The user's
pre-existing Gaia state file was backed up before launch and restored afterward.
Keyboard attempts rejected by the desktop helper's foreground check or zero-duration synthetic key taps
that did not produce an observable change are not successful tests.

Another source-level finding is not repaired in Nu: `Box2dNetPhysicsEngine.toFluid` changes a particle's
configuration using its existing position/velocity, ignoring simultaneous replacement position/velocity
fields. The prototype now relies on the phase's gravity instead of pretending those requested velocity
changes are applied. This contract deserves a separate upstream issue/test before changing the backend.

The project rewind repair retains the intrinsic rigid-body ID, explicitly adopts ManualMotion during
facet registration, restores the prior policy on removal, synchronizes live entity transforms and
committed pose/velocities, and never gives historical ghost renders their own physics bodies. The new
World-level regression checks dynamic attach/detach/reattach, motion/history, preview/cancel, direct commit,
destruction and same-address recreation. The inherited facet still restores Presence to Exterior after
preview; current prototype props are Exterior, so generic Interior/Omnipresent reuse is not validated.

The test project now copies the owning game's processed runtime assets after project-reference building;
it does not duplicate canonical source assets or redirect runtime file lookup. The first missing-shader
test-host abort remains recorded as a failure, not a passed assertion run.

The test plugin belongs to the test assembly. At the pinned revision,
`Reflection.loadReferencedAssembliesTransitively` recurses into each loaded assembly's references without
yielding that loaded assembly itself, so the game facet was absent from the resulting registry. The
diagnostic explicitly returned `Invalid facet name 'RewindableFacet'` and `registered: false`. Test setup
now uses existing `World.updateLateBindings` after normal World construction to register the game assembly,
and asserts registration and successful attachment. No Nu assembly-loading change was made. This setup
finding must not be presented as a failed product motion-policy assertion.

A project-review claim that manual-motion containers retained stale spawn positions was withdrawn after
tracing `SetPerimeter` through the Transform setter: it updates position and size. Explicit perimeter
selection for heat overlap/emission may clarify spatial intent, but is not evidence of that alleged bug.

Evidence directory (untracked):
`C:/Users/hadri/AppData/Local/Temp/blobbo-integration-20260912/`.
Relevant logs: `focused-tests-rerun.log`, `worldtests-integration.log`, `worldtests-cache-reset.log`,
`worldtests-guard-and-cache.log`, `playground-focused.log`, `production-build.log`,
`production-scene-rebuild.log`, `gaia-build.log`, `production-runtime.stdout.log` / `.stderr.log`,
`final-repeat-alltests.log`, `final-repeat-verifier.log`, `final-production-build.log`,
`final-release-publish.log`, `nu-swapchain-focused.log`, and `candidate-runtime.log`.
`nu-human-review.patch` in that directory records the full Nu/sample delta against the pinned upstream,
including inherited branch behavior, for human review.