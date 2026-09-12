namespace BlobboPlayground
open System
open System.Numerics
open Box2D.NET
open Prime
open Nu

type PhysicsBodyTransform =
    { BodyCenter : Vector2
      BodyRotation : Quaternion
      BodyLinearVelocity : Vector2
      BodyAngularVelocity : Vector2 }

[<RequireQualifiedAccess>]
module BlobboBodyModel =

    let CenterBodyIndex = -1
    let ContourCount = 32
    let CenterToContourDistance = 32.0f
    let ContourRadius = CenterToContourDistance * sin (MathF.PI / single ContourCount)
    let CenterRadius = ContourRadius * 1.5f
    let MaximumWaterContent = 32

    let waterScale waterContent = 1.0f + single waterContent / single MaximumWaterContent * 0.5f

    let resizeForWater previous current center (contour : PhysicsBodyTransform array) =
        let ratio = waterScale current / waterScale previous
        contour
        |> Array.map (fun point ->
            { point with BodyCenter = center + (point.BodyCenter - center) * ratio })

    /// The same expanded contour owns both rendering and direct pointer contact.
    let renderedPoints (contour : PhysicsBodyTransform array) =
        let points = contour |> Array.map _.BodyCenter
        if points.Length < 3 then points
        else
            let mutable signedArea = 0.0f
            for index in 0 .. points.Length - 1 do
                let point = points[index]
                let next = points[(index + 1) % points.Length]
                signedArea <- signedArea + point.X * next.Y - next.X * point.Y
            let points = if signedArea < 0.0f then Array.rev points else points
            Array.init points.Length (fun index ->
                let previous = points[index] - points[(index + points.Length - 1) % points.Length]
                let next = points[(index + 1) % points.Length] - points[index]
                let normal (edge : Vector2) =
                    let length = edge.Length ()
                    if length > 0.0001f then v2 edge.Y -edge.X / length else v2Zero
                let outward = normal previous + normal next
                let length = outward.Length ()
                points[index] + if length > 0.0001f then outward * (ContourRadius / length) else v2Zero)

    let canAbsorb waterContent (center : Vector2) (particle : FluidParticle) =
        waterContent < MaximumWaterContent && particle.FluidParticleConfig = "Water" &&
        Vector2.Distance (particle.FluidParticlePosition.V2, center) < CenterToContourDistance * 1.5f

module [<AutoOpen>] BlobboExtensions =
    type Entity with
        member this.GetBlobboCenter world : PhysicsBodyTransform = this.Get (nameof this.BlobboCenter) world
        member this.SetBlobboCenter (value : PhysicsBodyTransform) world = this.Set (nameof this.BlobboCenter) value world
        member this.BlobboCenter = lens (nameof this.BlobboCenter) this this.GetBlobboCenter this.SetBlobboCenter
        member this.GetBlobboContour world : PhysicsBodyTransform array = this.Get (nameof this.BlobboContour) world
        member this.SetBlobboContour (value : PhysicsBodyTransform array) world = this.Set (nameof this.BlobboContour) value world
        member this.BlobboContour = lens (nameof this.BlobboContour) this this.GetBlobboContour this.SetBlobboContour
        member this.ReviveEvent = stoa<unit> "Revive/Event" --> this

type BlobboDispatcher () =
    inherit Entity2dDispatcherImSim (true, false, false)

    static let centerBodyIndex = BlobboBodyModel.CenterBodyIndex
    static let centerToContourDistance = BlobboBodyModel.CenterToContourDistance
    static let initialBlobboCenter =
        { BodyCenter = v2Zero
          BodyRotation = Quaternion.Identity
          BodyLinearVelocity = v2Zero
          BodyAngularVelocity = v2Zero }
    static let initialBlobboContour =
        let count = BlobboBodyModel.ContourCount
        [|for i in 0 .. count - 1 ->
            let angle = single i * MathF.TWO_PI / single count
            { BodyCenter = v2 (cos angle * centerToContourDistance) (sin angle * centerToContourDistance)
              BodyRotation = Quaternion.Identity
              BodyLinearVelocity = v2Zero
              BodyAngularVelocity = v2Zero }
        |]
    static let contourCircleRadius = BlobboBodyModel.ContourRadius
    static let centerCircleRadius = BlobboBodyModel.CenterRadius
    static let maxWaterContent = BlobboBodyModel.MaximumWaterContent
    static let blobboFullCollisionCategories = "10000000000000000" // bit 16, outside fluid default mask (0xFFFF)

    static member Facets =
        [typeof<WaterContainerFacet>]
    static member Properties =
        [define Entity.BlobboCenter initialBlobboCenter
         define Entity.BlobboContour initialBlobboContour
         define Entity.AwakeTimeStamp 0
         nonPersistent Entity.PhysicsMotion ManualMotion]

    override _.Register (blobbo, world) =
        World.monitor (fun event world ->
            let previous = event.Data.Previous :?> int
            let current = event.Data.Value :?> int
            let center = blobbo.GetBlobboCenter world
            let contour =
                blobbo.GetBlobboContour world
                |> BlobboBodyModel.resizeForWater previous current center.BodyCenter
            blobbo.SetBlobboContour contour world
            if World.getBodyExists { BodySource = blobbo; BodyIndex = centerBodyIndex } world then blobbo.PropagatePhysics world
            Cascade) blobbo.WaterContent.ChangeEvent blobbo world

    override _.RegisterPhysics (blobbo, world) =
        let waterContent = blobbo.GetWaterContent world
        let isFull = waterContent >= maxWaterContent
        let collisionCategories = if isFull then blobboFullCollisionCategories else "1"
        let expansionScale = BlobboBodyModel.waterScale waterContent
        let registerPhysicsTransform radius i physicsTransform =
            let bodyId = { BodySource = blobbo; BodyIndex = i }
            let bodyProperties =
                { Enabled = true
                  Center = physicsTransform.BodyCenter.V3
                  Rotation = physicsTransform.BodyRotation
                  Scale = v3One
                  BodyShape = SphereShape { Radius = radius; TransformOpt = None; PropertiesOpt = None }
                  BodyType = Dynamic
                  SleepingAllowed = true
                  Friction = Constants.Physics.FrictionDefault
                  Restitution = 0f
                  LinearVelocity = physicsTransform.BodyLinearVelocity.V3
                  LinearDamping = 0f
                  AngularVelocity = physicsTransform.BodyAngularVelocity.V3
                  AngularDamping = 0f
                  AngularFactor = v3One
                  KinematicPushLimitOpt = None
                  Substance = Density 1f
                  Gravity = GravityWorld
                  CharacterProperties = PogoSpringCharacterProperties PogoSpringCharacterProperties.defaultProperties
                  VehicleProperties = VehiclePropertiesAbsent
                  CollisionDetection = Continuous
                  CollisionGroup = 0
                  CollisionCategories = Physics.categorizeCollisionMask collisionCategories
                  CollisionMask = Physics.categorizeCollisionMask Constants.Physics.CollisionWildcard
                  Sensor = false
                  BodyIndex = i }
            World.createBody2d bodyId bodyProperties world
        let existingContour = blobbo.GetBlobboContour world
        Array.iteri (registerPhysicsTransform contourCircleRadius) existingContour
        registerPhysicsTransform centerCircleRadius centerBodyIndex (blobbo.GetBlobboCenter world)

        let registerBodyJoint bodyIdA bodyIdB distance i =
            let bodyJointProperties =
                { BodyJoint = Box2dNetBodyJoint { CreateBodyJoint = fun toPhysics _ a b world ->
                    let mutable jointDef = B2Joints.b2DefaultDistanceJointDef ()
                    jointDef.``base``.bodyIdA <- a
                    jointDef.``base``.bodyIdB <- b
                    jointDef.length <- toPhysics distance
                    jointDef.enableSpring <- true
                    jointDef.hertz <- 4f
                    jointDef.dampingRatio <- 0.5f
                    B2Joints.b2CreateDistanceJoint (world, &jointDef) }
                  BodyJointTarget = bodyIdA
                  BodyJointTarget2 = bodyIdB
                  BodyJointEnabled = true
                  BreakingPointOpt = None
                  Broken = false
                  CollideConnected = true
                  BodyJointIndex = i }
            World.createBodyJoint2d blobbo bodyJointProperties world
        for i in 0 .. existingContour.Length - 1 do
            for j in i + 1 .. existingContour.Length - 1 do
                let distance = Vector2.Distance (initialBlobboContour[i].BodyCenter, initialBlobboContour[j].BodyCenter) * expansionScale
                registerBodyJoint
                    { BodySource = blobbo; BodyIndex = i }
                    { BodySource = blobbo; BodyIndex = j }
                    distance
                    (existingContour.Length + i * existingContour.Length + j)
            registerBodyJoint
                { BodySource = blobbo; BodyIndex = i }
                { BodySource = blobbo; BodyIndex = centerBodyIndex }
                (centerToContourDistance * expansionScale)
                i
    override _.UnregisterPhysics (blobbo, world) =
        let existingContour = blobbo.GetBlobboContour world
        for i in 0 .. existingContour.Length - 1 do
            for j in i + 1 .. existingContour.Length - 1 do
                World.destroyBodyJoint2d
                    { BodySource = blobbo; BodyIndex = i }
                    { BodySource = blobbo; BodyIndex = j }
                    { BodyJointSource = blobbo; BodyJointIndex = existingContour.Length + i * existingContour.Length + j }
                    world
            World.destroyBodyJoint2d
                { BodySource = blobbo; BodyIndex = i }
                { BodySource = blobbo; BodyIndex = centerBodyIndex }
                { BodyJointSource = blobbo; BodyJointIndex = i }
                world
        World.destroyBody2d { BodySource = blobbo; BodyIndex = centerBodyIndex } world
        World.destroyBodies2d [for i in 0 .. existingContour.Length - 1 -> { BodySource = blobbo; BodyIndex = i }] world
    override _.Process (blobbo, world) =

        if world.ContextInitializing then
            let spawnPos = blobbo.GetPosition world
            let center = blobbo.GetBlobboCenter world
            let delta = spawnPos.V2 - center.BodyCenter
            let center' = { center with BodyCenter = spawnPos.V2 }
            blobbo.SetBlobboCenter center' world
            let centerBodyId = { BodySource = blobbo; BodyIndex = centerBodyIndex }
            World.setBodyCenter spawnPos centerBodyId world
            let contour = Array.copy (blobbo.GetBlobboContour world)
            for i in 0 .. contour.Length - 1 do
                let t = contour[i]
                let t' = { t with BodyCenter = t.BodyCenter + delta }
                contour[i] <- t'
                World.setBodyCenter t'.BodyCenter.V3 { BodySource = blobbo; BodyIndex = i } world
            blobbo.SetBlobboContour contour world
        let contour = Array.copy (blobbo.GetBlobboContour world)
        for event in World.doSubscriptionToBodyEvents "BlobboBodyEvents" blobbo world do
            match event with
            | BodyTransformData transform when transform.BodyId.BodyIndex = centerBodyIndex ->
                blobbo.SetBlobboCenter
                    { BodyCenter = transform.BodyCenter.V2
                      BodyRotation = transform.BodyRotation
                      BodyLinearVelocity = transform.BodyLinearVelocity.V2
                      BodyAngularVelocity = transform.BodyAngularVelocity.V2 } world
            | BodyTransformData transform when transform.BodyId.BodyIndex >= 0 && transform.BodyId.BodyIndex < contour.Length ->
                contour[transform.BodyId.BodyIndex] <-
                    { BodyCenter = transform.BodyCenter.V2
                      BodyRotation = transform.BodyRotation
                      BodyLinearVelocity = transform.BodyLinearVelocity.V2
                      BodyAngularVelocity = transform.BodyAngularVelocity.V2 }
            | _ -> ()
        blobbo.SetBlobboContour contour world
        let center = blobbo.GetBlobboCenter world
        let waterContent = blobbo.GetWaterContent world

        // Absorb nearby fluid particles when not full.
        if world.TimeAdvancing && waterContent < maxWaterContent then
            match tryResolve (blobbo.GetWorldFluidEmitter world) blobbo with
            | Some emitter ->
                let mutable absorbed = 0
                let remainingCapacity = maxWaterContent - waterContent
                World.chooseFluidParticles (fun particle ->
                    if absorbed < remainingCapacity && BlobboBodyModel.canAbsorb waterContent center.BodyCenter particle then
                        absorbed <- absorbed + 1
                        ValueNone
                    else ValueSome particle)
                    (emitter.GetFluidEmitterId world) world
                if absorbed > 0 then
                    let waterContent' = min maxWaterContent (waterContent + absorbed)
                    blobbo.SetWaterContent waterContent' world
            | None -> ()

        let perimeter =
            (box2 (center.BodyCenter - v2Dup centerCircleRadius) (v2Dup (centerCircleRadius * 2f)), blobbo.GetBlobboContour world)
            ||> Array.fold (fun perimeter t -> box2 (t.BodyCenter - v2Dup contourCircleRadius) (v2Dup (contourCircleRadius * 2f)) |> perimeter.Combine)
        blobbo.SetPerimeter perimeter.Box3 world

    override _.Render (_, blobbo, world) =
        let contour = blobbo.GetBlobboContour world
        if contour.Length >= 3 then
            let position = blobbo.GetPosition world
            let size =
                let s = (blobbo.GetSize world).V2
                v2 (max 0.0001f s.X) (max 0.0001f s.Y)
            let worldPoints = BlobboBodyModel.renderedPoints contour
            // Normalize to entity-local space for tessellation.
            let points = worldPoints |> Array.map (fun p -> (p - position.V2) / size)
            let commands = Array.zeroCreate<ContourCommand> (points.Length + 1)
            commands[0] <- MoveTo points[0]
            for i in 1 .. points.Length - 1 do
                commands[i] <- LineTo points[i]
            commands[points.Length] <- CloseContour
            let tessellation =
                Contour.make
                    (ContourFill.ofColorWinding Color.Aqua ContourWinding.NonZero)
                    ContourStroke.none
                    commands
                    size
            let mutable transform = blobbo.GetTransform world
            transform.Rotation <- Quaternion.Identity
            transform.Scale <- v3One
            World.renderContour
                { Transform = transform
                  ClipOpt = ValueNone
                  Contour = tessellation } world