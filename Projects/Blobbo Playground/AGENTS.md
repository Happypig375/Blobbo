# Blobbo Playground agent instructions

`Blobbo Playground` is an **executable gameplay laboratory** for isolated hypotheses that support the product plan in `../Blobbo and Chrono/PLAN.md`.

It is not the actual game, a campaign/content branch, or the authoritative home of production architecture. Do not turn the whole project into a unit-test assembly: input feel, rendering, soft-body behavior, audio-visual feedback, and full-scene physics require an interactive executable. Automated tests belong in a separate Blobbo-specific test project when deterministic production logic exists to test.

Before changing this project:

1. Read the repository-root `AGENTS.md`.
2. Read `../Blobbo and Chrono/PLAN.md` completely and implement only its current milestone.
3. Read `../Blobbo and Chrono/PROJECT_STRUCTURE.md` for project ownership, promotion, testing, and `.nugroup` rules.
4. Treat every playground scene as a controlled experiment, not automatic production architecture.
5. State or preserve the experiment's hypothesis, comparison variants, controlled conditions, telemetry, deterministic reset/fixture, and acceptance evidence.
6. Preserve the existing baseline behind configuration when comparing bodies, controls, rewind, water, obstacles, or presentation.
7. Prefer one uncertain gameplay element or one comparison axis per focused scene. For the integrated interaction playtest authorized in the plan's 2026-09-12 amendment, make the existing mechanics available in one accessible playtest and combine relevant mechanics in purposeful interaction scenarios. Room switching is welcome; do not force every prototype into one physical room. Preserve isolated scenes as controls. Integrated feedback does not replace the controlled M1 comparison or its human gate.
8. Add bounded telemetry, deterministic reset/replay fixtures, and explicit evidence before drawing conclusions.
9. Do not promote an experiment to `../Blobbo and Chrono/` until the relevant code-ready and human gates in the plan are satisfied.
10. When an experiment is promoted, move or reimplement the minimum selected behavior under production ownership. The actual game must never add a project reference to this project or load assets from it.
11. A promoted production/core component may later be referenced **from** the playground for a regression or tuning harness; dependency must never point from the actual game into the playground.
12. `.nugroup` files in this project are experimental scene/template assets. Production must copy or re-author selected groups under its own assets and remove experiment-only entities and configuration.
13. Record exact build/test/profile commands and results in the plan's evidence log; do not mark human gates complete without human playtest evidence.

Keep generated binaries, media, captures, caches, model weights, and temporary analysis output out of Git.

Do not place offline analyzers, browser-integration spikes, model runtimes, or batch generator fuzzers here merely because they are experimental. Prefer a dedicated tool or adapter project when the research is not primarily an interactive gameplay element.