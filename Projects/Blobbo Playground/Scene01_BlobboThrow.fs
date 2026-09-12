namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu
open BlobboPlayground

// this represents the state of gameplay simulation.
type GameplayState =
    | Playing
    | Quit

/// Scene-owned values for the default experiment and its measurements.
type Scene01ExperimentConfiguration =
    { FluidEmitterSize : Vector3
      FluidParticleCap : int
      Balloon1Position : Vector3
      Balloon2Position : Vector3
      BlobboPosition : Vector3
      PointerUpdateRateHz : double
      SettleSpeedThreshold : single
      SettleAngularSpeedThreshold : single
      SettleHoldSeconds : double }

module Scene01ExperimentConfiguration =
    let defaultConfiguration =
        { FluidEmitterSize = v3 640f 400f 0f
          FluidParticleCap = 20000
          Balloon1Position = v3 -90f 0f 0f
          Balloon2Position = v3 90f 0f 0f
          BlobboPosition = v3 0f 0f 0f
          PointerUpdateRateHz = 60.0
          SettleSpeedThreshold = 0.5f
          SettleAngularSpeedThreshold = 0.5f
          SettleHoldSeconds = 0.25 }

type Scene01Telemetry =
    { PreviousPointerOpt : Vector2 option
      WasHeld : bool
      ReleaseElapsedSecondsOpt : double option
      SettlingElapsedSeconds : double
      SettleDurationOpt : double option
      ResetCount : int
      FixtureVersion : int }

module Scene01Telemetry =
    let initial =
        { PreviousPointerOpt = None
          WasHeld = false
          ReleaseElapsedSecondsOpt = None
          SettlingElapsedSeconds = 0.0
          SettleDurationOpt = None
          ResetCount = 0
          FixtureVersion = 0 }

// this extends the Screen API to expose the Gameplay model as well as the Quit event.
[<AutoOpen>]
module GameplayExtensions =
    type Screen with
        member this.GetGameplayState world : GameplayState = this.Get (nameof Screen.GameplayState) world
        member this.SetGameplayState (value : GameplayState) world = this.Set (nameof Screen.GameplayState) value world
        member this.GameplayState = lens (nameof Screen.GameplayState) this this.GetGameplayState this.SetGameplayState
        member this.GetHeldEntity world : Entity Address = this.Get (nameof Screen.HeldEntity) world
        member this.SetHeldEntity (value : Entity Address) world = this.Set (nameof Screen.HeldEntity) value world
        member this.HeldEntity = lens (nameof Screen.HeldEntity) this this.GetHeldEntity this.SetHeldEntity
        member this.GetScene01Telemetry world : Scene01Telemetry = this.Get (nameof Screen.Scene01Telemetry) world
        member this.SetScene01Telemetry (value : Scene01Telemetry) world = this.Set (nameof Screen.Scene01Telemetry) value world
        member this.Scene01Telemetry = lens (nameof Screen.Scene01Telemetry) this this.GetScene01Telemetry this.SetScene01Telemetry

// this is the dispatcher that defines the behavior of the screen where gameplay takes place.
type Scene01_BlobboThrowDispatcher () =
    inherit ScreenDispatcherImSim ()

    // here we define default property values
    static member Properties =
        [define Screen.GameplayState Quit
         define Screen.HeldEntity Address.empty
         define Screen.Scene01Telemetry Scene01Telemetry.initial]

    // here we define the behavior of our gameplay
    override this.Process (_, screen, world) =

        if screen.GetSelected world then
            let configuration = Scene01ExperimentConfiguration.defaultConfiguration
            let telemetry = screen.GetScene01Telemetry world
            World.beginGroup (sprintf "Fixture %d" telemetry.FixtureVersion) [] world
            let pauseButton =
                World.doButton "Pause"
                    [Entity.Position .= v3 -104f -144f 0f
                     Entity.Size .= v3 96f 22f 0f
                     Entity.Elevation .= 10f
                     Entity.FontSizing .= Some 8f
                     Entity.Text @= (if world.TimeAdvancing then "Pause (Space)" else "Resume (Space)")] world
            let pauseRequested = pauseButton || World.isKeyboardKeyPressed KeyboardKey.Space world
            let wasTimeAdvancing = world.TimeAdvancing
            let gameplayAdvancing = wasTimeAdvancing && not pauseRequested
            if pauseRequested then
                // Clear first when transitioning into pause; the time setter may become
                // observable only after this process, but the joint must disappear now.
                if wasTimeAdvancing then
                    screen.SetHeldEntity Address.empty world
                World.setTimeAdvancing (not wasTimeAdvancing) world
            // A paused scene must never retain a drag owner; clearing before declarations
            // also prevents the mouse joint from being reconstructed for this frame.
            if not wasTimeAdvancing then
                screen.SetHeldEntity Address.empty world
            // declare border
            World.doBlockBody2d "Border"
                [Entity.Size .= (World.getDisplayVirtualResolution ()).V3
                 Entity.BodyShape .= ContourShape
                     { Links =
                         [|v3 -0.5f 0.5f 0f
                           v3 0.5f 0.5f 0f
                           v3 0.5f -0.5f 0f
                           v3 -0.5f -0.5f 0f|]
                       Closed = true
                       TransformOpt = None
                       PropertiesOpt = None }
                 Entity.Elevation .= -1f
                 Entity.StaticImage .= Assets.Gameplay.Background] world |> ignore
                
            World.doEntity<FluidEmitter2dDispatcher> "World fluid"
                 [Entity.Size .= configuration.FluidEmitterSize
                  Entity.FluidParticlesMax .= configuration.FluidParticleCap] world
            let fluidEmitter = world.DeclaredEntity
            if gameplayAdvancing && World.isKeyboardKeyDown KeyboardKey.Grave world then
               let spawn = v2 0f 0f
               World.emitFluidParticles (SArray.init 32 (fun _ ->
                   let jitter = v2 (Gen.randomf * 2f - 1f) (Gen.randomf - 0.5f) * 32.0f
                   { FluidParticlePosition = (spawn + jitter).V3; FluidParticleVelocity = v3Zero; FluidParticleConfig = "Water" }))
                (fluidEmitter.GetFluidEmitterId world)
                world
            World.doEntity<WaterBalloonDispatcher> "Balloon1"
                [Entity.Position .= configuration.Balloon1Position
                 Entity.WorldFluidEmitter .= fluidEmitter.EntityAddress] world
            let balloon1 = world.DeclaredEntity
            World.doEntity<WaterBalloonDispatcher> "Balloon2"
                [Entity.Position .= configuration.Balloon2Position
                 Entity.WorldFluidEmitter .= fluidEmitter.EntityAddress] world
            let balloon2 = world.DeclaredEntity
            World.doEntity<BlobboDispatcher> "Blobbo"
                [Entity.Position .= configuration.BlobboPosition
                 Entity.WorldFluidEmitter .= fluidEmitter.EntityAddress
                 Entity.FacetNames .= set [nameof FeelerFacet]] world
            let blobbo = world.DeclaredEntity
            let eyeBounds = World.getEye2dBounds world
            let raw = World.getMousePosition2dWorld false world
            let mousePosition = v3 (max eyeBounds.Min.X (min eyeBounds.Max.X raw.X)) (max eyeBounds.Min.Y (min eyeBounds.Max.Y raw.Y)) 0f
            let heldBeforeInput = gameplayAdvancing && screen.GetHeldEntity world <> Address.empty
            let dt = 1.0 / configuration.PointerUpdateRateHz
            let pointerVelocity =
                match telemetry.PreviousPointerOpt, dt with
                | Some previous, delta when delta > 0.0 -> (mousePosition.V2 - previous) / single delta
                | _ -> v2Zero
            let heldAfterInput =
                if gameplayAdvancing && blobbo.GetTouched world then
                    screen.SetHeldEntity blobbo.EntityAddress world
                    true
                elif heldBeforeInput && World.isMouseButtonReleased MouseLeft world then
                    screen.SetHeldEntity Address.empty world
                    false
                else heldBeforeInput
            if heldBeforeInput && not heldAfterInput then
                screen.SetScene01Telemetry
                    { telemetry with
                        ReleaseElapsedSecondsOpt = Some 0.0
                        SettlingElapsedSeconds = 0.0
                        SettleDurationOpt = None
                        WasHeld = false } world
            elif heldAfterInput then
                screen.SetScene01Telemetry
                    { telemetry with
                        WasHeld = true
                        ReleaseElapsedSecondsOpt = None
                        SettlingElapsedSeconds = 0.0
                        SettleDurationOpt = None } world
            let telemetry = screen.GetScene01Telemetry world
            if heldAfterInput then
                screen.SetHeldEntity blobbo.EntityAddress world
            if screen.GetHeldEntity world <> Address.empty then
                World.doOrbBody2d "Mouse"
                    [Entity.Position @= mousePosition
                     Entity.Visible .= false
                     Entity.Sensor .= true] world |> ignore
                World.doBodyJoint2d "Mouse joint"
                    [Entity.BodyJointTarget .= stoa "^/Mouse"
                     Entity.BodyJointTarget2 .= screen.GetHeldEntity world
                     Entity.BodyJoint |= Box2dNetBodyJoint { CreateBodyJoint = fun _ _ a b world ->
                        let mutable jointDef = Box2D.NET.B2Joints.b2DefaultDistanceJointDef ()
                        jointDef.``base``.bodyIdA <- a
                        jointDef.``base``.bodyIdB <- b
                        jointDef.length <- 0f
                        jointDef.enableLimit <- true
                        jointDef.minLength <- 0f
                        jointDef.maxLength <- 0f
                        Box2D.NET.B2Joints.b2CreateDistanceJoint (world, &jointDef)
                        }] world |> ignore

            World.endGroup world
            World.beginGroup "Interface" [] world

            if screen.GetSelected world then
                if world.TimeAdvancing then ()
                else
                    World.doStaticSprite "Overlay" 
                        [Entity.Position .= v3 0f 0f 0.1f
                         Entity.Size .= (World.getDisplayVirtualResolution ()).V3
                         Entity.Absolute .= true
                         Entity.StaticImage .= Assets.Default.White
                         Entity.Color .= color 0.5f 0.5f 0.5f 0.5f] world |> ignore
                if gameplayAdvancing && World.isKeyboardKeyPressed KeyboardKey.Enter world then
                    World.publish () blobbo.ReviveEvent blobbo world

            let fluidParticleCount = (fluidEmitter.GetFluidParticles world).Length
            let center = blobbo.GetBlobboCenter world
            let held = screen.GetHeldEntity world <> Address.empty
            let extension = Vector2.Distance (mousePosition.V2, center.BodyCenter)
            let speed = center.BodyLinearVelocity.Length ()
            let angularSpeed = center.BodyAngularVelocity.Length ()
            let telemetry =
                match telemetry.ReleaseElapsedSecondsOpt, telemetry.SettleDurationOpt with
                | Some elapsed, None when not held && gameplayAdvancing ->
                    let settlingElapsed = telemetry.SettlingElapsedSeconds + dt
                    let settled = speed <= configuration.SettleSpeedThreshold && angularSpeed <= configuration.SettleAngularSpeedThreshold
                    if settled && settlingElapsed >= configuration.SettleHoldSeconds then
                        { telemetry with
                            ReleaseElapsedSecondsOpt = Some (elapsed + dt)
                            SettlingElapsedSeconds = settlingElapsed
                            SettleDurationOpt = Some (elapsed + dt) }
                    elif settled then
                        { telemetry with
                            ReleaseElapsedSecondsOpt = Some (elapsed + dt)
                            SettlingElapsedSeconds = settlingElapsed }
                    else
                        { telemetry with
                            ReleaseElapsedSecondsOpt = Some (elapsed + dt)
                            SettlingElapsedSeconds = 0.0 }
                | _ -> telemetry
            screen.SetScene01Telemetry { telemetry with PreviousPointerOpt = Some mousePosition.V2 } world
            let settleText =
                match telemetry.ReleaseElapsedSecondsOpt, telemetry.SettleDurationOpt with
                | None, _ -> "n/a"
                | Some _, Some duration -> sprintf "%.2fs" duration
                | Some elapsed, None -> sprintf "elapsed %.2fs stable %.2fs" elapsed telemetry.SettlingElapsedSeconds
            let balloonBodyCount (balloon : Entity) =
                32 + (balloon.GetWaterBalloonCenter world |> Option.map (fun _ -> 1) |> Option.defaultValue 0)
            let balloonJointCount (balloon : Entity) =
                32 + (balloon.GetWaterBalloonCenter world |> Option.map (fun _ -> 32) |> Option.defaultValue 0)
            let blobboBodyCount = 1 + (blobbo.GetBlobboContour world).Length
            let logicalBodyCount = 1 + blobboBodyCount + balloonBodyCount balloon1 + balloonBodyCount balloon2 + (if held then 1 else 0)
            let logicalJointCount = 528 + balloonJointCount balloon1 + balloonJointCount balloon2 + (if held then 1 else 0)
            World.doText "TelemetryTiming"
                [Entity.Position .= v3 0f 165f 0f
                 Entity.Size .= v3 620f 20f 0f
                 Entity.Elevation .= 10f
                 Entity.FontSizing .= Some 8f
                 Entity.Justification .= Justified (JustifyLeft, JustifyMiddle)
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.TextColor .= Color.White
                 Entity.Text @= sprintf "M0 frame %d | frame %.2fms | physics %.2fms" world.UpdateTime world.Timers.FrameTimer.Elapsed.TotalMilliseconds world.Timers.PhysicsTimer.Elapsed.TotalMilliseconds] world
            World.doText "TelemetryTopology"
                [Entity.Position .= v3 0f 145f 0f
                 Entity.Size .= v3 620f 20f 0f
                 Entity.Elevation .= 10f
                 Entity.FontSizing .= Some 8f
                 Entity.Justification .= Justified (JustifyLeft, JustifyMiddle)
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.TextColor .= Color.White
                 Entity.Text @= sprintf "logical bodies %d joints %d | expected baseline 100/656 | engine totals unavailable" logicalBodyCount logicalJointCount] world
            World.doText "TelemetryPointer"
                [Entity.Position .= v3 0f 125f 0f
                 Entity.Size .= v3 620f 20f 0f
                 Entity.Elevation .= 10f
                 Entity.FontSizing .= Some 8f
                 Entity.Justification .= Justified (JustifyLeft, JustifyMiddle)
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.TextColor .= Color.White
                 Entity.Text @= sprintf "pointer (%.1f, %.1f) v (%.1f, %.1f) px/s @60Hz | held %b extension %.1f | force unavailable" mousePosition.X mousePosition.Y pointerVelocity.X pointerVelocity.Y held extension] world
            World.doText "TelemetryBlobbo"
                [Entity.Position .= v3 0f 105f 0f
                 Entity.Size .= v3 620f 20f 0f
                 Entity.Elevation .= 10f
                 Entity.FontSizing .= Some 8f
                 Entity.Justification .= Justified (JustifyLeft, JustifyMiddle)
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.TextColor .= Color.White
                 Entity.Text @= sprintf "Blobbo v (%.2f, %.2f) a %.2f | settle %s | water %d | fluid %d/%d | resets %d" center.BodyLinearVelocity.X center.BodyLinearVelocity.Y angularSpeed settleText (blobbo.GetWaterContent world) fluidParticleCount configuration.FluidParticleCap telemetry.ResetCount] world

            World.doText "Instructions"
                [Entity.Position .= v3 0.0f 82.0f 0.0f
                 Entity.Size .= v3 620.0f 20.0f 0.0f
                 Entity.Absolute .= true
                 Entity.Elevation .= 10.0f
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.FontSizing .= Some 8.0f
                 Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)
                 Entity.Text @= (if world.TimeAdvancing then "Drag Blobbo to throw | hold `: water | Space: pause | Enter: revive | R: reset"
                                 else "PAUSED | release is cancelled | Resume (Space) to interact") ] world

            if World.doButton "Reset"
                [Entity.Position .= v3 104f -144f 0f
                 Entity.Elevation .= 10f
                 Entity.FontSizing .= Some 8f
                 Entity.Text .= "Reset (R)"] world ||
               World.isKeyboardKeyPressed KeyboardKey.R world then
                screen.SetHeldEntity Address.empty world
                let nextTelemetry =
                    { Scene01Telemetry.initial with
                        ResetCount = telemetry.ResetCount + 1
                        FixtureVersion = telemetry.FixtureVersion + 1 }
                screen.SetScene01Telemetry nextTelemetry world

            // declare quit button
            if World.doButton "Quit"
                [Entity.Position .= v3 232.0f -144.0f 0.0f
                 Entity.FontSizing .= Some 8f
                 Entity.Text .= "Quit"
                 Entity.Elevation .= 10f] world then
                screen.SetGameplayState Quit world

            World.endGroup world
