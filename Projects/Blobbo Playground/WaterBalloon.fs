namespace BlobboPlayground
open System
open System.Numerics
open Box2D.NET
open Prime
open Nu

module [<AutoOpen>] WaterBalloonExtensions =
    type Entity with
        member this.GetWaterBalloonCenter world : PhysicsBodyTransform option = this.Get (nameof this.WaterBalloonCenter) world
        member this.SetWaterBalloonCenter (value : PhysicsBodyTransform option) world = this.Set (nameof this.WaterBalloonCenter) value world
        member this.WaterBalloonCenter = lens (nameof this.WaterBalloonCenter) this this.GetWaterBalloonCenter this.SetWaterBalloonCenter
        member this.GetWaterBalloonContour world : PhysicsBodyTransform array = this.Get (nameof this.WaterBalloonContour) world
        member this.SetWaterBalloonContour (value : PhysicsBodyTransform array) world = this.Set (nameof this.WaterBalloonContour) value world
        member this.WaterBalloonContour = lens (nameof this.WaterBalloonContour) this this.GetWaterBalloonContour this.SetWaterBalloonContour

// A water balloon is a soft-body made of a Dynamic center body surrounded by
// a ring of 32 contour bodies linked with revolute joints (perimeter chain) and
// distance joints back to the center (shape stabilization). When the center
// body escapes the contour ring, the balloon pops and emits water particles.
type WaterBalloonDispatcher () =
    inherit Entity2dDispatcherImSim (true, false, false)

    static let internalIndex = -1
    static let contourCount = 32
    static let initialWaterContent = 12
    static let centerRadius = 8.0f
    static let centerToContourDistance = 32.0f
    static let contourRadius = MathF.PI * centerToContourDistance / single contourCount // half arc-spacing so bodies just touch
    static let baseLinearDamping = 0.2f
    static let baseAngularDamping = Constants.Physics.AngularDampingDefault

    // BodyIndex layout:
    //   Constants.Physics.InternalIndex (-1) = center body (owned directly by this dispatcher)
    //   0 .. contourCount-1                  = contour bodies
    // BodyJointIndex layout:
    //   0 .. contourCount-1                  = revolute (perimeter) joints
    //   contourCount .. 2*contourCount-1     = distance (spring) joints to center

    let computeBoundingBox (waterBalloon : Entity) world =
        waterBalloon.GetWaterBalloonContour world
        |> Array.map (fun t -> Box2 (t.BodyCenter - v2Dup contourRadius, v2Dup (contourRadius * 2.0f)))
        |> Array.reduce _.Combine
        |> fun bounds -> bounds.Box3

    // Even-odd point-in-polygon test on the contour loop in world space.
    let isPointInsideContour (point : Vector2) (contour : PhysicsBodyTransform array) =
        if contour.Length < 3 then false
        else
            let mutable inside = false
            let mutable j = contour.Length - 1
            for i in 0 .. contour.Length - 1 do
                let pi = contour[i].BodyCenter
                let pj = contour[j].BodyCenter
                if ((pi.Y > point.Y) <> (pj.Y > point.Y)) &&
                   (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y + 0.000001f) + pi.X) then
                    inside <- not inside
                j <- i
            inside

    let contourCentroid (contour : PhysicsBodyTransform array) =
        let mutable sum = v2Zero
        for t in contour do
            sum <- sum + t.BodyCenter
        sum / single contour.Length


    static let initialWaterBalloonCenter : PhysicsBodyTransform option =
        Some { BodyCenter = v2Zero
               BodyRotation = Quaternion.Identity
               BodyLinearVelocity = v2Zero
               BodyAngularVelocity = v2Zero }
    static let initialWaterBalloonContour =
        let count = 32
        [|for i in 0 .. count - 1 ->
            let angle = single i * MathF.TWO_PI / single count
            { BodyCenter = v2 (cos angle * centerToContourDistance) (sin angle * centerToContourDistance)
              BodyRotation = Quaternion.Identity
              BodyLinearVelocity = v2Zero
              BodyAngularVelocity = v2Zero }
        |]
    static member Facets =
        [typeof<WaterContainerFacet>]
    static member Properties =
        [define Entity.Visible true
         define Entity.WaterBalloonCenter initialWaterBalloonCenter
         define Entity.WaterBalloonContour initialWaterBalloonContour
         nonPersistent Entity.PhysicsMotion ManualMotion // disable automatic Position/Rotation/LinearVelocity/AngularVelocity updates for internalIndex.
         nonPersistent Entity.AwakeTimeStamp 0L
         computed Entity.BodyId (fun waterBalloon _ -> { BodySource = waterBalloon; BodyIndex = internalIndex }) None
         ]

    override this.Register (waterBalloon, world) =
        World.monitor (fun _ world ->
            if world.TimeAdvancing then
                this.UnregisterPhysics (waterBalloon, world)
                let spawn = (waterBalloon.GetPosition world).V2
                let center = initialWaterBalloonCenter |> Option.map (fun center -> { center with BodyCenter = spawn })
                let contour =
                    initialWaterBalloonContour
                    |> Array.map (fun point -> { point with BodyCenter = point.BodyCenter + spawn })
                waterBalloon.SetWaterBalloonCenter center world
                waterBalloon.SetWaterBalloonContour contour world
                waterBalloon.SetWaterContent initialWaterContent world
                this.RegisterPhysics (waterBalloon, world)
            Cascade) waterBalloon.ReviveEvent waterBalloon world

    override this.RegisterPhysics (waterBalloon, world) =


        // Create contour bodies.
        for i in 0 .. contourCount - 1 do
            let t = (waterBalloon.GetWaterBalloonContour world)[i]
            let bodyId = { BodySource = waterBalloon; BodyIndex = i }
            World.createBody2d bodyId
                { Enabled = true
                  Center = t.BodyCenter.V3
                  Rotation = t.BodyRotation
                  Scale = v3One
                  BodyShape = SphereShape { Radius = contourRadius; TransformOpt = None; PropertiesOpt = None }
                  BodyType = Dynamic
                  SleepingAllowed = true
                  Friction = Constants.Physics.FrictionDefault
                  Restitution = 0f
                  LinearVelocity = t.BodyLinearVelocity.V3
                  LinearDamping = baseLinearDamping
                  AngularVelocity = t.BodyAngularVelocity.V3
                  AngularDamping = baseAngularDamping
                  AngularFactor = v3One
                  KinematicPushLimitOpt = None
                  Substance = Mass (1f / single contourCount)
                  Gravity = GravityWorld
                  CharacterProperties = (PogoSpringCharacterProperties PogoSpringCharacterProperties.defaultProperties)
                  VehicleProperties = VehiclePropertiesAbsent
                  CollisionDetection = Continuous
                  CollisionGroup = 1 // positive group: all water balloon bodies always collide with each other
                  CollisionCategories = Physics.categorizeCollisionMask "1"
                  CollisionMask = Physics.categorizeCollisionMask Constants.Physics.CollisionWildcard
                  Sensor = false
                  BodyIndex = i } world

        // Create revolute joints linking contour bodies in a closed ring.
        for i in 0 .. contourCount - 1 do
            let next = (i + 1) % contourCount
            let bodyIdA = { BodySource = waterBalloon; BodyIndex = i }
            let bodyIdB = { BodySource = waterBalloon; BodyIndex = next }
            let bodyJointId = { BodyJointSource = waterBalloon; BodyJointIndex = i }
            World.createBodyJoint2d waterBalloon
                { BodyJoint = Box2dNetBodyJoint { CreateBodyJoint = fun _ _ a b world ->
                    let posA = B2Bodies.b2Body_GetPosition a
                    let posB = B2Bodies.b2Body_GetPosition b
                    let midX = (posA.X + posB.X) * 0.5f
                    let midY = (posA.Y + posB.Y) * 0.5f
                    let mutable jointDef = B2Joints.b2DefaultRevoluteJointDef ()
                    jointDef.``base``.bodyIdA <- a
                    jointDef.``base``.bodyIdB <- b
                    jointDef.``base``.localFrameA.p <- new _ (midX - posA.X, midY - posA.Y)
                    jointDef.``base``.localFrameB.p <- new _ (midX - posB.X, midY - posB.Y)
                    B2Joints.b2CreateRevoluteJoint (world, &jointDef) }
                  BodyJointTarget = bodyIdA
                  BodyJointTarget2 = bodyIdB
                  BodyJointEnabled = true
                  BreakingPointOpt = None
                  Broken = false
                  CollideConnected = true
                  BodyJointIndex = bodyJointId.BodyJointIndex } world

        match waterBalloon.GetWaterBalloonCenter world with
        | Some center ->

            // Create the center body.
            let centerBodyId = { BodySource = waterBalloon; BodyIndex = internalIndex }
            World.createBody2d centerBodyId
                { Enabled = true
                  Center = center.BodyCenter.V3
                  Rotation = center.BodyRotation
                  Scale = v3One
                  BodyShape = SphereShape { Radius = centerRadius; TransformOpt = None; PropertiesOpt = None }
                  BodyType = Dynamic
                  SleepingAllowed = true
                  Friction = Constants.Physics.FrictionDefault
                  Restitution = 0.333f
                  LinearVelocity = center.BodyLinearVelocity.V3
                  LinearDamping = baseLinearDamping
                  AngularVelocity = center.BodyAngularVelocity.V3
                  AngularDamping = baseAngularDamping
                  AngularFactor = v3One
                  KinematicPushLimitOpt = None
                  Substance = Mass 1f
                  Gravity = GravityWorld
                  CharacterProperties = (PogoSpringCharacterProperties PogoSpringCharacterProperties.defaultProperties)
                  VehicleProperties = VehiclePropertiesAbsent
                  CollisionDetection = Continuous
                  CollisionGroup = 1 // positive group: all water balloon bodies always collide with each other
                  CollisionCategories = Physics.categorizeCollisionMask "1"
                  CollisionMask = Physics.categorizeCollisionMask Constants.Physics.CollisionWildcard
                  Sensor = false
                  BodyIndex = internalIndex } world

            // Create distance (spring) joints from each contour body back to the center.
            for i in 0 .. contourCount - 1 do
                let contourBodyId = { BodySource = waterBalloon; BodyIndex = i }
                let bodyJointId = { BodyJointSource = waterBalloon; BodyJointIndex = contourCount + i }
                World.createBodyJoint2d waterBalloon
                    { BodyJoint = Box2dNetBodyJoint { CreateBodyJoint = fun toPhysics _ a b world ->
                        let mutable jointDef = B2Joints.b2DefaultDistanceJointDef ()
                        jointDef.``base``.bodyIdA <- a
                        jointDef.``base``.bodyIdB <- b
                        jointDef.length <- toPhysics centerToContourDistance
                        jointDef.enableSpring <- true
                        jointDef.hertz <- 3f
                        jointDef.dampingRatio <- 1f
                        B2Joints.b2CreateDistanceJoint (world, &jointDef) }
                      BodyJointTarget = centerBodyId
                      BodyJointTarget2 = contourBodyId
                      BodyJointEnabled = true
                      BreakingPointOpt = None
                      Broken = false
                      CollideConnected = false
                      BodyJointIndex = bodyJointId.BodyJointIndex } world
        | None -> ()

    override this.UnregisterPhysics (waterBalloon, world) =

        // destroy revolute joints (perimeter)
        for i in 0 .. contourCount - 1 do
            let next = (i + 1) % contourCount
            let bodyIdA = { BodySource = waterBalloon; BodyIndex = i }
            let bodyIdB = { BodySource = waterBalloon; BodyIndex = next }
            let bodyJointId = { BodyJointSource = waterBalloon; BodyJointIndex = i }
            World.destroyBodyJoint2d bodyIdA bodyIdB bodyJointId world
    
        if waterBalloon.GetWaterBalloonCenter world |> Option.isSome then
            // destroy distance joints (center-to-contour)
            let centerBodyId = { BodySource = waterBalloon; BodyIndex = internalIndex }
            for i in 0 .. contourCount - 1 do
                let contourBodyId = { BodySource = waterBalloon; BodyIndex = i }
                let bodyJointId = { BodyJointSource = waterBalloon; BodyJointIndex = contourCount + i }
                World.destroyBodyJoint2d centerBodyId contourBodyId bodyJointId world

            // destroy center body
            World.destroyBody2d centerBodyId world

        // destroy contour bodies
        for i in 0 .. contourCount - 1 do
            World.destroyBody2d { BodySource = waterBalloon; BodyIndex = i } world

    override this.Process (waterBalloon, world) =

        if world.ContextInitializing then
            let spawnPos = waterBalloon.GetPosition world
            waterBalloon.SetWaterContent initialWaterContent world
            match waterBalloon.GetWaterBalloonCenter world with
            | Some center ->
                let center' = { center with BodyCenter = spawnPos.V2 }
                waterBalloon.SetWaterBalloonCenter (Some center') world
                let centerBodyId = { BodySource = waterBalloon; BodyIndex = internalIndex }
                World.setBodyCenter spawnPos centerBodyId world
            | None -> ()
            let contour = Array.copy (waterBalloon.GetWaterBalloonContour world)
            for i in 0 .. contour.Length - 1 do
                let t = contour[i]
                let t' = { t with BodyCenter = t.BodyCenter + spawnPos.V2 }
                contour[i] <- t'
                World.setBodyCenter t'.BodyCenter.V3 { BodySource = waterBalloon; BodyIndex = i } world
            waterBalloon.SetWaterBalloonContour contour world

        // Track body transforms from physics events.
        let contour =
            let existing = waterBalloon.GetWaterBalloonContour world
            if existing.Length = contourCount then Array.copy existing
            else Array.zeroCreate contourCount
        let inflated = waterBalloon.GetWaterBalloonCenter world |> Option.isSome
        for event in World.doSubscriptionToBodyEvents "WaterBalloonBodyEvents" waterBalloon world do
            match event with
            | BodyTransformData transform when transform.BodyId.BodyIndex = internalIndex && inflated ->
                waterBalloon.SetWaterBalloonCenter (Some
                    { BodyCenter = transform.BodyCenter.V2
                      BodyRotation = transform.BodyRotation
                      BodyLinearVelocity = transform.BodyLinearVelocity.V2
                      BodyAngularVelocity = transform.BodyAngularVelocity.V2 }) world
            | BodyTransformData transform when transform.BodyId.BodyIndex >= 0 && transform.BodyId.BodyIndex < contourCount ->
                contour[transform.BodyId.BodyIndex] <-
                    { BodyCenter = transform.BodyCenter.V2
                      BodyRotation = transform.BodyRotation
                      BodyLinearVelocity = transform.BodyLinearVelocity.V2
                      BodyAngularVelocity = transform.BodyAngularVelocity.V2 }
            | _ -> ()
        waterBalloon.SetWaterBalloonContour contour world

        // Update perimeter for rendering presence etc.
        let contourBounds = computeBoundingBox waterBalloon world
        waterBalloon.SetPerimeter contourBounds world

        // Pop check: if the center body escapes the contour ring, emit water particles and disable the center.
        if world.TimeAdvancing && contour.Length = contourCount then
            match waterBalloon.GetWaterBalloonCenter world with
            | Some center ->
                if not (isPointInsideContour center.BodyCenter contour) then
                    // Emit water particles from the centroid.
                    let centroid = contourCentroid contour
                    match tryResolve (waterBalloon.GetWorldFluidEmitter world) waterBalloon with
                    | Some emitter ->
                        let particleCount = waterBalloon.GetWaterContent world |> max 1
                        World.emitFluidParticles
                            (WaterParticles.burst "Water" centroid v2Zero 1.5f particleCount)
                            (emitter.GetFluidEmitterId world) world
                    | None -> ()
                    this.UnregisterPhysics (waterBalloon, world)
                    waterBalloon.SetWaterBalloonCenter None world
                    this.RegisterPhysics (waterBalloon, world)
                    waterBalloon.SetWaterContent 0 world
            | None -> ()

    override _.Render (_, waterBalloon, world) =
        let contour = waterBalloon.GetWaterBalloonContour world
        if contour.Length >= 3 then
            let position = waterBalloon.GetPosition world
            let size =
                let s = (waterBalloon.GetSize world).V2
                v2 (max 0.0001f s.X) (max 0.0001f s.Y)
            // Compute base polygon from body centers in world space.
            let worldPoints = contour |> Array.map (fun t -> t.BodyCenter)
            // Force CCW winding so edge normals expand outward correctly.
            let worldPoints =
                let mutable signedArea2x = 0.0f
                for i in 0 .. worldPoints.Length - 1 do
                    let p = worldPoints[i]
                    let q = worldPoints[(i + 1) % worldPoints.Length]
                    signedArea2x <- signedArea2x + (p.X * q.Y - q.X * p.Y)
                if signedArea2x < 0.0f then Array.rev worldPoints else worldPoints
            // Expand polygon outward by contourRadius using edge normals (handles concave contours).
            let n = worldPoints.Length
            let worldPoints =
                Array.init n (fun i ->
                    let iPrev = (i - 1 + n) % n
                    let iNext = (i + 1) % n
                    let dPrev = worldPoints[i] - worldPoints[iPrev]
                    let dNext = worldPoints[iNext] - worldPoints[i]
                    let lenPrev = dPrev.Magnitude
                    let lenNext = dNext.Magnitude
                    // Outward normals for CCW polygon: right normal of edge direction.
                    let nPrev = if lenPrev > 0.0001f then v2 (dPrev.Y / lenPrev) (-dPrev.X / lenPrev) else v2Zero
                    let nNext = if lenNext > 0.0001f then v2 (dNext.Y / lenNext) (-dNext.X / lenNext) else v2Zero
                    let vn = nPrev + nNext
                    let vnLen = vn.Magnitude
                    let outward = if vnLen > 0.0001f then vn * (contourRadius / vnLen) else v2Zero
                    worldPoints[i] + outward)
            // Normalize to entity-local space for tessellation.
            let points = worldPoints |> Array.map (fun p -> (p - position.V2) / size)
            let commands = Array.zeroCreate<ContourCommand> (points.Length + 1)
            commands[0] <- MoveTo points[0]
            for i in 1 .. points.Length - 1 do
                commands[i] <- LineTo points[i]
            commands[points.Length] <- CloseContour
            let tessellation =
                Contour.make
                    (ContourFill.ofColorWinding Color.Red ContourWinding.NonZero)
                    ContourStroke.none
                    commands
                    size
            let mutable transform = waterBalloon.GetTransform world
            transform.Rotation <- Quaternion.Identity
            transform.Scale <- v3One
            World.renderContour
                { Transform = transform
                  ClipOpt = ValueNone
                  Contour = tessellation } world