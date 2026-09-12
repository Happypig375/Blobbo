// Nu Game Engine.
// Required Notice:
// Copyright (C) Bryan Edds.
// Nu Game Engine is licensed under the Nu Game Engine Noncommercial License.
// See https://github.com/bryanedds/Nu/blob/master/License.md.

namespace BlobboPlayground.Tests
open System.Numerics
open NUnit.Framework
open Prime
open Nu
open BlobboPlayground
module M1ExperimentTests =

    [<Test>]
    let ``M1 deterministic verification passes`` () =
        let result = M1Verification.evaluate ()
        Assert.That (result.Passed, Is.True, String.concat "\n" result.Checks)

    [<Test>]
    let ``Simplified candidates materially reduce constraint cost`` () =
        Assert.That (M1Topology.jointCount SimplifiedRing, Is.EqualTo 24)
        Assert.That (M1Topology.constraintReduction SimplifiedRing, Is.GreaterThan 0.95f)
        Assert.That (M1Topology.jointCount StableHull, Is.Zero)

    [<Test>]
    let ``Touch samples use the common pointer abstraction`` () =
        let sample = M1PointerInput.fromTouch 7 42L (v2 12.0f -8.0f) PointerHeld
        Assert.That (sample.Tick, Is.EqualTo 7)
        Assert.That (sample.Position, Is.EqualTo (Vector2 (12.0f, -8.0f)))
        Assert.That (sample.Phase, Is.EqualTo PointerHeld)
        Assert.That (sample.Device, Is.EqualTo (TouchPointer 42L))

    [<Test>]
    let ``Magnitude clamp enforces configured limits`` () =
        let clamped = M1Control.clampMagnitude 5.0f (v2 30.0f 40.0f)
        Assert.That (clamped.Length (), (Is.EqualTo 5.0f).Within 0.0001f)

    [<Test>]
    let ``Release velocity change preserves candidate direction rules`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let pointerVelocity = v2 100.0f 0.0f
        let grab = M1Control.releaseVelocityChange configuration GrabThrow v2Zero v2Zero pointerVelocity
        let pull =
            M1Control.releaseVelocityChange
                configuration
                PullSling
                v2Zero
                (v2 -100.0f 0.0f)
                pointerVelocity
        let swipe = M1Control.releaseVelocityChange configuration SwipeSmack v2Zero v2Zero pointerVelocity
        Assert.That (grab.X, (Is.EqualTo 16.0f).Within 0.0001f)
        Assert.That (pull.X, (Is.EqualTo 280.0f).Within 0.0001f)
        Assert.That (swipe.X, (Is.EqualTo 20.0f).Within 0.0001f)
        Assert.That (grab.Y, Is.Zero)
        Assert.That (pull.Y, Is.Zero)
        Assert.That (swipe.Y, Is.Zero)

    [<Test>]
    let ``Paused control cancels a gesture without a delayed launch`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let activeSample =
            { Tick = 0
              Position = v2 0.0f 0.0f
              Phase = PointerPressed
              Device = MousePointer }
        let active = M1Control.step configuration GrabThrow v2Zero v2Zero true activeSample ControlInactive
        let pausedSample = { activeSample with Tick = 1; Phase = PointerReleased; Position = v2 40.0f 0.0f }
        let paused = M1Control.stepWhenAdvancing false configuration GrabThrow v2Zero v2Zero true pausedSample active.State
        Assert.That (paused.State, Is.EqualTo ControlInactive)
        Assert.That (paused.Released, Is.False)
        Assert.That (paused.VelocityChange, Is.EqualTo v2Zero)
        let resumed = M1Control.step configuration GrabThrow v2Zero v2Zero true pausedSample paused.State
        Assert.That (resumed.Released, Is.False)
        Assert.That (resumed.VelocityChange, Is.EqualTo v2Zero)

    [<Test>]
    let ``Press requires rendered body contact even near the center`` () =
        let sample = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        let output = M1Control.step M1ControlConfiguration.defaultConfiguration GrabThrow v2Zero v2Zero false sample ControlInactive
        Assert.That (output.Started, Is.False)
        Assert.That (output.State, Is.EqualTo ControlInactive)

    [<Test>]
    let ``Concave contour hit test matches nonzero fill and boundary`` () =
        let points = [|v2 0.0f 0.0f; v2 10.0f 0.0f; v2 10.0f 3.0f; v2 3.0f 3.0f; v2 3.0f 10.0f; v2 0.0f 10.0f|]
        Assert.That (M1Geometry.containsPoint (v2 2.0f 8.0f) points, Is.True)
        Assert.That (M1Geometry.containsPoint (v2 8.0f 8.0f) points, Is.False)
        Assert.That (M1Geometry.containsPoint (v2 3.0f 8.0f) points, Is.True)
        Assert.That (M1Geometry.containsPoint (v2 2.0f 8.0f) (Array.rev points), Is.True)
        Assert.That (M1Geometry.containsPoint (v2 8.0f 8.0f) [|v2Zero; v2Zero; v2Zero|], Is.False)

    [<Test>]
    let ``UI release cancels an active gesture`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let press = M1PointerInput.fromTouch 0 1L CombinedRoom.SubjectSpawn PointerPressed
        let active = M1Control.step configuration PullSling CombinedRoom.SubjectSpawn v2Zero true press ControlInactive
        let release = { press with Tick = 1; Position = v2 0.0f (CombinedRoom.PlayBounds.Max.Y + 1.0f); Phase = PointerReleased }
        let allowed = M1PointerInput.insidePlayfield CombinedRoom.PlayBounds release
        let output = M1Control.stepWhenAdvancing allowed configuration PullSling CombinedRoom.SubjectSpawn v2Zero true release active.State
        Assert.That (allowed, Is.False)
        Assert.That (output.State, Is.EqualTo ControlInactive)
        Assert.That (output.VelocityChange, Is.EqualTo v2Zero)

    [<Test>]
    let ``Release preview agrees with clamped release execution`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let press = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        let held = { press with Tick = 1; Position = v2 -80.0f -20.0f; Phase = PointerHeld }
        let release = { held with Tick = 2; Phase = PointerReleased }
        for mode in [GrabThrow; PullSling; SwipeSmack] do
            let start = M1Control.step configuration mode v2Zero v2Zero true press ControlInactive
            let holding = M1Control.step configuration mode v2Zero v2Zero true held start.State
            match holding.State with
            | ControlActive active ->
                let previewChange = M1Control.previewVelocityChange configuration mode release active
                let output = M1Control.step configuration mode v2Zero v2Zero true release holding.State
                let velocity = v2 590.0f 150.0f
                Assert.That (output.VelocityChange, Is.EqualTo previewChange)
                Assert.That (M1Control.releaseVelocity configuration output.VelocityChange velocity,
                    Is.EqualTo (M1Control.releaseVelocity configuration previewChange velocity))
                Assert.That ((M1Control.releaseVelocity configuration previewChange velocity).Length (), Is.LessThanOrEqualTo (configuration.MaximumSpeed + 0.001f))
            | ControlInactive -> Assert.Fail "A valid body press should start the gesture."

    [<Test>]
    let ``Recent motion survives a few stationary pointer samples`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let press = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        let mutable output = M1Control.step configuration SwipeSmack v2Zero v2Zero true press ControlInactive
        for tick in 1 .. 8 do
            let sample = { press with Tick = tick; Position = v2 (single (min 6 tick) * 10.0f) 0.0f; Phase = PointerHeld }
            output <- M1Control.step configuration SwipeSmack v2Zero v2Zero true sample output.State
        let release = { press with Tick = 9; Position = v2 60.0f 0.0f; Phase = PointerReleased }
        let output = M1Control.step configuration SwipeSmack v2Zero v2Zero true release output.State
        Assert.That (output.VelocityChange.X, (Is.EqualTo 45.0f).Within 0.01f)

    [<Test>]
    let ``Stopped pointer motion expires before release even without intervening samples`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let press = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        for mode in [GrabThrow; SwipeSmack] do
            let start = M1Control.step configuration mode v2Zero v2Zero true press ControlInactive
            let held = { press with Tick = 1; Position = v2 30.0f 0.0f; Phase = PointerHeld }
            let holding = M1Control.step configuration mode v2Zero v2Zero true held start.State
            let release = { held with Tick = 7; Phase = PointerReleased }
            let output = M1Control.step configuration mode v2Zero v2Zero true release holding.State
            Assert.That (output.VelocityChange, Is.EqualTo v2Zero)

    [<Test>]
    let ``Pointer velocity window gives the same release at different sample rates`` () =
        for rate in [30; 60; 120] do
            let configuration = { M1ControlConfiguration.defaultConfiguration with FixedDeltaSeconds = 1.0f / single rate }
            let press = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
            let mutable output = M1Control.step configuration SwipeSmack v2Zero v2Zero true press ControlInactive
            for tick in 1 .. rate / 5 - 1 do
                let sample = { press with Tick = tick; Position = v2 (800.0f * single tick / single rate) 0.0f; Phase = PointerHeld }
                output <- M1Control.step configuration SwipeSmack v2Zero v2Zero true sample output.State
            let release = { press with Tick = rate / 5; Position = v2 160.0f 0.0f; Phase = PointerReleased }
            let output = M1Control.step configuration SwipeSmack v2Zero v2Zero true release output.State
            Assert.That (output.VelocityChange.X, (Is.EqualTo 160.0f).Within 0.001f)

    [<Test>]
    let ``Pointer motion history stays bounded during a long gesture`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let press = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        let mutable output = M1Control.step configuration SwipeSmack v2Zero v2Zero true press ControlInactive
        for tick in 1 .. 1000 do
            let sample = { press with Tick = tick; Position = v2 (single tick) 0.0f; Phase = PointerHeld }
            output <- M1Control.step configuration SwipeSmack v2Zero v2Zero true sample output.State
        match output.State with
        | ControlActive active -> Assert.That (active.PointerSamplesRev.Length, Is.LessThanOrEqualTo 7)
        | ControlInactive -> Assert.Fail "The held gesture should remain active."

    [<Test>]
    let ``Grab acceleration can lift the whole body against gravity and stays bounded`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let press = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        let active = M1Control.step configuration GrabThrow v2Zero v2Zero true press ControlInactive
        let held = { press with Tick = 1; Position = v2 0.0f 100.0f; Phase = PointerHeld }
        let output = M1Control.step configuration GrabThrow v2Zero v2Zero true held active.State
        Assert.That (output.Acceleration.Y, Is.GreaterThan (abs Constants.Physics.GravityDefault.Y * Constants.Engine.Meter2d))
        Assert.That (output.Acceleration.Length (), Is.LessThanOrEqualTo configuration.MaximumAcceleration)

    [<Test>]
    let ``Comparison masses follow sphere area and include every constituent`` () =
        let center = M1BodyModel.bodyMass LegacyGraph M1BodyModel.CenterBodyIndex
        let node = M1BodyModel.bodyMass LegacyGraph 0
        Assert.That (center / node, (Is.EqualTo 2.25f).Within 0.0001f)
        Assert.That (M1BodyModel.bodyMass StableHull -1, Is.GreaterThan center)
        for candidate in [LegacyGraph; SimplifiedRing; StableHull] do
            let indices = if candidate = StableHull then [-1] else [-1 .. M1Topology.bodyCount candidate - 2]
            let mass = indices |> List.sumBy (M1BodyModel.bodyMass candidate)
            Assert.That (indices.Length, Is.EqualTo (M1Topology.bodyCount candidate))
            Assert.That (mass, Is.GreaterThan 0.0f)

    [<Test>]
    let ``Reset clears gestures previews and combined diagnostics`` () =
        let sample = M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
        let active = M1Control.step M1ControlConfiguration.defaultConfiguration GrabThrow v2Zero v2Zero true sample ControlInactive
        let state =
            { M1SceneState.initial with
                ControlState = active.State
                CaptureRev = [sample]
                InputReplay = PendingInputReplay
                Trail = [|v2One|]
                CombinedState = { CombinedRoomState.initial with WaterBursts = 7; RewindPreviewOpt = Some { PreviewUpdates = 120L; ResumeAdvancing = true } } }
        let reset = M1SceneState.reset state
        Assert.That (reset.ControlState, Is.EqualTo ControlInactive)
        Assert.That (reset.InputReplay, Is.EqualTo NoInputReplay)
        Assert.That (reset.CaptureRev, Is.Empty)
        Assert.That (reset.Trail, Is.Empty)
        Assert.That (reset.CombinedState, Is.EqualTo CombinedRoomState.initial)
        Assert.That (reset.FixtureVersion, Is.EqualTo (state.FixtureVersion + 1))

    [<Test>]
    let ``Room selection locks only Combined to Legacy and resets each menu entry`` () =
        let combined = M1SceneState.rebuild StableHull SwipeSmack CombinedInteractionsRoom M1SceneState.initial
        let toy = M1SceneState.rebuild StableHull SwipeSmack EmptyToyRoom combined
        Assert.That (combined.Candidate, Is.EqualTo LegacyGraph)
        Assert.That (combined.ControlMode, Is.EqualTo SwipeSmack)
        Assert.That (toy.Candidate, Is.EqualTo StableHull)
        Assert.That ((M1SceneState.enter false combined).Room, Is.EqualTo EmptyToyRoom)
        Assert.That ((M1SceneState.enter true toy).Room, Is.EqualTo CombinedInteractionsRoom)
        Assert.That ((M1SceneState.enter true toy).Candidate, Is.EqualTo LegacyGraph)

    [<Test>]
    let ``Long gestures stop trace capture at the declared bound`` () =
        let sample = M1PointerInput.fromTouch 0 1L v2Zero PointerHeld
        let almostFull = List.replicate (M1Trace.MaximumSamples - 1) sample
        let full = M1Trace.tryRecord sample almostFull |> Option.get
        Assert.That (full.Length, Is.EqualTo M1Trace.MaximumSamples)
        Assert.That ((M1Trace.tryRecord sample full).IsNone, Is.True)

    [<Test>]
    let ``Input repeat translates first contact while preserving gesture timing and control deltas`` () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let originalCenter = v2 180.0f -40.0f
        let freshCenter = v2 -175.0f -15.0f
        let samples =
            [|M1PointerInput.fromTouch 0 7L (v2 200.0f -30.0f) PointerPressed
              M1PointerInput.fromTouch 1 7L (v2 230.0f -30.0f) PointerHeld
              M1PointerInput.fromTouch 2 7L (v2 260.0f -30.0f) PointerReleased|]
        let original = Array.copy samples
        let bounds = box2 (v2 -310.0f -82.0f) (v2 620.0f 150.0f)
        match M1Trace.prepareReplay bounds freshCenter samples with
        | PlayingInputReplay (translated, index) ->
            Assert.That (index, Is.Zero)
            Assert.That (translated[0].Position, Is.EqualTo freshCenter)
            for index in 0 .. samples.Length - 1 do
                Assert.That (translated[index].Tick, Is.EqualTo samples[index].Tick)
                Assert.That (translated[index].Phase, Is.EqualTo samples[index].Phase)
                Assert.That (translated[index].Device, Is.EqualTo ReplayPointer)
                Assert.That (translated[index].Position - translated[0].Position,
                    Is.EqualTo (samples[index].Position - samples[0].Position))
            for mode in [GrabThrow; PullSling; SwipeSmack] do
                let mutable recordedState = ControlInactive
                let mutable repeatedState = ControlInactive
                for index in 0 .. samples.Length - 1 do
                    let recorded = M1Control.step configuration mode originalCenter v2Zero true samples[index] recordedState
                    let repeated = M1Control.step configuration mode freshCenter v2Zero true translated[index] repeatedState
                    Assert.That (repeated.Acceleration, Is.EqualTo recorded.Acceleration)
                    Assert.That (repeated.VelocityChange, Is.EqualTo recorded.VelocityChange)
                    Assert.That (repeated.Started, Is.EqualTo recorded.Started)
                    Assert.That (repeated.Released, Is.EqualTo recorded.Released)
                    recordedState <- recorded.State
                    repeatedState <- repeated.State
        | _ -> Assert.Fail "The translated gesture should fit the reset playfield."
        Assert.That (samples, Is.EqualTo original)

    [<Test>]
    let ``Input repeat reports translated paths outside the reset playfield`` () =
        let bounds = box2 (v2 -310.0f -82.0f) (v2 620.0f 150.0f)
        let samples =
            [|M1PointerInput.fromTouch 0 1L (v2 200.0f 0.0f) PointerPressed
              M1PointerInput.fromTouch 1 1L v2Zero PointerReleased|]
        Assert.That (samples |> Array.forall (M1PointerInput.insidePlayfield bounds), Is.True)
        let replay = M1Trace.prepareReplay bounds (v2 -175.0f -15.0f) samples
        Assert.That (replay, Is.EqualTo (CancelledInputReplay ReplayOutsidePlayfield))
        let state = M1SceneState.cancelGesture { M1SceneState.initial with InputReplay = replay }
        Assert.That (state.InputReplay, Is.EqualTo replay)
        Assert.That (state.ControlState, Is.EqualTo ControlInactive)

    [<Test>]
    let ``Input repeat advances fixed translated samples and reports missed first contact`` () =
        let samples =
            [|M1PointerInput.fromTouch 0 1L v2Zero PointerPressed
              M1PointerInput.fromTouch 1 1L (v2 10.0f 0.0f) PointerReleased|]
        let replay = M1Trace.prepareReplay (box2 (v2Dup -50.0f) (v2Dup 100.0f)) v2Zero samples
        Assert.That (M1Trace.advanceReplay false replay, Is.EqualTo (CancelledInputReplay ReplayMissingContact))
        let advanced = M1Trace.advanceReplay true replay
        match replay, advanced with
        | PlayingInputReplay (first, 0), PlayingInputReplay (second, 1) -> Assert.That (second, Is.SameAs first)
        | _ -> Assert.Fail "Advancing should retain the once-translated trace."
        Assert.That (M1Trace.advanceReplay false advanced, Is.EqualTo FinishedInputReplay)

    [<Test>]
    let ``Repeat input resets the fixture but keeps the selected body mode and recorded trace`` () =
        let sample = M1PointerInput.fromTouch 0 1L (v2 200.0f 0.0f) PointerPressed
        let state =
            { M1SceneState.initial with
                Candidate = StableHull
                ControlMode = PullSling
                Room = GenerousTargetRoom
                LastTrace = [|sample|]
                CaptureRev = [sample]
                CombinedState = { CombinedRoomState.initial with WaterBursts = 3 } }
        let repeat = M1SceneState.repeatInput state
        Assert.That (repeat.Candidate, Is.EqualTo state.Candidate)
        Assert.That (repeat.ControlMode, Is.EqualTo state.ControlMode)
        Assert.That (repeat.Room, Is.EqualTo state.Room)
        Assert.That (repeat.LastTrace, Is.SameAs state.LastTrace)
        Assert.That (repeat.InputReplay, Is.EqualTo PendingInputReplay)
        Assert.That (repeat.CaptureRev, Is.Empty)
        Assert.That (repeat.CombinedState, Is.EqualTo CombinedRoomState.initial)
        Assert.That (repeat.FixtureVersion, Is.EqualTo (state.FixtureVersion + 1))
        Assert.That ((M1SceneState.cancelGesture repeat).InputReplay, Is.EqualTo NoInputReplay)

    [<Test>]
    let ``Water bursts are deterministic and bounded by requested count`` () =
        let first = WaterParticles.burst "Water" (v2 10.0f 20.0f) (v2 0.0f -18.0f) 4.0f 64 |> Seq.toArray
        let second = WaterParticles.burst "Water" (v2 10.0f 20.0f) (v2 0.0f -18.0f) 4.0f 64 |> Seq.toArray
        Assert.That (first, Is.EqualTo second)
        Assert.That (first.Length, Is.EqualTo 64)
        Assert.That (first |> Array.forall (fun particle -> Vector2.Distance (particle.FluidParticlePosition.V2, v2 10.0f 20.0f) <= 20.0f), Is.True)
        for count in [1; 12; 31; 32] do
            let burst = WaterParticles.burst "Smoke" (v2 10.0f 20.0f) v2Zero 2.0f count |> Seq.toArray
            let mean = burst |> Array.fold (fun sum particle -> sum + particle.FluidParticlePosition.V2) v2Zero |> fun sum -> sum / single count
            Assert.That (Vector2.Distance (mean, v2 10.0f 20.0f), Is.LessThan 0.0001f)

    [<Test>]
    let ``Publishing a moved physical perimeter updates the heat source and overlap`` () =
        let spawn = v3 -350.0f -76.0f 0.0f
        let physicalBounds = box2 (CombinedRoom.HeaterBounds.Center - v2Dup 20.0f) (v2Dup 40.0f)
        let mutable transform = Transform.makeDefault ()
        transform.Position <- spawn
        transform.Size <- v3Dup 64.0f
        transform.Perimeter <- physicalBounds.Box3
        Assert.That (transform.Position.V2, Is.EqualTo physicalBounds.Center)
        Assert.That (transform.PerimeterCenter.V2, Is.EqualTo physicalBounds.Center)
        Assert.That (CombinedRoom.overlaps CombinedRoom.HeaterBounds transform.Perimeter.Box2, Is.True)
        Assert.That (CombinedRoom.overlaps CombinedRoom.HeaterBounds (box2 (spawn.V2 - v2Dup 32.0f) (v2Dup 64.0f)), Is.False)

    [<Test>]
    let ``Heat smoke clears the current physical envelope with deterministic count and momentum`` () =
        let perimeter = box2 (v2 -145.0f -94.0f) (v2 82.0f 68.0f)
        for count in [0; 1; 12; 32] do
            let first = WaterParticles.smokeAbovePerimeter perimeter count |> Seq.toArray
            let second = WaterParticles.smokeAbovePerimeter perimeter count |> Seq.toArray
            Assert.That (first, Is.EqualTo second)
            Assert.That (first.Length, Is.EqualTo count)
            for particle in first do
                Assert.That (particle.FluidParticleConfig, Is.EqualTo "Smoke")
                Assert.That (particle.FluidParticleVelocity, Is.EqualTo (v3 0.0f 36.0f 0.0f))
                Assert.That (particle.FluidParticlePosition.Y - FluidParticleConfig.smokeConfig.Radius, Is.GreaterThan perimeter.Max.Y)
            if count > 0 then
                let meanX = first |> Array.averageBy (fun particle -> particle.FluidParticlePosition.X)
                Assert.That (meanX, (Is.EqualTo perimeter.Center.X).Within 0.0001f)

    [<Test>]
    let ``Blobbo absorbs water only inside capacity and contact range`` () =
        let water = { FluidParticlePosition = v3Zero; FluidParticleVelocity = v3Zero; FluidParticleConfig = "Water" }
        Assert.That (BlobboBodyModel.canAbsorb 0 v2Zero water, Is.True)
        Assert.That (BlobboBodyModel.canAbsorb 0 v2Zero { water with FluidParticleConfig = "Smoke" }, Is.False)
        Assert.That (BlobboBodyModel.canAbsorb 32 v2Zero water, Is.False)
        Assert.That (BlobboBodyModel.canAbsorb 0 (v2 100.0f 0.0f) water, Is.False)

    [<Test>]
    let ``Heating reverses absorbed body expansion around the physical center`` () =
        let center = v2 200.0f -30.0f
        let point =
            { BodyCenter = center + v2 32.0f 0.0f
              BodyRotation = Quaternion.Identity
              BodyLinearVelocity = v2 80.0f 20.0f
              BodyAngularVelocity = v2Zero }
        let expanded = BlobboBodyModel.resizeForWater 0 32 center [|point|]
        Assert.That (Vector2.Distance (expanded[0].BodyCenter, center), (Is.EqualTo 48.0f).Within 0.0001f)
        let heated = BlobboBodyModel.resizeForWater 32 0 center expanded
        Assert.That (heated[0].BodyCenter, Is.EqualTo point.BodyCenter)
        Assert.That (heated[0].BodyLinearVelocity, Is.EqualTo point.BodyLinearVelocity)

    [<Test>]
    let ``Heat and cooling use positive world bounds and preserve unrelated phases`` () =
        Assert.That (CombinedRoom.HeaterBounds.Size.X, Is.GreaterThan 0.0f)
        Assert.That (CombinedRoom.HeaterBounds.Size.Y, Is.GreaterThan 0.0f)
        let water = { FluidParticlePosition = CombinedRoom.HeaterBounds.Center.V3; FluidParticleVelocity = v3 12.0f -8.0f 0.0f; FluidParticleConfig = "Water" }
        let smoke = CombinedRoom.convertParticle true water
        Assert.That (smoke.FluidParticleConfig, Is.EqualTo "Smoke")
        Assert.That (smoke.FluidParticleVelocity, Is.EqualTo water.FluidParticleVelocity)
        let smokeGravity = Gravity.localize (Constants.Physics.GravityDefault * Constants.Engine.Meter2d) FluidParticleConfig.smokeConfig.Gravity
        Assert.That (smokeGravity.Y, Is.GreaterThan 0.0f)
        Assert.That (CombinedRoom.convertParticle false water, Is.EqualTo water)
        let cooled = CombinedRoom.convertParticle false { smoke with FluidParticlePosition = CombinedRoom.CoolerBounds.Center.V3 }
        Assert.That (cooled.FluidParticleConfig, Is.EqualTo "Water")
        Assert.That (cooled.FluidParticleVelocity, Is.EqualTo smoke.FluidParticleVelocity)
        let outside = { water with FluidParticlePosition = v3 300.0f 0.0f 0.0f }
        Assert.That (CombinedRoom.convertParticle true outside, Is.EqualTo outside)

    [<Test>]
    let ``Combined geometry fits the canonical playfield and heater starts at plate surface`` () =
        Assert.That (CombinedRoom.PlayBounds.Min.X, Is.GreaterThan -320.0f)
        Assert.That (CombinedRoom.PlayBounds.Max.X, Is.LessThan 320.0f)
        Assert.That (CombinedRoom.PlayBounds.Min.Y, Is.GreaterThan -107.0f)
        Assert.That (CombinedRoom.PlayBounds.Max.Y, Is.LessThan 82.0f)
        for bounds in [CombinedRoom.HeaterPlateBounds; CombinedRoom.HeaterBounds; CombinedRoom.CoolerBounds] do
            Assert.That (CombinedRoom.PlayBounds.Contains bounds.Min, Is.Not.EqualTo ContainmentType.Disjoint)
            Assert.That (CombinedRoom.PlayBounds.Contains bounds.Max, Is.Not.EqualTo ContainmentType.Disjoint)
        Assert.That (CombinedRoom.HeaterBounds.Min.Y, Is.EqualTo CombinedRoom.HeaterPlateBounds.Max.Y)

    [<Test>]
    let ``Prop rewind scrub and zero-time history are bounded`` () =
        Assert.That (CombinedRoom.clampRewind 100L -6L, Is.Zero)
        Assert.That (CombinedRoom.clampRewind 100L 1000L, Is.EqualTo 100L)
        Assert.That (CombinedRoom.clampRewind 1000L 1000L, Is.EqualTo CombinedRoom.RewindMaximumUpdates)
        let record = { PropertyName = "Position"; PreviousValue = valueToSymbol v3Zero; TimePassed = GameTime.zero }
        let trimmed = List.replicate 2500 record |> CombinedRoom.trimHistory
        Assert.That (trimmed.Length, Is.EqualTo CombinedRoom.RewindMaximumRecords)
        let timed = List.replicate 1000 { record with TimePassed = GameTime.ofUpdates 1L } |> CombinedRoom.trimHistory
        Assert.That (timed.Length, Is.EqualTo (int CombinedRoom.RewindMaximumUpdates))