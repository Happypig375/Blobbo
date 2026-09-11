# Listening atmosphere and external-media decisions

**Reconciled:** 2026-09-11, Hong Kong time.  
**Status:** scoped product-plan amendment, not implementation or legal clearance.  
**Read with:** [PLAN.md](PLAN.md), especially sections 2, 8, 10, 11 and 15. This document adds experience hypotheses and narrows provider-permission assumptions; it does not replace the milestone sequence, evidence log, or current M1 human gate.  
**Source baseline:** `blobbo` commit `7d7840461cbd1118da52c4e6f02cfbc6a82474a9`.

## Experience objective

Let the player participate in music they enjoy through a tactile, readable physics journey. Preserve the existing listening-companion identity: no mandatory note chart, exact-beat grading, or replay of a song after ordinary mistakes. The atmospheric direction refines the combined music-shaped Blobbo product; it is not a decision to create a separate nightclub simulator or to return to the older campaign-versus-music split.

Distinguish three outcomes when designing and observing play:

- **Musical involvement:** actions, anticipation and world response feel connected to the music.
- **Atmosphere of a place:** materials, motion, sound and visual identity form a coherent experience.
- **Interpersonal connection:** another person actually shares or responds to the experience.

These are design distinctions, not interchangeable metrics. The solo-first slice can pursue the first two without pretending to provide the third. A Discord community, cheering audio or animated spectators does not establish shared human presence. Any fictional audience must remain recognisably fictional; do not fabricate live players or reactions.

## Design hypotheses

**Continuity with intelligible recovery.** Ordinary collisions should lead to understandable Chrono recovery while Journey-mode music continues. The player can voluntarily pause or leave. Continuing automatically to another item is a convenience, not a reason to obscure stopping or pressure the player into a longer session.

**Legible response at several timescales.** Sections and phrases can change materials, motion amplitude, pattern character, lighting and rest/action balance. Beat-level cues should initially remain cosmetic or non-critical. Keep media intensity separate from difficulty. A player should be able to describe at least one relationship without reading analyser diagnostics.

**Invitation and response.** Explore recognisable motifs that invite a physical response, allow the result to unfold, and then vary. Do not interpret performer/audience interaction as a requirement to place an obstacle on every detected note. Immediate input feedback must remain clear even when expressive effects wait for a musical boundary.

**Contrast, not permanent maximum stimulation.** Test breathers, anticipation and release rather than merely increasing event count. Use editable F# mappings, authored scene/rig sources, shaders and parameters for durable work. A distinctive visual identity should communicate an experience the actual game delivers, not substitute for readable interaction.

**Voluntary participation and attention.** Quiet play and reduced visual motion are legitimate modes of participation. Preserve separate media/effects levels, reduced-flash options, readable silhouettes and non-audio alternatives for critical cues. Default effects should support rather than mask the music. Voice chat, social performance and sustained high-intensity input are not prerequisites.

**Monetisation must not interrupt the promised experience.** Free core plus optional authored content/cosmetics remains a business hypothesis. Do not interrupt songs or recovery with purchase prompts, sell basic continuity back to players, or infer willingness to pay from another industry's prices. Intentionally bounded sessions are compatible with success; session length alone is not proof of enjoyment.

## Evidence and transfer limits

The following are research leads reviewed in the 2026-09-11 discussion, not direct evidence that a Blobbo feature works. No new systematic review or playtest was performed for this amendment.

| Source | Finding used | Limit on transfer |
|---|---|---|
| Danatzis et al., *Curating the Crowd*, DOI [10.1177/00222429251328277](https://doi.org/10.1177/00222429251328277) | Berlin-club ethnography treats social atmosphere partly as participant fit, not decoration alone. | Abstract-level reading; qualitative and venue-specific. Do not copy exclusion practices or infer a game revenue effect. |
| Swarbrick et al., *Corona Concerts*, DOI [10.3389/fpsyg.2021.648448](https://doi.org/10.3389/fpsyg.2021.648448) | Attention, audio quality, performer interaction and liveness were among factors associated with reported connection. | Selected full-text results; observational virtual-concert evidence, not a causal feature recipe or proof that chat creates connection. |
| Davidson and Keene, *Alone in a Crowded Room*, DOI [10.1080/19376529.2018.1490911](https://doi.org/10.1080/19376529.2018.1490911) | The reported 60-participant experiment did not find increased enjoyment or imagery from added crowd noise. | Abstract-level result; a useful negative result, not proof all audience cues fail. |
| Witek et al., *Syncopation, Body-Movement and Pleasure in Groove Music*, DOI [10.1371/journal.pone.0094446](https://doi.org/10.1371/journal.pone.0094446) | Medium syncopation in the tested rhythms elicited greater pleasure and desire to move than the tested extremes. | Selected full-text results; not a universal game-difficulty or effects-density optimum. [2015 correction](https://doi.org/10.1371/journal.pone.0139409) was checked; authors state the stimulus corrections do not change findings. |

## Placement in existing milestones

No new milestone, live task list, VR feature, multiplayer service, browser fork or production art programme is authorised by this document.

- **M1:** retain the existing controlled body/control comparison and pending human gate unchanged. Do not add atmospheric variants that confound it.
- **M2/M4:** observe whether recovery, readable feedback and advancing-world pressure support participation. M4 remains a synthetic-clock integration; do not claim it demonstrates real musical coherence.
- **M5:** refine the already planned matched-mapping comparison using owned or explicitly licensed tracks. Hold physics, route, difficulty, audiovisual event density and sound levels as comparable as practical; vary meaningful correspondence versus a time-shuffled or otherwise uncorrelated mapping. Counterbalance order. Separate non-critical presentation tests from geometry changes.
- **M6:** evaluate next-item continuity together with clear stopping, not only retention.
- **M7/M8:** assess concrete provider/analyser operations under the boundary below; atmosphere does not justify bypassing the review.
- **M9:** retain the existing vertical-slice gate. Evaluate musical involvement, coherent presentation and attention cost separately. A small pilot diagnoses problems; it does not establish market demand or a population-level effect.

For an atmosphere comparison, record track/permission, fixture, mapping/version/seed, variant order, tester familiarity, assistance, observed failures, whether a relationship was noticed without prompting, reported enjoyment/effort, and reasons for continuing or stopping. Predeclare what would lead to retain, revise or reject. These are future experiment requirements, not completed observations.

## Browser hypothesis and actual implementation

**Product proposal:** players choose websites and save ordinary links/bookmarks; desktop may use an external browser plus extension, while mobile may use an owned/forked browser. This remains a candidate access architecture, not a completed implementation or proof that responsibility transfers entirely to the player.

**Source finding at the baseline commit:** `Architecture.fs` exposes `IBrowserBridge`, navigation/playback events and mono PCM input. `AudioIngress.Submit` copies samples into analysis and playback rings. `CompositionRoot` defaults to `NullBrowserBridge` and `NullInference`. The documented desktop/mobile adapters, native composition and complete synchronised output remain unimplemented. Buffer capacity does not establish a working delay scheduler.

The operation chain potentially includes more than bookmarking:

```text
user navigation / playback
-> optional raw-sample access and copying
-> feature extraction or model inference
-> optional retention / delayed output
-> generated gameplay
```

Review those operations separately. Choosing a song, granting browser capture permission or naming the host a browser does not by itself authorise every later operation. Conversely, a restricted API policy is not automatically the governing contract for every independent browser feature. Do not declare the entire browser approach either cleared or unlawful from the label alone.

## Release boundary by operation

| Route | Planning boundary |
|---|---|
| Synthetic fixtures and feature files with valid provenance | Independent core/test path; confirm provenance and any rights in supplied data. |
| Local media with permission for the intended processing | First real-media path. Possession or purchase alone must not be relabelled permission for every reuse. |
| Ordinary user-directed browsing/bookmarks | Candidate playback path; assess the actual integration. Do not silently attach capture, replay or inference. |
| Supported visible YouTube embed | Existing M7 feasibility baseline. Preserve applicable player behaviour; use separately permitted features or a clearly labelled fallback when raw features are unavailable. |
| Independent extension or mobile browser integration | Separate implementation-specific feasibility comparison, not an assumed exemption and not a replacement of M7 by default. Record applicable service terms, developer terms where used, distribution and platform constraints. |
| Spotify content into MuScriptor/another AI model | Not an approved path. Spotify's ordinary user guidelines contain a model-ingestion restriction, independently of the developer-policy question [P1]. |
| Live capture, raw retention, delayed replay, transcription or sharing | Review each operation and exact output. Keep uncleared adapters outside release capability; the game remains usable without them. |

Provider detection may enforce permitted capabilities and disable unsupported analysis; it is not a technique for concealing the same operation or manufacturing permission. A navigation/source change must invalidate prior approval assumptions, stale samples and future events before processing resumes. Do not silently capture unrelated tabs or keep browsing history, tokens or credentials in diagnostics. Implement only the smallest capability boundary needed by the active adapter milestone, not a speculative policy engine now.

For each concrete adapter review, record provider, route/API, jurisdiction and distribution markets, dated policy URLs/sections, operations, data path and retention, user permission, model licence if any, public/private use, decision-maker and unresolved questions. Distinguish technical feasibility, user consent, copyright, contractual restrictions and distribution rules. Permission may arise from applicable terms, a specific licence, a supported legal exception or another documented basis; not every operation necessarily requires a separately purchased music licence.

The review must distinguish temporary buffers, low-level features, symbolic transcription and retained media. It must also distinguish ML inference from training, and both from ordinary signal processing. Removing a model removes that particular operation; it does not automatically clear remaining capture or copying. No DRM/access-control bypass, hidden playback/extraction, or server-side YouTube downloader is authorised.

## Dated policy anchors

These are planning risk records, not legal advice or a conclusion about enforceability against a particular party. Recheck the exact applicable versions at adapter/release review.

- **[P1] [Spotify User Guidelines, Hong Kong English](https://www.spotify.com/hk-en/legal/user-guidelines/), checked 2026-09-11:** item 5 covers ingestion of Spotify content into an ML/AI model as well as training. This is relevant to inference, not only model training. Ordinary playback and the analyser's additional operations remain distinct.
- **[P2] [Spotify Developer Policy](https://developer.spotify.com/policy), checked 2026-09-11:** the retrieved policy is effective 2025-05-15 and includes restrictions on games, audiovisual synchronisation and analysis/model use through the Spotify Platform. Establish platform applicability rather than mechanically importing every developer clause into an independent-browser analysis.
- **[P3] [YouTube API Developer Policies](https://developers.google.com/youtube/terms/developer-policies), checked 2026-09-11:** governs access/use of YouTube API Services. For a non-API route, additionally assess the applicable [ordinary service terms](https://www.youtube.com/static?template=terms), not a presumed API exemption. The earlier ordinary-terms retrieval was locale-routed; no market-specific clearance is claimed here.
- **[P4] [Hong Kong IPD copyright overview](https://www.ipd.gov.hk/en/copyright/what-is-copyright/index.html) and [copyright FAQ](https://www.ipd.gov.hk/en/copyright/legislative-proposals-and-amendments/copyright-amendment-ordinance-2022/details-faqs/index.html):** references retained from the preceding review. The overview fetch failed in this update, so no fresh statutory verification is claimed. Private entertainment is not assumed to be a universal fair-use category; public performance is not the only operation to assess.

Keep the existing MuScriptor code/weight licensing distinction and replacement seam in `PLAN.md`; recheck the actual version before deployment. A zero-price core with paid add-ons is not automatically a non-commercial use. No platform permission or legal opinion was obtained by adding this document.

## Decision and verification record

2026-09-11: accepted for planning are the experience-first framing, separation of atmosphere from compulsory socialising, source-independent feature boundary, operation-specific media review and preservation of current milestone order. The browser/bookmark responsibility theory remains an unverified proposal, not an accepted legal conclusion. Monetisation and atmosphere effects remain hypotheses. No personal diary text or third-party encounter details belong in this repository.

This change modifies documentation only. Source inspection and cross-document reconciliation were performed; no code, adapter, gameplay asset, build, automated test, runtime validation, human playtest or compliance audit is claimed.