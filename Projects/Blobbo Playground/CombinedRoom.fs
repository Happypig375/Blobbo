namespace BlobboPlayground
open System.Numerics
open Prime
open Nu

type CombinedRewindPreview =
    { PreviewUpdates : int64
      ResumeAdvancing : bool }

type CombinedRoomState =
    { HeatEnabled : bool
      WaterBursts : int
      EvaporatedParticles : int
      CondensedParticles : int
      HeatedContainers : int
      HeaterContacts : int
      RewindPreviewOpt : CombinedRewindPreview option
      RewindCommits : int }

[<RequireQualifiedAccess>]
module CombinedRoomState =

    let initial =
        { HeatEnabled = true
          WaterBursts = 0
          EvaporatedParticles = 0
          CondensedParticles = 0
          HeatedContainers = 0
          HeaterContacts = 0
          RewindPreviewOpt = None
          RewindCommits = 0 }

type CombinedRoomActions =
    { AddWater : bool
      ToggleHeat : bool
      ReviveBalloons : bool
      BeginRewind : bool
      RewindEarlier : bool
      RewindLater : bool
      CommitRewind : bool
      CancelRewind : bool }

[<RequireQualifiedAccess>]
module CombinedRoomActions =

    let none =
        { AddWater = false
          ToggleHeat = false
          ReviveBalloons = false
          BeginRewind = false
          RewindEarlier = false
          RewindLater = false
          CommitRewind = false
          CancelRewind = false }

type CombinedRoomEntities =
    { FluidEmitter : Entity
      Balloons : Entity array
      Fan : Entity
      RewindProp : Entity }

[<RequireQualifiedAccess>]
module CombinedRoom =

    let PlayBounds = box2 (v2 -310.0f -103.0f) (v2 620.0f 180.0f)
    let SubjectSpawn = v2 -246.0f -44.0f
    let HeaterPlateBounds = box2 (v2 -64.0f -103.0f) (v2 128.0f 10.0f)
    let HeaterBounds = box2 (v2 -58.0f -93.0f) (v2 116.0f 28.0f)
    let CoolerBounds = box2 (v2 -64.0f 57.0f) (v2 128.0f 20.0f)
    let FluidParticleCap = 1200
    let WaterBurstCount = 64
    let RewindMaximumUpdates = 300L
    let RewindMaximumRecords = 1800

    let clampRewind available requested = max 0L (min (min RewindMaximumUpdates available) requested)

    let trimHistory history =
        let rec trim elapsed count records =
            match records with
            | record :: rest when elapsed < RewindMaximumUpdates && count < RewindMaximumRecords ->
                record :: trim (elapsed + record.TimePassed.Updates) (count + 1) rest
            | _ -> []
        trim 0L 0 history

    let overlaps (left : Box2) (right : Box2) =
        left.Min.X <= right.Max.X && left.Max.X >= right.Min.X &&
        left.Min.Y <= right.Max.Y && left.Max.Y >= right.Min.Y

    let convertParticle heatEnabled (particle : FluidParticle) =
        // Box2D phase replacement preserves velocity; lift and falling come from the new phase's gravity.
        if heatEnabled && particle.FluidParticleConfig = "Water" &&
           HeaterBounds.Contains particle.FluidParticlePosition.V2 <> ContainmentType.Disjoint then
            { particle with FluidParticleConfig = "Smoke" }
        elif particle.FluidParticleConfig = "Smoke" &&
             CoolerBounds.Contains particle.FluidParticlePosition.V2 <> ContainmentType.Disjoint then
            { particle with FluidParticleConfig = "Water" }
        else particle

    let declare state world =
        World.doEntity<FluidEmitter2dDispatcher> "Combined Water"
            [Entity.Position .= PlayBounds.Center.V3
             Entity.Size .= PlayBounds.Size.V3
             Entity.FluidParticlesMax .= FluidParticleCap] world
        let fluidEmitter = world.DeclaredEntity
        PlaygroundVisual.configureFluidAppearance fluidEmitter world

        let balloons =
            [|for (name, spawn) in [("Left balloon", v2 -134.0f -55.0f); ("Right balloon", v2 247.0f -55.0f)] do
                  World.doEntity<WaterBalloonDispatcher> name
                      [Entity.Position .= spawn.V3
                       Entity.WorldFluidEmitter .= fluidEmitter.EntityAddress
                       Entity.Elevation .= 2.0f] world
                  yield world.DeclaredEntity|]

        // The physical plate ends exactly where the visible conversion region begins.
        World.doBlockBody2d "Heater plate collision"
            [Entity.Position .= HeaterPlateBounds.Center.V3
             Entity.Size .= HeaterPlateBounds.Size.V3
             Entity.Visible .= false] world |> ignore
        ThermalStationVisual.heater "Heater" HeaterPlateBounds HeaterBounds state.HeatEnabled state.HeaterContacts world
        ThermalStationVisual.cooler "Cooler" CoolerBounds world

        let fanPosition = v3 142.0f 7.0f 0.0f
        World.doOrbBody2d "Fan anchor"
            [Entity.Position .= fanPosition
             Entity.Size .= v3Dup 8.0f
             Entity.BodyType .= Static
             Entity.Sensor .= true
             Entity.StaticImage .= Assets.Default.Ball
             Entity.Color .= Color.White
             Entity.Elevation .= 4.0f] world |> ignore
        let fanAnchor = world.DeclaredEntity
        World.doBoxBody2d "Fan horizontal"
            [Entity.Position |= fanPosition
             Entity.Size .= v3 90.0f 7.0f 0.0f
             Entity.Substance .= Mass 0.12f
             Entity.LinearDamping .= 0.0f
             Entity.AngularDamping .= 0.12f
             Entity.Color .= color 0.9f 0.74f 0.26f 1.0f
             Entity.StaticImage .= Assets.Default.White] world |> ignore
        let fan = world.DeclaredEntity
        World.doBoxBody2d "Fan vertical"
            [Entity.Position |= fanPosition
             Entity.Size .= v3 7.0f 90.0f 0.0f
             Entity.Substance .= Mass 0.12f
             Entity.LinearDamping .= 0.0f
             Entity.AngularDamping .= 0.12f
             Entity.Color .= color 0.9f 0.74f 0.26f 1.0f
             Entity.StaticImage .= Assets.Default.White] world |> ignore
        let fanVertical = world.DeclaredEntity
        World.doBodyJoint2d "Fan weld"
            [Entity.BodyJointTarget .= fan.EntityAddress
             Entity.BodyJointTarget2 .= fanVertical.EntityAddress
             Entity.BodyJoint |= Box2dNetBodyJoint { CreateBodyJoint = fun _ _ a b physicsWorld ->
                let mutable definition = Box2D.NET.B2Joints.b2DefaultWeldJointDef ()
                definition.``base``.bodyIdA <- a
                definition.``base``.bodyIdB <- b
                Box2D.NET.B2Joints.b2CreateWeldJoint (physicsWorld, &definition) }] world |> ignore
        World.doBodyJoint2d "Fan pin"
            [Entity.BodyJointTarget .= fanAnchor.EntityAddress
             Entity.BodyJointTarget2 .= fan.EntityAddress
             Entity.BodyJoint |= Box2dNetBodyJoint { CreateBodyJoint = fun _ _ a b physicsWorld ->
                let mutable definition = Box2D.NET.B2Joints.b2DefaultRevoluteJointDef ()
                definition.``base``.bodyIdA <- a
                definition.``base``.bodyIdB <- b
                Box2D.NET.B2Joints.b2CreateRevoluteJoint (physicsWorld, &definition) }] world |> ignore
        PlaygroundVisual.text "Fan label" false (v2 177.0f 65.0f) (v2 204.0f 16.0f) 8.0f Color.White
            "PUSH FAN WITH BLOBBO / WATER" world

        World.doBoxBody2d "Local rewind prop"
            [Entity.Position |= v3 -220.0f 42.0f 0.0f
             Entity.Size .= v3Dup 18.0f
             Entity.LinearVelocity |= v3 95.0f 20.0f 0.0f
             Entity.Friction .= 0.0f
             Entity.Restitution .= 0.8f
             Entity.Color .= color 0.82f 0.4f 1.0f 1.0f
             Entity.StaticImage .= Assets.Default.White
             Entity.FacetNames .= set [nameof RewindableFacet]] world |> ignore
        let rewindProp = world.DeclaredEntity

        PlaygroundVisual.text "Prop label" false (v2 -210.0f 66.0f) (v2 188.0f 16.0f) 8.0f (color 0.88f 0.64f 1.0f 1.0f)
            "PURPLE BOX: LOCAL REWIND" world
        { FluidEmitter = fluidEmitter
          Balloons = balloons
          Fan = fan
          RewindProp = rewindProp }

    let advanceInteractions (subject : Entity) entities actions state world =
        let prop = entities.RewindProp
        let mutable state = state
        let history = trimHistory (prop.GetRewindHistory world)
        prop.SetRewindHistory history world
        let available =
            min RewindMaximumUpdates
                ((prop.GetTimeSinceLastHistoryEntry world).Updates + (history |> List.sumBy _.TimePassed.Updates))

        if actions.BeginRewind && state.RewindPreviewOpt.IsNone then
            state <- { state with RewindPreviewOpt = Some { PreviewUpdates = 0L; ResumeAdvancing = world.TimeAdvancing } }
            World.setTimeAdvancing false world
        match state.RewindPreviewOpt with
        | Some preview ->
            let delta = (if actions.RewindEarlier then 6L else 0L) - (if actions.RewindLater then 6L else 0L)
            let preview = { preview with PreviewUpdates = clampRewind available (preview.PreviewUpdates + delta) }
            if actions.CommitRewind || actions.CancelRewind then
                if actions.CommitRewind && preview.PreviewUpdates > 0L then
                    World.publish { RewindAnchorOpt = ValueNone; RewindTime = GameTime.ofUpdates preview.PreviewUpdates }
                        prop.RewindEvent prop world
                prop.SetRewindPreview None world
                World.setTimeAdvancing preview.ResumeAdvancing world
                state <-
                    { state with
                        RewindPreviewOpt = None
                        RewindCommits = state.RewindCommits + (if actions.CommitRewind then 1 else 0) }
            else
                state <- { state with RewindPreviewOpt = Some preview }
                prop.SetRewindPreview (Some (GameTime.ofUpdates preview.PreviewUpdates)) world
                World.setTimeAdvancing false world
        | None ->
            if (prop.GetRewindPreview world).IsSome then prop.SetRewindPreview None world

        if world.TimeAdvancing then
            if actions.ToggleHeat then state <- { state with HeatEnabled = not state.HeatEnabled }
            if actions.AddWater then
                let center = subject.GetBlobboCenter world
                let spawn =
                    v2
                        (max (PlayBounds.Min.X + 20.0f) (min (PlayBounds.Max.X - 20.0f) center.BodyCenter.X))
                        (min (PlayBounds.Max.Y - 22.0f) (center.BodyCenter.Y + 55.0f))
                let count = min WaterBurstCount (max 0 (FluidParticleCap - (entities.FluidEmitter.GetFluidParticles world).Length))
                World.emitFluidParticles (WaterParticles.burst "Water" spawn (v2 0.0f -18.0f) 4.0f count)
                    (entities.FluidEmitter.GetFluidEmitterId world) world
                state <- { state with WaterBursts = state.WaterBursts + 1 }
            if actions.ReviveBalloons then
                for balloon in entities.Balloons do World.publish () balloon.ReviveEvent balloon world
            let mutable evaporated = 0
            let mutable condensed = 0
            World.chooseFluidParticles (fun particle ->
                let converted = convertParticle state.HeatEnabled particle
                if converted.FluidParticleConfig <> particle.FluidParticleConfig then
                    if converted.FluidParticleConfig = "Smoke" then evaporated <- evaporated + 1
                    else condensed <- condensed + 1
                ValueSome converted) (entities.FluidEmitter.GetFluidEmitterId world) world
            let mutable heated = 0
            let mutable contacts = 0
            let intactBalloons = entities.Balloons |> Array.filter (fun balloon -> (balloon.GetWaterBalloonCenter world).IsSome)
            for container in Array.append [|subject|] intactBalloons do
                // Manual bodies publish their physical envelope as Perimeter; Bounds also includes render overflow.
                if overlaps HeaterBounds (container.GetPerimeter world).Box2 then
                    contacts <- contacts + 1
                    if state.HeatEnabled && container.GetWaterContent world > 0 then
                        World.publish () container.HeatEvent container world
                        heated <- heated + 1
            state <-
                { state with
                    EvaporatedParticles = state.EvaporatedParticles + evaporated
                    CondensedParticles = state.CondensedParticles + condensed
                    HeatedContainers = state.HeatedContainers + heated
                    HeaterContacts = contacts }
        state

    let diagnostics (subject : Entity) entities state world =
        let particles = entities.FluidEmitter.GetFluidParticles world
        let water = particles |> Seq.sumBy (fun particle -> if particle.FluidParticleConfig = "Water" then 1 else 0)
        let smoke = particles.Length - water
        let balloonStatus =
            entities.Balloons
            |> Array.map (fun balloon -> if (balloon.GetWaterBalloonCenter world).IsSome then "intact" else "popped")
            |> String.concat "/"
        let rewindText =
            match state.RewindPreviewOpt with
            | Some preview -> sprintf "PROP PREVIEW %.1fs | Left/Right scrub | Enter commit | Esc cancel" (single preview.PreviewUpdates / 60.0f)
            | None -> sprintf "Local prop history %d/%d records | %d commits" (entities.RewindProp.GetRewindHistory world).Length RewindMaximumRecords state.RewindCommits
        [|sprintf "Blobbo water %d/32 | balloons %s | fluid %d/%d (water %d smoke %d)"
              (subject.GetWaterContent world) balloonStatus particles.Length FluidParticleCap water smoke
          sprintf "Evaporated %d | condensed %d | heat releases %d | fan %.2f rad/s | Math / Eyes: Menu"
              state.EvaporatedParticles state.CondensedParticles state.HeatedContainers
              (entities.Fan.GetAngularVelocity world).Z
          rewindText|]