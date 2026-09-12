namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu

/// Physical representations compared by the M1 experiment.
type M1BodyCandidate =
    | LegacyGraph
    | SimplifiedRing
    | StableHull

/// Direct-manipulation mappings compared against the same bodies and rooms.
type M1ControlMode =
    | GrabThrow
    | PullSling
    | SwipeSmack

/// Controlled spaces used for free play and landing measurement.
type M1Room =
    | EmptyToyRoom
    | GenerousTargetRoom
    /// A bounded cross-system room for participant interaction feedback.
    | CombinedInteractionsRoom

/// Input provenance stays explicit so touch can use the same control path as mouse and replay.
type M1PointerDevice =
    | MousePointer
    | TouchPointer of int64
    | ReplayPointer

type M1PointerPhase =
    | PointerIdle
    | PointerPressed
    | PointerHeld
    | PointerReleased

type M1PointerSample =
    { Tick : int
      Position : Vector2
      Phase : M1PointerPhase
      Device : M1PointerDevice }

type M1ReplayCancellation =
    | ReplayOutsidePlayfield
    | ReplayMissingContact

/// Input replay owns only its translated samples and progress, not a snapshot of the physical world.
type M1InputReplay =
    | NoInputReplay
    | PendingInputReplay
    | PlayingInputReplay of Samples : M1PointerSample array * Index : int
    | FinishedInputReplay
    | CancelledInputReplay of M1ReplayCancellation

type M1ControlConfiguration =
    { FixedDeltaSeconds : single
      PointerVelocityWindowSeconds : single
      GrabSpring : single
      GrabDamping : single
      PullHoldSpring : single
      PullHoldDamping : single
      GrabReleaseGain : single
      PullSpeedPerPixel : single
      SwipeImpulseGain : single
      MaximumAcceleration : single
      MaximumReleaseSpeed : single
      MaximumSpeed : single }

[<RequireQualifiedAccess>]
module M1ControlConfiguration =

    let defaultConfiguration =
        { FixedDeltaSeconds = 1.0f / 60.0f
          PointerVelocityWindowSeconds = 0.08f
          GrabSpring = 70.0f
          GrabDamping = 12.0f
          PullHoldSpring = 65.0f
          PullHoldDamping = 14.0f
          GrabReleaseGain = 0.16f
          PullSpeedPerPixel = 2.8f
          SwipeImpulseGain = 0.2f
          MaximumAcceleration = 2400.0f
          MaximumReleaseSpeed = 420.0f
          MaximumSpeed = 620.0f }

type M1ActiveControl =
    { PressPosition : Vector2
      BodyPositionAtPress : Vector2
      GrabOffset : Vector2
      PointerSamplesRev : M1PointerSample list
      PointerVelocity : Vector2 }

type M1ControlState =
    | ControlInactive
    | ControlActive of M1ActiveControl

type M1ControlOutput =
    { State : M1ControlState
      Acceleration : Vector2
      VelocityChange : Vector2
      Started : bool
      Released : bool }

[<RequireQualifiedAccess>]
module M1Control =

    let clampMagnitude maximum (value : Vector2) =
        let magnitudeSquared = value.LengthSquared ()
        if magnitudeSquared > maximum * maximum && magnitudeSquared > 0.0f then
            value * (maximum / sqrt magnitudeSquared)
        else value

    let clampSpeed (configuration : M1ControlConfiguration) (velocity : Vector2) =
        clampMagnitude configuration.MaximumSpeed velocity

    /// World units per second, shared by release execution and the live trajectory preview.
    let releaseVelocityChange
        (configuration : M1ControlConfiguration)
        (mode : M1ControlMode)
        (pressPosition : Vector2)
        (pointerPosition : Vector2)
        (pointerVelocity : Vector2) =
        (match mode with
         | GrabThrow -> pointerVelocity * configuration.GrabReleaseGain
         | PullSling -> (pressPosition - pointerPosition) * configuration.PullSpeedPerPixel
         | SwipeSmack -> pointerVelocity * configuration.SwipeImpulseGain)
        |> clampMagnitude configuration.MaximumReleaseSpeed

    let releaseVelocity configuration velocityChange bodyVelocity =
        clampSpeed configuration (bodyVelocity + velocityChange)

    let private updatePointer (configuration : M1ControlConfiguration) (sample : M1PointerSample) (active : M1ActiveControl) =
        let elapsedSince previous = single (sample.Tick - previous.Tick) * configuration.FixedDeltaSeconds
        let window = configuration.PointerVelocityWindowSeconds
        // Keep the window plus one older sample so its boundary can be interpolated across sparse input.
        let rec trim = function
            | newer :: older :: rest when elapsedSince older < window -> newer :: trim (older :: rest)
            | newer :: older :: _ -> [newer; older]
            | samples -> samples
        let samples = sample :: (active.PointerSamplesRev |> List.filter (fun previous -> previous.Tick < sample.Tick)) |> trim
        let velocity =
            match List.rev samples with
            | oldest :: next :: _ ->
                let elapsed = elapsedSince oldest
                let duration = min window elapsed
                let origin =
                    if elapsed > window then
                        let interval = single (next.Tick - oldest.Tick) * configuration.FixedDeltaSeconds
                        Vector2.Lerp (oldest.Position, next.Position, (elapsed - duration) / interval)
                    else oldest.Position
                (sample.Position - origin) / duration
            | _ -> v2Zero
        { active with PointerSamplesRev = samples; PointerVelocity = velocity }

    /// Preview and release evaluate the same recent motion window at the current sample time.
    let previewVelocityChange configuration mode sample active =
        let active = updatePointer configuration sample active
        releaseVelocityChange configuration mode active.PressPosition sample.Position active.PointerVelocity

    let private continueControl
        (configuration : M1ControlConfiguration)
        (mode : M1ControlMode)
        (bodyPosition : Vector2)
        (bodyVelocity : Vector2)
        (sample : M1PointerSample)
        (active : M1ActiveControl) =
        let active = updatePointer configuration sample active
        let acceleration =
            (match mode with
             | GrabThrow ->
                 let target = sample.Position - active.GrabOffset
                 (target - bodyPosition) * configuration.GrabSpring - bodyVelocity * configuration.GrabDamping
             | PullSling ->
                 (active.BodyPositionAtPress - bodyPosition) * configuration.PullHoldSpring -
                 bodyVelocity * configuration.PullHoldDamping
             | SwipeSmack -> v2Zero)
            |> clampMagnitude configuration.MaximumAcceleration
        { State = ControlActive active
          Acceleration = acceleration
          VelocityChange = v2Zero
          Started = false
          Released = false }

    let private releaseControl
        (configuration : M1ControlConfiguration)
        (mode : M1ControlMode)
        (sample : M1PointerSample)
        (active : M1ActiveControl) =
        let velocityChange = previewVelocityChange configuration mode sample active
        { State = ControlInactive
          Acceleration = v2Zero
          VelocityChange = velocityChange
          Started = false
          Released = true }

    /// The caller supplies contact with the rendered body and applies acceleration in world units/s^2.
    let step
        (configuration : M1ControlConfiguration)
        (mode : M1ControlMode)
        (bodyPosition : Vector2)
        (bodyVelocity : Vector2)
        contactAllowed
        (sample : M1PointerSample)
        (state : M1ControlState) =
        match sample.Phase, state with
        | PointerPressed, ControlInactive when contactAllowed ->
            let active =
                { PressPosition = sample.Position
                  BodyPositionAtPress = bodyPosition
                  GrabOffset = sample.Position - bodyPosition
                  PointerSamplesRev = [sample]
                  PointerVelocity = v2Zero }
            { State = ControlActive active
              Acceleration = v2Zero
              VelocityChange = v2Zero
              Started = true
              Released = false }
        | (PointerPressed | PointerHeld), ControlActive active ->
            continueControl configuration mode bodyPosition bodyVelocity sample active
        | PointerReleased, ControlActive active ->
            releaseControl configuration mode sample active
        | PointerIdle, ControlActive active ->
            { State = ControlActive active
              Acceleration = v2Zero
              VelocityChange = v2Zero
              Started = false
              Released = false }
        | _, _ ->
            { State = ControlInactive
              Acceleration = v2Zero
              VelocityChange = v2Zero
              Started = false
              Released = false }

    /// Pausing cancels the gesture, so a later release cannot launch the body after resuming.
    let stepWhenAdvancing advancing configuration mode bodyPosition bodyVelocity contactAllowed sample state =
        if advancing then
            step configuration mode bodyPosition bodyVelocity contactAllowed sample state
        else
            { State = ControlInactive
              Acceleration = v2Zero
              VelocityChange = v2Zero
              Started = false
              Released = false }

[<RequireQualifiedAccess>]
module M1Trace =

    let MaximumSamples = 600

    let tryRecord sample samples =
        let count = List.length samples
        if count < MaximumSamples then Some ({ sample with Tick = count } :: samples)
        else None

    let normalize samples =
        samples
        |> Seq.mapi (fun tick sample -> { sample with Tick = tick })
        |> Seq.toArray

    let private phaseCode = function
        | PointerIdle -> 0u
        | PointerPressed -> 1u
        | PointerHeld -> 2u
        | PointerReleased -> 3u

    let private deviceCode = function
        | MousePointer -> 1UL
        | TouchPointer pointerId -> 2UL ^^^ uint64 pointerId
        | ReplayPointer -> 3UL

    let checksum samples =
        let mix hash value = (hash ^^^ value) * 1099511628211UL
        let mutable hash = 14695981039346656037UL
        for sample in samples do
            hash <- mix hash (uint64 sample.Tick)
            hash <- mix hash (uint64 (BitConverter.SingleToInt32Bits sample.Position.X))
            hash <- mix hash (uint64 (BitConverter.SingleToInt32Bits sample.Position.Y))
            hash <- mix hash (uint64 (phaseCode sample.Phase))
            hash <- mix hash (deviceCode sample.Device)
        sprintf "%016X" hash

    let asReplay samples =
        samples |> Array.map (fun sample -> { sample with Device = ReplayPointer })

    /// Translate the gesture once onto a fresh contact, retaining timing and every relative pointer displacement.
    let prepareReplay (bounds : Box2) origin (samples : M1PointerSample array) =
        if Array.isEmpty samples then NoInputReplay
        else
            let firstPosition = samples[0].Position
            let translated =
                samples
                |> Array.map (fun sample ->
                    { sample with Position = origin + (sample.Position - firstPosition); Device = ReplayPointer })
            if translated |> Array.exists (fun sample -> bounds.Contains sample.Position = ContainmentType.Disjoint) then
                CancelledInputReplay ReplayOutsidePlayfield
            else PlayingInputReplay (translated, 0)

    let advanceReplay started replay =
        match replay with
        | PlayingInputReplay (_, 0) when not started -> CancelledInputReplay ReplayMissingContact
        | PlayingInputReplay (samples, index) when index + 1 < samples.Length -> PlayingInputReplay (samples, index + 1)
        | PlayingInputReplay _ -> FinishedInputReplay
        | NoInputReplay | PendingInputReplay | FinishedInputReplay | CancelledInputReplay _ -> replay

[<RequireQualifiedAccess>]
module M1Geometry =

    /// Match the nonzero winding fill, including boundary contact and concave deformations.
    let containsPoint (point : Vector2) (polygon : Vector2 array) =
        let mutable winding = 0
        let mutable onEdge = false
        if polygon.Length >= 3 then
            for index in 0 .. polygon.Length - 1 do
                let start = polygon[index]
                let stop = polygon[(index + 1) % polygon.Length]
                let edge = stop - start
                let offset = point - start
                let cross = edge.X * offset.Y - edge.Y * offset.X
                let along = Vector2.Dot (offset, edge)
                if edge.LengthSquared () > 0.000001f then
                    if abs cross <= 0.001f && along >= 0.0f && along <= edge.LengthSquared () then onEdge <- true
                elif Vector2.DistanceSquared (point, start) <= 0.000001f then onEdge <- true
                if start.Y <= point.Y && stop.Y > point.Y && cross > 0.0f then winding <- winding + 1
                elif start.Y > point.Y && stop.Y <= point.Y && cross < 0.0f then winding <- winding - 1
        onEdge || winding <> 0

[<RequireQualifiedAccess>]
module M1Topology =

    let bodyCount = function
        | LegacyGraph -> 33
        | SimplifiedRing -> 13
        | StableHull -> 1

    let jointCount = function
        | LegacyGraph -> 528
        | SimplifiedRing -> 24
        | StableHull -> 0

    let constraintReduction candidate =
        1.0f - single (jointCount candidate) / single (jointCount LegacyGraph)

type M1VerificationResult =
    { Passed : bool
      Checks : string array }

[<RequireQualifiedAccess>]
module M1Verification =

    let private samples =
        [|{ Tick = 0; Position = v2 0.0f 0.0f; Phase = PointerPressed; Device = MousePointer }
          { Tick = 1; Position = v2 24.0f 10.0f; Phase = PointerHeld; Device = MousePointer }
          { Tick = 2; Position = v2 58.0f 26.0f; Phase = PointerHeld; Device = MousePointer }
          { Tick = 3; Position = v2 96.0f 44.0f; Phase = PointerReleased; Device = MousePointer }|]

    let private simulate mode replaySamples =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let mutable state = ControlInactive
        let mutable position = v2Zero
        let mutable velocity = v2Zero
        let mutable maximumAcceleration = 0.0f
        let mutable maximumReleaseSpeed = 0.0f
        for sample in replaySamples do
            let output = M1Control.step configuration mode position velocity true sample state
            state <- output.State
            maximumAcceleration <- max maximumAcceleration (output.Acceleration.Length ())
            maximumReleaseSpeed <- max maximumReleaseSpeed (output.VelocityChange.Length ())
            velocity <- velocity + output.Acceleration * configuration.FixedDeltaSeconds + output.VelocityChange
            velocity <- M1Control.clampSpeed configuration velocity
            position <- position + velocity * configuration.FixedDeltaSeconds
        (position, velocity, maximumAcceleration, maximumReleaseSpeed)

    let evaluate () =
        let configuration = M1ControlConfiguration.defaultConfiguration
        let trace = M1Trace.normalize samples
        let replay = M1Trace.asReplay trace
        let checks = ResizeArray<string> ()
        let mutable passed = true
        let check condition description =
            passed <- passed && condition
            checks.Add ((if condition then "PASS " else "FAIL ") + description)
        check (M1Topology.bodyCount SimplifiedRing < M1Topology.bodyCount LegacyGraph)
            "simplified ring reduces body count"
        check (M1Topology.jointCount SimplifiedRing <= 24)
            "simplified ring reduces the legacy 528-joint graph to 24 joints"
        check (M1Topology.jointCount StableHull = 0)
            "stable hull has no constraints"
        check (M1Trace.checksum trace = M1Trace.checksum (M1Trace.normalize trace))
            "trace normalization and checksum are deterministic"
        for mode in [GrabThrow; PullSling; SwipeSmack] do
            let first = simulate mode replay
            let second = simulate mode replay
            let (position, velocity, maximumAcceleration, maximumReleaseSpeed) = first
            let (position2, velocity2, _, _) = second
            check (Vector2.Distance (position, position2) < 0.0001f && Vector2.Distance (velocity, velocity2) < 0.0001f)
                (sprintf "%A replay is deterministic" mode)
            check (maximumAcceleration <= configuration.MaximumAcceleration + 0.001f)
                (sprintf "%A acceleration is bounded" mode)
            check (maximumReleaseSpeed <= configuration.MaximumReleaseSpeed + 0.001f)
                (sprintf "%A release velocity change is bounded" mode)
            check (velocity.Length () <= configuration.MaximumSpeed + 0.001f)
                (sprintf "%A speed is bounded" mode)
        { Passed = passed; Checks = checks.ToArray () }

    let report () =
        let result = evaluate ()
        for check in result.Checks do Console.WriteLine check
        Console.WriteLine (sprintf "M1 trace %s" (M1Trace.checksum (M1Trace.normalize samples)))
        if result.Passed then 0 else 1

[<RequireQualifiedAccess>]
module M1Launch =
    let mutable Direct = false
    let mutable Combined = false