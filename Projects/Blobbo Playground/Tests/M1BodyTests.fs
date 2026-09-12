namespace BlobboPlayground.Tests
open System.Numerics
open NUnit.Framework
open Prime
open Nu
open BlobboPlayground

module M1BodyTests =

    [<Test; NonParallelizable; Category "Integration">]
    let ``Ring and hull retain control motion through their real dispatcher process`` () =
        Nu.init ()
        let worldConfig = { WorldConfig.defaultConfig with Accompanied = true }
        let windowSize = Globals.Render.DisplayVirtualResolution * Globals.Render.DisplayScalar
        match SdlDeps.tryMake worldConfig.SdlConfig false windowSize with
        | Right sdlDeps ->
            use _ = sdlDeps
            let windowViewport = Viewport.makeWindow1 windowSize
            let geometryViewport = Viewport.makeGeometry windowViewport.Bounds.Size
            let world = World.make (constant None) sdlDeps worldConfig windowSize geometryViewport windowViewport (RewindLifecyclePlugin ())
            try
                World.updateLateBindings false [|typeof<M1BlobboDispatcher>.Assembly|] world
                Assert.That (Map.containsKey (nameof M1BlobboDispatcher) (World.getEntityDispatchers world), Is.True)
                let screen = World.createScreen<ScreenDispatcher> (Some "M1 body motion") world
                let group = World.createGroup<GroupDispatcher> (Some "Subjects") screen world
                let subjects =
                    [|for (candidate, spawn) in [(SimplifiedRing, v2 -120.0f 0.0f); (StableHull, v2 120.0f 0.0f)] do
                          let subject = World.createEntity<M1BlobboDispatcher> None DefaultOverlay (Some [|string candidate|]) group world
                          subject.SetM1BodyCandidate candidate world
                          subject.SetPosition spawn.V3 world
                          yield (candidate, subject, spawn)|]
                World.selectScreen (IdlingState GameTime.zero) screen world
                let configuration = M1ControlConfiguration.defaultConfiguration
                let release =
                    { State = ControlInactive
                      Acceleration = v2Zero
                      VelocityChange = v2 120.0f 0.0f
                      Started = false
                      Released = true }
                for (candidate, subject, _) in subjects do
                    for bodyId in M1BodyModel.bodyIds candidate subject do
                        Assert.That (World.getBodyExists bodyId world, Is.True)
                    M1BodyControl.apply configuration candidate subject release world
                    for bodyId in M1BodyModel.bodyIds candidate subject do
                        Assert.That ((World.getBodyLinearVelocity bodyId world).X, (Is.EqualTo 120.0f).Within 0.005f)

                let mutable frame = 0
                let beforeFrame world =
                    if frame >= 12 && frame < 24 then
                        let held = { release with Acceleration = v2 400.0f 0.0f; VelocityChange = v2Zero; Released = false }
                        for (candidate, subject, _) in subjects do
                            M1BodyControl.apply configuration candidate subject held world
                let afterFrame world =
                    frame <- frame + 1
                    if frame = 12 || frame = 30 then
                        for (candidate, subject, spawn) in subjects do
                            let center = subject.GetM1BodyCenter world
                            let requiredDistance = if frame = 12 then 10.0f else 45.0f
                            let requiredSpeed = if frame = 12 then 90.0f else 150.0f
                            Assert.That (center.BodyCenter.X - spawn.X, Is.GreaterThan requiredDistance, string candidate)
                            for bodyId in M1BodyModel.bodyIds candidate subject do
                                Assert.That ((World.getBodyLinearVelocity bodyId world).X, Is.GreaterThan requiredSpeed, string candidate)
                            let points = M1BlobboVisual.bodyPoints candidate subject world
                            let renderedCenter = Array.fold (+) v2Zero points / single points.Length
                            Assert.That (renderedCenter.X - spawn.X, Is.GreaterThan requiredDistance, string candidate)
                            Assert.That (subject.GetM1AppliedPosition world, Is.EqualTo (subject.GetPosition world))
                    if frame = 30 then
                        for (_, subject, spawn) in subjects do
                            subject.SetPosition (spawn + v2 0.0f 20.0f).V3 world
                    elif frame = 33 then
                        for (candidate, subject, spawn) in subjects do
                            Assert.That ((subject.GetM1BodyCenter world).BodyCenter.X, (Is.EqualTo spawn.X).Within 0.1f)
                            for bodyId in M1BodyModel.bodyIds candidate subject do
                                Assert.That ((World.getBodyLinearVelocity bodyId world).X, (Is.EqualTo 0.0f).Within 0.01f)
                            World.destroyEntityImmediate subject world
                            for bodyId in M1BodyModel.bodyIds candidate subject do
                                Assert.That (World.getBodyExists bodyId world, Is.False)
                            for index in 0 .. M1Topology.jointCount candidate - 1 do
                                Assert.That (World.getBodyJointExists { BodyJointSource = subject; BodyJointIndex = index } world, Is.False)
                World.runWithoutCleanUp (fun _ -> frame < 33) beforeFrame ignore afterFrame ignore ignore (Some ignore) world
                Assert.That (frame, Is.EqualTo 33)
            finally World.cleanUp world
        | Left error -> Assert.Fail (string error)

    [<Test; NonParallelizable; Category "Integration">]
    let ``Scene input repeat activates a far-origin gesture and reports an out-of-room translation`` () =
        Nu.init ()
        let worldConfig = { WorldConfig.defaultConfig with Accompanied = true }
        let windowSize = Globals.Render.DisplayVirtualResolution * Globals.Render.DisplayScalar
        match SdlDeps.tryMake worldConfig.SdlConfig false windowSize with
        | Right sdlDeps ->
            use _ = sdlDeps
            let windowViewport = Viewport.makeWindow1 windowSize
            let geometryViewport = Viewport.makeGeometry windowViewport.Bounds.Size
            let world = World.make (constant None) sdlDeps worldConfig windowSize geometryViewport windowViewport (RewindLifecyclePlugin ())
            try
                World.updateLateBindings false [|typeof<Scene06_M1ControlStudyDispatcher>.Assembly|] world
                let screen = World.createScreen<Scene06_M1ControlStudyDispatcher> (Some "M1 input repeat") world
                let samples =
                    [|M1PointerInput.fromTouch 0 1L (v2 200.0f -30.0f) PointerPressed
                      M1PointerInput.fromTouch 1 1L (v2 230.0f -30.0f) PointerHeld
                      M1PointerInput.fromTouch 2 1L (v2 260.0f -30.0f) PointerReleased|]
                let replayState =
                    M1SceneState.repeatInput
                        { M1SceneState.initial with
                            Candidate = StableHull
                            ControlMode = SwipeSmack
                            Room = EmptyToyRoom
                            LastTrace = samples }
                screen.SetM1SceneState replayState world
                World.selectScreen (IdlingState GameTime.zero) screen world
                let subject = screen / (sprintf "M1 Fixture %d" replayState.FixtureVersion) / "Subject"
                let mutable frame = 0
                let mutable firstContact = v2Zero
                let afterFrame world =
                    frame <- frame + 1
                    let state = screen.GetM1SceneState world
                    match frame with
                    | 1 ->
                        Assert.That (subject.GetExists world, Is.True)
                        match state.ControlState, state.InputReplay with
                        | ControlActive active, PlayingInputReplay (translated, 1) ->
                            firstContact <- active.PressPosition
                            Assert.That (firstContact, Is.EqualTo ((subject.GetM1BodyCenter world).BodyCenter))
                            Assert.That (Vector2.Distance (firstContact, samples[0].Position), Is.GreaterThan 300.0f)
                            Assert.That (translated[0].Position, Is.EqualTo firstContact)
                            Assert.That (translated[1].Position, Is.EqualTo (firstContact + v2 30.0f 0.0f))
                        | _ -> Assert.Fail "The translated first press must activate on the fresh body."
                    | 2 ->
                        match state.ControlState, state.InputReplay with
                        | ControlActive active, PlayingInputReplay (translated, 2) ->
                            Assert.That (active.PressPosition, Is.EqualTo firstContact)
                            Assert.That (translated[0].Position, Is.EqualTo firstContact)
                        | _ -> Assert.Fail "Replay must advance without moving its anchor with the body."
                    | 12 ->
                        Assert.That (state.InputReplay, Is.EqualTo FinishedInputReplay)
                        Assert.That (state.Attempts, Is.EqualTo 1)
                        Assert.That (state.LastTrace, Is.SameAs samples)
                        Assert.That (state.LastVelocityChange.X, Is.GreaterThan 300.0f)
                        Assert.That ((subject.GetM1BodyCenter world).BodyCenter.X - firstContact.X, Is.GreaterThan 30.0f)
                        let outside =
                            [|samples[0]
                              { samples[2] with Position = v2 0.0f -30.0f }|]
                        screen.SetM1SceneState (M1SceneState.repeatInput { state with LastTrace = outside }) world
                    | 13 | 14 ->
                        Assert.That (state.InputReplay, Is.EqualTo (CancelledInputReplay ReplayOutsidePlayfield))
                        Assert.That (state.ControlState, Is.EqualTo ControlInactive)
                        Assert.That (state.LastVelocityChange, Is.EqualTo v2Zero)
                        Assert.That (state.Attempts, Is.EqualTo 1)
                    | _ -> ()
                World.runWithoutCleanUp (fun _ -> frame < 14) ignore ignore afterFrame ignore ignore (Some ignore) world
                Assert.That (frame, Is.EqualTo 14)
            finally World.cleanUp world
        | Left error -> Assert.Fail (string error)