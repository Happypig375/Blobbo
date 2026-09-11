# Blobbo and Chrono agent instructions

These instructions apply to `Projects/Blobbo and Chrono/` and take precedence over repository-wide
instructions when more specific.

This project is the **actual shipping game**. It owns production gameplay, product-domain state,
generated-world architecture, shipping assets, saves, progression, platform adapters, and behavior promoted
from experiments. It must never depend on `../Blobbo Playground/` code or assets.

## Required reading and authority

Before changing this project:

1. Read the repository-root `AGENTS.md` and the routed skills under `.agents/skills/`. The canonical skill
   path is `.agents/skills/`; treat any legacy `.github/skills/` reference in older planning text as stale.
2. Read `PLAN.md` completely. It defines the current product hypothesis, milestone order, acceptance gates,
   and explicit non-goals. Read `EXPERIENCE_AND_MEDIA.md` as its 2026-09-11 scoped amendment for listening
   atmosphere and provider-operation review; it does not advance or replace the current milestone.
3. Read `PROJECT_STRUCTURE.md`. It defines project ownership, experiment promotion, test placement, and the
   role of Nu-native serialized entity trees.
4. Read `ARCHITECTURE.md`. It describes the system actually implemented; update it rather than preserving an
   obsolete prototype assumption.
5. Inspect the exact current code, assets, scene files, tests, and relevant Playground experiment before
   making an implementation claim.

When documents disagree, current executable behavior and tests establish what exists; `PLAN.md` and its
scoped amendment establish the intended product and gates; `PROJECT_STRUCTURE.md` establishes where it
belongs. Record a reconciliation instead of silently choosing the most convenient statement.

## Milestone discipline

- Implement the **current milestone only**. Do not begin a later milestone or fold deferred features into the
  current change.
- Build the smallest typed, deterministic, inspectable experiment or production slice that can falsify the
  milestone hypothesis.
- Keep code-ready evidence separate from human evidence. A build or telemetry result does not prove feel,
  readability, delight, fatigue, or listening continuity.
- Update milestone checkboxes only for work actually completed. Human gates remain pending until humans
  perform the playtest.
- Record exact commands, results, observations, and blockers in `PLAN.md`'s evidence log.

A milestone change is incomplete without appropriate tests or executable validation, debug/inspection
support, documentation updates, and an explicit statement of what was not validated.

## Production and experiment boundary

- `Blobbo and Chrono` must not reference `Blobbo Playground`.
- Production assets must not load from the Playground directory.
- Experimental namespaces, scene addresses, provider tokens, and configuration must not enter production
  save or generation formats.
- A successful experiment is distilled and re-owned by production through an explicit promotion. Do not
  consume it in place or import the Playground wholesale.
- Inspect a Playground experiment only when the current milestone names it. Preserve the controlled baseline
  and promote only the behavior supported by evidence.

## Product architecture invariants

- Prefer typed, immutable domain values for musical truth, media features, generation keys, pattern
  contracts, blueprints, validation results, snapshots, and save formats.
- Media analysis and MuScriptor may propose a versioned feature timeline or symbolic enrichment. Exact F#
  generation, validation, physics, and state transitions remain authoritative and must be reproducible
  without model-specific tokens.
- Use Nu-native `.nugroup` files for authored scene shells, rigs, fixtures, and pattern templates where Gaia
  editing helps. Keep seed derivation, provenance, pattern compatibility, safety policy, difficulty mapping,
  culling, runtime mutation, and save semantics in typed versioned systems.
- Keep mutable I/O and platform integration at explicit service/adapter edges. Dispatchers translate Nu
  lifecycle, events, and completed service results into model transitions; they do not own browser, capture,
  upload, or analyzer process lifecycles.
- Do not block Nu's render/update loop. External work is bounded, cancellable, and returned through explicit
  queues or completed results.
- Keep every queue, history, cache, generated segment window, recovery buffer, and runtime entity population
  bounded.
- Preserve reconstructability: a versioned source/features + seed + generator versions should reproduce the
  initial blueprint, while runtime mutation and Chrono state remain explicit.

## Compliance and external media

Do not introduce YouTube downloading, server-side extraction, hidden capture, redistributed model weights,
or a release dependency on a policy- or licence-uncleared adapter. Owned-media and synthetic paths must remain
usable independently of provider-specific research.

Apply the operation-specific review in `EXPERIENCE_AND_MEDIA.md`. User-selected links, private playback,
browser permissions and avoiding an SDK are not blanket clearance; equally, do not assume every independent
browser feature is governed by an API contract it does not use. Provider detection is a capability control,
not a bypass. Keep atmosphere hypotheses separate from claims about law, retention or willingness to pay.

Keep provenance, source identity, consent/licensing assumptions, analyzer version, and failure status
explicit at adapter boundaries. Do not allow external identifiers or model output to become unversioned game
state.

## Validation and documentation

Run the narrow project build first, then the checks required by the changed layer:

- pure generation/domain work: deterministic fixtures, validation, serialization, and owning-project build;
- Nu scene/dispatcher/facet work: build, Gaia load/reload, runtime interaction, and logs;
- promoted physics/control work: automated invariants plus the named human comparison gate;
- asset work: asset graph/default propagation where relevant, build, Gaia/runtime load, and visual inspection;
- adapter work: deterministic fake/fixture path, cancellation/failure cases, bounded queues, and no dependency
  on a live provider for core tests.

Update `ARCHITECTURE.md` when runtime architecture changes, `PROJECT_STRUCTURE.md` when ownership or authoring
boundaries change, and the `PLAN.md` decision/evidence logs when evidence resolves or reverses a choice.
Documentation-only changes must still verify references and preserve evidence limits; do not claim an
unperformed build, human gate or legal audit.