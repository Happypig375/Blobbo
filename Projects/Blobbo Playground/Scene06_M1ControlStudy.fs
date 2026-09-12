namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu

type M1LandingOutcome =
    | AwaitingAttempt
    | FreePlayComplete
    | TargetInFlight
    | TargetHit
    | TargetMiss

type M1SceneState =
    { Candidate : M1BodyCandidate
      ControlMode : M1ControlMode
      Room : M1Room
      ControlState : M1ControlState
      CaptureRev : M1PointerSample list
      CaptureOverflow : bool
      LastTrace : M1PointerSample array
      InputReplay : M1InputReplay
      Attempts : int
      Hits : int
      Misses : int
      Outcome : M1LandingOutcome
      FlightSeconds : single
      StableSeconds : single
      FixtureVersion : int
      ResetCount : int
      ReplayCount : int
      Trail : Vector2 array
      LastAcceleration : Vector2
      LastVelocityChange : Vector2
      CombinedState : CombinedRoomState }

[<RequireQualifiedAccess>]
module M1SceneState =

    let initial =
        { Candidate = LegacyGraph
          ControlMode = GrabThrow
          Room = CombinedInteractionsRoom
          ControlState = ControlInactive
          CaptureRev = []
          CaptureOverflow = false
          LastTrace = Array.empty
          InputReplay = NoInputReplay
          Attempts = 0
          Hits = 0
          Misses = 0
          Outcome = AwaitingAttempt
          FlightSeconds = 0.0f
          StableSeconds = 0.0f
          FixtureVersion = 0
          ResetCount = 0
          ReplayCount = 0
          Trail = Array.empty
          LastAcceleration = v2Zero
          LastVelocityChange = v2Zero
          CombinedState = CombinedRoomState.initial }

    let cancelGesture state =
        { state with
            ControlState = ControlInactive
            CaptureRev = []
            CaptureOverflow = false
            InputReplay =
                match state.InputReplay with
                | FinishedInputReplay | CancelledInputReplay _ -> state.InputReplay
                | NoInputReplay | PendingInputReplay | PlayingInputReplay _ -> NoInputReplay
            Trail = Array.empty
            LastAcceleration = v2Zero
            LastVelocityChange = v2Zero }

    let rebuild candidate controlMode room state =
        { initial with
            Candidate = if room = CombinedInteractionsRoom then LegacyGraph else candidate
            ControlMode = controlMode
            Room = room
            LastTrace = if room = state.Room then state.LastTrace else Array.empty
            FixtureVersion = state.FixtureVersion + 1
            ResetCount = state.ResetCount
            ReplayCount = state.ReplayCount }

    let enter combined state =
        { initial with
            Candidate = if combined then LegacyGraph else SimplifiedRing
            Room = if combined then CombinedInteractionsRoom else EmptyToyRoom
            FixtureVersion = state.FixtureVersion + 1 }

    let reset state =
        { cancelGesture state with
            InputReplay = NoInputReplay
            Outcome = AwaitingAttempt
            FlightSeconds = 0.0f
            StableSeconds = 0.0f
            FixtureVersion = state.FixtureVersion + 1
            ResetCount = state.ResetCount + 1
            CombinedState = CombinedRoomState.initial }

    let repeatInput state =
        { reset state with InputReplay = PendingInputReplay; ReplayCount = state.ReplayCount + 1 }

[<RequireQualifiedAccess>]
module M1PointerInput =

    let fromMouse tick position world =
        let phase =
            if World.isMouseButtonPressed MouseLeft world then PointerPressed
            elif World.isMouseButtonReleased MouseLeft world then PointerReleased
            elif World.isMouseButtonDown MouseLeft world then PointerHeld
            else PointerIdle
        { Tick = tick; Position = position; Phase = phase; Device = MousePointer }

    /// Touch platforms feed this adapter without changing control or replay semantics.
    let fromTouch tick pointerId position phase =
        { Tick = tick; Position = position; Phase = phase; Device = TouchPointer pointerId }

    let insidePlayfield (bounds : Box2) (sample : M1PointerSample) =
        bounds.Contains sample.Position <> ContainmentType.Disjoint

module [<AutoOpen>] M1SceneExtensions =
    type Screen with
        member this.GetM1SceneState world : M1SceneState = this.Get (nameof this.M1SceneState) world
        member this.SetM1SceneState (value : M1SceneState) world = this.Set (nameof this.M1SceneState) value world
        member this.M1SceneState = lens (nameof this.M1SceneState) this this.GetM1SceneState this.SetM1SceneState

type Scene06_M1ControlStudyDispatcher () =
    inherit ScreenDispatcherImSim ()

    static let toySpawn = v2 -175.0f -15.0f
    static let toyBounds = box2 (v2 -310.0f -82.0f) (v2 620.0f 150.0f)
    static let targetBounds = box2 (v2 105.0f -67.0f) (v2 170.0f 120.0f)

    static member Properties =
        [define Screen.GameplayState Quit
         define Screen.M1SceneState M1SceneState.initial]

    static member private CenterSnapshot candidate (subject : Entity) world =
        match candidate with
        | LegacyGraph -> subject.GetBlobboCenter world
        | SimplifiedRing | StableHull -> subject.GetM1BodyCenter world

    static member private OutcomeText = function
        | AwaitingAttempt -> "READY"
        | FreePlayComplete -> "FREE PLAY"
        | TargetInFlight -> "IN FLIGHT"
        | TargetHit -> "TARGET HIT"
        | TargetMiss -> "MISS - TRY AGAIN"

    static member private GestureHint = function
        | GrabThrow -> "Touch Blobbo, drag, then flick and release. The tether shows your grip."
        | PullSling -> "Touch Blobbo, pull away from your destination, release along the dots."
        | SwipeSmack -> "Touch Blobbo and flick across it. Release in the orange arrow's direction."

    static member private Controls state world =
        let mutable state = state
        World.beginGroup "M1 Controls" [] world
        PlaygroundVisual.panel "Header" (v2 0.0f 136.0f) (v2 640.0f 88.0f) world
        let selected selectedValue value label = if selectedValue = value then "[" + label + "]" else label
        let rebuild candidate mode room =
            World.setTimeAdvancing true world
            state <- M1SceneState.rebuild candidate mode room state
        if state.Room = CombinedInteractionsRoom then
            PlaygroundVisual.text "Locked body" true (v2 -172.0f 138.0f) (v2 270.0f 22.0f) 9.0f Color.Cyan
                "LEGACY WATER BODY (Ring / Hull in Toy)" world
        else
            for (name, candidate, x) in [("Legacy", LegacyGraph, -264.0f); ("Ring", SimplifiedRing, -173.0f); ("Hull", StableHull, -82.0f)] do
                if PlaygroundVisual.button name (selected state.Candidate candidate name) (v2 x 138.0f) 86.0f world then
                    rebuild candidate state.ControlMode state.Room
        for (name, mode, x) in [("Grab", GrabThrow, 16.0f); ("Pull", PullSling, 99.0f); ("Swipe", SwipeSmack, 182.0f)] do
            if PlaygroundVisual.button name (selected state.ControlMode mode name) (v2 x 138.0f) 78.0f world then
                rebuild state.Candidate mode state.Room
        for (name, room, x, width) in
            [("Toy", EmptyToyRoom, -264.0f, 86.0f)
             ("Target", GenerousTargetRoom, -173.0f, 86.0f)
             ("Combined", CombinedInteractionsRoom, -66.0f, 112.0f)] do
            if PlaygroundVisual.button name (selected state.Room room name) (v2 x 112.0f) width world then
                rebuild state.Candidate state.ControlMode room
        if PlaygroundVisual.button "Replay" (if state.LastTrace.Length = 0 then "Repeat --" else "Repeat input") (v2 59.0f 112.0f) 112.0f world &&
           state.LastTrace.Length > 0 then
            World.setTimeAdvancing true world
            state <- M1SceneState.repeatInput state
        if PlaygroundVisual.button "Reset" "Reset (R)" (v2 175.0f 112.0f) 106.0f world ||
           World.isKeyboardKeyPressed KeyboardKey.R world then
            World.setTimeAdvancing true world
            state <- M1SceneState.reset state
        if PlaygroundVisual.button "Pause" (if world.TimeAdvancing then "Pause (Space)" else "Resume (Space)") (v2 273.0f 112.0f) 90.0f world ||
           World.isKeyboardKeyPressed KeyboardKey.Space world then
            World.setTimeAdvancing (not world.TimeAdvancing) world
            state <- { M1SceneState.cancelGesture state with CombinedState = { state.CombinedState with RewindPreviewOpt = None } }
        let menu =
            PlaygroundVisual.button "Menu" "Menu (Esc)" (v2 273.0f 138.0f) 90.0f world ||
            (World.isKeyboardKeyPressed KeyboardKey.Escape world && state.CombinedState.RewindPreviewOpt.IsNone)
        PlaygroundVisual.text "Title" true (v2 0.0f 165.0f) (v2 620.0f 24.0f) 13.0f Color.White
            (if state.Room = CombinedInteractionsRoom then "BLOBBO - WATER, HEAT AND MACHINES" else "M1 - BODY AND CONTROL STUDY") world
        let hint =
            if state.CombinedState.RewindPreviewOpt.IsSome then "PROP PREVIEW ONLY - Blobbo and water stay here. Enter commits; Esc cancels."
            elif not world.TimeAdvancing then "PAUSED - gesture cancelled. Resume (Space) to interact."
            else
                match state.InputReplay with
                | PendingInputReplay | PlayingInputReplay _ -> "REPEATING INPUT FROM RESET - same gesture, not the original trajectory or room state."
                | FinishedInputReplay -> "INPUT REPEAT COMPLETE - fresh start, current body and control. Touch Blobbo to try again."
                | CancelledInputReplay ReplayOutsidePlayfield -> "REPEAT CANCELLED - translated gesture leaves the reset playfield. Record a shorter stroke."
                | CancelledInputReplay ReplayMissingContact -> "REPEAT CANCELLED - the reset body did not receive the initial contact."
                | NoInputReplay -> Scene06_M1ControlStudyDispatcher.GestureHint state.ControlMode
        PlaygroundVisual.text "Gesture hint" true (v2 0.0f 90.0f) (v2 620.0f 16.0f) 8.0f
            (if world.TimeAdvancing then Color.White else Color.Yellow) hint world
        World.endGroup world

        let mutable actions = CombinedRoomActions.none
        World.beginGroup "M1 Actions" [] world
        PlaygroundVisual.panel "Footer" (v2 0.0f -144.0f) (v2 640.0f 74.0f) world
        if state.Room = CombinedInteractionsRoom then
            if state.CombinedState.RewindPreviewOpt.IsSome then
                actions <-
                    { actions with
                        RewindEarlier = PlaygroundVisual.button "Earlier" "Earlier (Left)" (v2 -237.0f -119.0f) 146.0f world || World.isKeyboardKeyDown KeyboardKey.Left world
                        RewindLater = PlaygroundVisual.button "Later" "Later (Right)" (v2 -80.0f -119.0f) 146.0f world || World.isKeyboardKeyDown KeyboardKey.Right world
                        CommitRewind = PlaygroundVisual.button "Commit" "Commit prop (Enter)" (v2 80.0f -119.0f) 146.0f world || World.isKeyboardKeyPressed KeyboardKey.Enter world
                        CancelRewind = PlaygroundVisual.button "Cancel" "Cancel (Esc)" (v2 237.0f -119.0f) 146.0f world || World.isKeyboardKeyPressed KeyboardKey.Escape world }
            else
                actions <-
                    { actions with
                        AddWater = PlaygroundVisual.button "Water" "Water (W)" (v2 -237.0f -119.0f) 146.0f world || World.isKeyboardKeyPressed KeyboardKey.W world || World.isKeyboardKeyPressed KeyboardKey.Grave world
                        ToggleHeat = PlaygroundVisual.button "Heat" (ThermalStationVisual.heatStatus state.CombinedState.HeatEnabled state.CombinedState.HeaterContacts) (v2 -80.0f -119.0f) 146.0f world || World.isKeyboardKeyPressed KeyboardKey.H world
                        ReviveBalloons = PlaygroundVisual.button "Revive" "Revive balloons (B)" (v2 80.0f -119.0f) 146.0f world || World.isKeyboardKeyPressed KeyboardKey.B world
                        BeginRewind = PlaygroundVisual.button "Rewind" "Rewind PROP (Down)" (v2 237.0f -119.0f) 146.0f world || World.isKeyboardKeyPressed KeyboardKey.Down world }
        World.endGroup world
        (state, actions, menu)

    static member private Room (bounds : Box2) world =
        PlaygroundVisual.sprite "Backdrop" bounds.Center bounds.Size -30.0f (color 0.035f 0.065f 0.14f 1.0f) Assets.Default.White world
        for index in -7 .. 7 do
            let x = single index * 64.0f
            if x > bounds.Min.X && x < bounds.Max.X then
                PlaygroundVisual.segment (sprintf "Grid %d" index) (v2 x bounds.Min.Y) (v2 x bounds.Max.Y) 1.0f -29.0f (color 0.25f 0.45f 0.6f 0.16f) world
        World.doBlockBody2d "Room Border"
            [Entity.Position .= bounds.Center.V3
             Entity.Size .= bounds.Size.V3
             Entity.BodyShape .= ContourShape
                { Links = [|v3 -0.5f 0.5f 0.0f; v3 0.5f 0.5f 0.0f; v3 0.5f -0.5f 0.0f; v3 -0.5f -0.5f 0.0f|]
                  Closed = true; TransformOpt = None; PropertiesOpt = None }
             Entity.Visible .= false] world |> ignore
        for (name, start, stop) in
            [("Floor", bounds.Min, v2 bounds.Max.X bounds.Min.Y)
             ("Ceiling", v2 bounds.Min.X bounds.Max.Y, bounds.Max)
             ("Left wall", bounds.Min, v2 bounds.Min.X bounds.Max.Y)
             ("Right wall", v2 bounds.Max.X bounds.Min.Y, bounds.Max)] do
            PlaygroundVisual.segment name start stop 3.0f -10.0f (color 0.2f 0.8f 1.0f 0.7f) world

    static member private Preview configuration state (sample : M1PointerSample) (center : PhysicsBodyTransform) (bounds : Box2) world =
        for index in 0 .. state.Trail.Length - 1 do
            let progress = single (index + 1) / single state.Trail.Length
            PlaygroundVisual.sprite (sprintf "Trail %02d" index) state.Trail[index] (v2Dup (3.0f + progress * 8.0f))
                2.0f (color 0.3f 0.85f 1.0f (progress * 0.32f)) Assets.Default.Ball world
        match state.ControlState with
        | ControlActive active ->
            let contact = center.BodyCenter + active.GrabOffset
            PlaygroundVisual.segment "Grip tether" contact sample.Position 3.0f 4.0f (color 1.0f 0.82f 0.28f 0.8f) world
            PlaygroundVisual.sprite "Pointer halo" sample.Position (v2Dup 22.0f) 4.1f (color 1.0f 0.9f 0.35f 0.45f) Assets.Default.Ball world
            let velocityChange = M1Control.previewVelocityChange configuration state.ControlMode sample active
            if state.ControlMode = PullSling then
                let launchVelocity = M1Control.releaseVelocity configuration velocityChange center.BodyLinearVelocity
                let gravity = (Constants.Physics.GravityDefault * Constants.Engine.Meter2d).V2
                for index in 1 .. 12 do
                    let time = single index * 0.075f
                    let point = center.BodyCenter + launchVelocity * time + gravity * (0.5f * time * time)
                    if bounds.Contains point <> ContainmentType.Disjoint then
                        PlaygroundVisual.sprite (sprintf "Trajectory %02d" index) point (v2Dup (7.0f - single index * 0.25f))
                            3.8f (color 1.0f 0.42f 0.65f (0.9f - single index * 0.05f)) Assets.Default.Ball world
            elif state.ControlMode = SwipeSmack && velocityChange.LengthSquared () > 0.01f then
                let direction = Vector2.Normalize velocityChange
                let strength = velocityChange.Length () / configuration.MaximumReleaseSpeed
                let start = center.BodyCenter + direction * 34.0f
                let stop = start + direction * (28.0f + strength * 70.0f)
                let normal = v2 -direction.Y direction.X
                let arrowBase = stop - direction * 14.0f
                let tint = color 1.0f 0.48f 0.12f 0.95f
                PlaygroundVisual.segment "Swipe direction" start stop 6.0f 3.9f tint world
                PlaygroundVisual.segment "Swipe left" stop (arrowBase + normal * 8.0f) 5.0f 3.9f tint world
                PlaygroundVisual.segment "Swipe right" stop (arrowBase - normal * 8.0f) 5.0f 3.9f tint world
        | ControlInactive -> ()

    override _.Process (_, screen, world) =
        if screen.GetSelected world then
            let configuration = M1ControlConfiguration.defaultConfiguration
            let (current, actions, menu) = Scene06_M1ControlStudyDispatcher.Controls (screen.GetM1SceneState world) world
            let mutable state = current
            if menu then
                state <- M1SceneState.cancelGesture state
                World.setTimeAdvancing true world
                World.setEye2dSize (World.getDisplayVirtualResolution ()).V2 world
                screen.SetGameplayState Quit world
            else
                let combined = state.Room = CombinedInteractionsRoom
                let bounds = if combined then CombinedRoom.PlayBounds else toyBounds
                let spawn = if combined then CombinedRoom.SubjectSpawn else toySpawn
                World.setEye2dCenter v2Zero world
                World.setEye2dSize (World.getDisplayVirtualResolution ()).V2 world
                World.beginGroup (sprintf "M1 Fixture %d" state.FixtureVersion) [] world
                Scene06_M1ControlStudyDispatcher.Room bounds world
                if state.Room = GenerousTargetRoom then
                    PlaygroundVisual.sprite "Target outer" targetBounds.Center (v2 138.0f 96.0f) -3.0f (color 0.18f 0.85f 1.0f 0.22f) Assets.Default.Ball world
                    PlaygroundVisual.sprite "Target middle" targetBounds.Center (v2 98.0f 70.0f) -2.9f (color 0.95f 0.24f 0.62f 0.5f) Assets.Default.Ball world
                    PlaygroundVisual.sprite "Target core" targetBounds.Center (v2 52.0f 38.0f) -2.8f (color 1.0f 0.88f 0.28f 0.85f) Assets.Default.Ball world
                    World.doBlockBody2d "Target deck"
                        [Entity.Position .= v3 targetBounds.Center.X (targetBounds.Min.Y - 7.0f) 0.0f
                         Entity.Size .= v3 190.0f 14.0f 0.0f
                         Entity.StaticImage .= Assets.Default.White
                         Entity.Color .= color 0.15f 0.65f 0.85f 0.8f] world |> ignore
                let combinedEntitiesOpt = if combined then Some (CombinedRoom.declare state.CombinedState world) else None
                match state.Candidate with
                | LegacyGraph ->
                    World.doEntity<M1LegacyBlobboDispatcher> "Subject"
                        [Entity.Position .= spawn.V3
                         Entity.Size .= v3 96.0f 96.0f 0.0f
                         Entity.Elevation .= 3.0f
                         Entity.WorldFluidEmitter .= (combinedEntitiesOpt |> Option.map _.FluidEmitter.EntityAddress |> Option.defaultValue Address.empty)] world
                | SimplifiedRing | StableHull ->
                    World.doEntity<M1BlobboDispatcher> "Subject"
                        [Entity.Position .= spawn.V3
                         Entity.Size .= v3 96.0f 96.0f 0.0f
                         Entity.Elevation .= 3.0f
                         Entity.M1BodyCandidate @= state.Candidate
                         Entity.M1FixtureVersion @= state.FixtureVersion] world
                let subject = world.DeclaredEntity
                match combinedEntitiesOpt with
                | Some entities -> state <- { state with CombinedState = CombinedRoom.advanceInteractions subject entities actions state.CombinedState world }
                | None -> ()
                let advancing = world.TimeAdvancing
                if not advancing || actions.BeginRewind || actions.CommitRewind || actions.CancelRewind then
                    state <- M1SceneState.cancelGesture state
                let center = Scene06_M1ControlStudyDispatcher.CenterSnapshot state.Candidate subject world
                if state.InputReplay = PendingInputReplay then
                    state <- { state with InputReplay = M1Trace.prepareReplay bounds center.BodyCenter state.LastTrace }
                let replaying = match state.InputReplay with PlayingInputReplay _ -> true | _ -> false
                let sample =
                    match state.InputReplay with
                    | PlayingInputReplay (samples, index) -> samples[index]
                    | _ -> M1PointerInput.fromMouse (int (world.UpdateTime % int64 Int32.MaxValue)) (World.getMousePosition2dWorld false world) world
                let pointerAllowed = M1PointerInput.insidePlayfield bounds sample
                if not pointerAllowed && (replaying || state.ControlState <> ControlInactive) then
                    state <- M1SceneState.cancelGesture state
                    if replaying then state <- { state with InputReplay = CancelledInputReplay ReplayOutsidePlayfield }
                let wasActive = match state.ControlState with ControlActive _ -> true | ControlInactive -> false
                let contact = M1Geometry.containsPoint sample.Position (M1BlobboVisual.bodyPoints state.Candidate subject world)
                let output =
                    M1Control.stepWhenAdvancing (advancing && pointerAllowed) configuration state.ControlMode
                        center.BodyCenter center.BodyLinearVelocity contact sample state.ControlState

                if advancing then
                    M1BodyControl.apply configuration state.Candidate subject output world
                if state.Candidate <> LegacyGraph then
                    subject.SetM1VisualPull (if output.State = ControlInactive then v2Zero else sample.Position - center.BodyCenter) world

                let mutable captureRev = state.CaptureRev
                let mutable captureOverflow = state.CaptureOverflow
                let mutable lastTrace = state.LastTrace
                if advancing && not replaying then
                    if output.Started then
                        captureRev <- [{ sample with Tick = 0 }]
                        captureOverflow <- false
                    elif wasActive then
                        match M1Trace.tryRecord sample captureRev with
                        | Some samples -> captureRev <- samples
                        | None -> captureOverflow <- true
                        if output.Released then
                            lastTrace <- if captureOverflow then Array.empty else captureRev |> List.rev |> M1Trace.normalize
                            captureRev <- []
                let inputReplay =
                    if advancing && replaying then M1Trace.advanceReplay output.Started state.InputReplay
                    elif output.Started then NoInputReplay
                    else state.InputReplay
                let mutable outcome = state.Outcome
                let mutable flightSeconds = state.FlightSeconds
                let mutable stableSeconds = state.StableSeconds
                let mutable hits = state.Hits
                let mutable misses = state.Misses
                if advancing && output.Released then
                    outcome <- if state.Room = GenerousTargetRoom then TargetInFlight else FreePlayComplete
                    flightSeconds <- 0.0f
                    stableSeconds <- 0.0f
                elif advancing && outcome = TargetInFlight then
                    flightSeconds <- flightSeconds + configuration.FixedDeltaSeconds
                    let speed = center.BodyLinearVelocity.Length ()
                    stableSeconds <- if speed < 28.0f then stableSeconds + configuration.FixedDeltaSeconds else 0.0f
                    if targetBounds.Contains center.BodyCenter <> ContainmentType.Disjoint && speed < 105.0f then
                        outcome <- TargetHit
                        hits <- hits + 1
                    elif flightSeconds >= 5.0f || stableSeconds >= 0.55f then
                        outcome <- TargetMiss
                        misses <- misses + 1
                let trail =
                    if advancing && (wasActive || output.Started || outcome <> AwaitingAttempt || replaying) then
                        let trail =
                            if Array.isEmpty state.Trail || Vector2.DistanceSquared (Array.last state.Trail, center.BodyCenter) >= 36.0f then
                                Array.append state.Trail [|center.BodyCenter|]
                            else state.Trail
                        if trail.Length > 18 then trail[trail.Length - 18 ..] else trail
                    else state.Trail
                state <-
                    { state with
                        ControlState = output.State
                        CaptureRev = captureRev
                        CaptureOverflow = captureOverflow
                        LastTrace = lastTrace
                        InputReplay = inputReplay
                        Attempts = state.Attempts + (if output.Released then 1 else 0)
                        Hits = hits
                        Misses = misses
                        Outcome = outcome
                        FlightSeconds = flightSeconds
                        StableSeconds = stableSeconds
                        Trail = trail
                        LastAcceleration = output.Acceleration
                        LastVelocityChange = if output.Released then output.VelocityChange else state.LastVelocityChange }
                Scene06_M1ControlStudyDispatcher.Preview configuration state sample center bounds world
                World.endGroup world

                World.beginGroup "M1 Status" [] world
                match combinedEntitiesOpt with
                | Some entities ->
                    let lines = CombinedRoom.diagnostics subject entities state.CombinedState world
                    for index in 0 .. lines.Length - 1 do
                        PlaygroundVisual.text (sprintf "Combined status %d" index) true (v2 0.0f (-143.0f - single index * 14.0f))
                            (v2 624.0f 16.0f) 7.5f Color.White lines[index] world
                | None ->
                    let outcome =
                        if not advancing then "PAUSED - gesture cancelled"
                        else
                            match state.ControlState with
                            | ControlActive _ ->
                                match state.ControlMode with
                                | GrabThrow -> "GRABBING - flick and release"
                                | PullSling -> "AIMING SLING - release along dots"
                                | SwipeSmack -> "AIMING SWIPE - release along arrow"
                            | ControlInactive -> Scene06_M1ControlStudyDispatcher.OutcomeText state.Outcome
                    let replayProgress =
                        match state.InputReplay with
                        | PlayingInputReplay (samples, index) -> sprintf "%d/%d" index samples.Length
                        | PendingInputReplay -> "starting"
                        | FinishedInputReplay -> "complete"
                        | CancelledInputReplay _ -> "cancelled"
                        | NoInputReplay -> "--"
                    let lines =
                        [|sprintf "%s | %A: %d bodies / %d joints | attempts %d hits %d misses %d"
                              outcome state.Candidate (M1Topology.bodyCount state.Candidate) (M1Topology.jointCount state.Candidate) state.Attempts state.Hits state.Misses
                          sprintf "accel %.0f / %.0f units/s2 | release delta-v %.0f / %.0f units/s | speed %.0f / %.0f"
                              (state.LastAcceleration.Length ()) configuration.MaximumAcceleration (state.LastVelocityChange.Length ())
                              configuration.MaximumReleaseSpeed (center.BodyLinearVelocity.Length ()) configuration.MaximumSpeed
                          sprintf "trace %d/%d samples | repeat %s | resets %d | frame %.2fms physics %.2fms"
                              state.LastTrace.Length M1Trace.MaximumSamples replayProgress state.ResetCount
                              world.Timers.FrameTimer.Elapsed.TotalMilliseconds world.Timers.PhysicsTimer.Elapsed.TotalMilliseconds|]
                    for index in 0 .. lines.Length - 1 do
                        PlaygroundVisual.text (sprintf "Study status %d" index) true (v2 0.0f (-122.0f - single index * 22.0f))
                            (v2 624.0f 20.0f) 8.0f Color.White lines[index] world
                World.endGroup world
            screen.SetM1SceneState state world