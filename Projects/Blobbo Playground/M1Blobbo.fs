namespace BlobboPlayground
open System
open System.Numerics
open Box2D.NET
open Prime
open Nu

[<RequireQualifiedAccess>]
module M1BodyModel =
    let CenterBodyIndex = -1
    let RingNodeCount = 12
    let RingRadius = 36.0f
    let RingNodeRadius = 9.0f
    let RingCenterRadius = 13.0f
    let StableHullRadius = 34.0f

    /// All comparison bodies use Density 1. Match the backend's sphere-radius conversion exactly.
    let bodyMass candidate bodyIndex =
        let radius =
            match candidate with
            | LegacyGraph ->
                if bodyIndex = CenterBodyIndex then BlobboBodyModel.CenterRadius else BlobboBodyModel.ContourRadius
            | SimplifiedRing -> if bodyIndex = CenterBodyIndex then RingCenterRadius else RingNodeRadius
            | StableHull -> StableHullRadius
        let physicsRadius = max Constants.Physics.Collision2dLinearSlop (radius / Constants.Engine.Meter2d)
        MathF.PI * physicsRadius * physicsRadius

    let bodyIds candidate (entity : Entity) =
        match candidate with
        | LegacyGraph ->
            [|yield { BodySource = entity; BodyIndex = CenterBodyIndex }
              for index in 0 .. 31 do
                  yield { BodySource = entity; BodyIndex = index }|]
        | SimplifiedRing ->
            [|yield { BodySource = entity; BodyIndex = CenterBodyIndex }
              for index in 0 .. RingNodeCount - 1 do
                  yield { BodySource = entity; BodyIndex = index }|]
        | StableHull ->
            [|{ BodySource = entity; BodyIndex = CenterBodyIndex }|]

    let initialRing =
        [|for index in 0 .. RingNodeCount - 1 do
              let angle = single index * MathF.TWO_PI / single RingNodeCount
              yield
                  { BodyCenter = v2 (cos angle * RingRadius) (sin angle * RingRadius)
                    BodyRotation = Quaternion.Identity
                    BodyLinearVelocity = v2Zero
                    BodyAngularVelocity = v2Zero }|]

[<RequireQualifiedAccess>]
module M1BodyControl =

    /// Distribute world-unit acceleration and delta-v by constituent mass, preserving contact velocity.
    let apply configuration candidate subject (output : M1ControlOutput) world =
        for bodyId in M1BodyModel.bodyIds candidate subject do
            if World.getBodyExists bodyId world then
                let mass = M1BodyModel.bodyMass candidate bodyId.BodyIndex
                if output.Acceleration.LengthSquared () > 0.0f then
                    World.applyBodyForce (output.Acceleration * mass).V3 None bodyId world
                let velocity = (World.getBodyLinearVelocity bodyId world).V2
                let releaseVelocity = M1Control.releaseVelocity configuration output.VelocityChange velocity
                if output.Released then
                    World.applyBodyLinearImpulse ((releaseVelocity - velocity) * mass).V3 None bodyId world
                elif Vector2.DistanceSquared (velocity, releaseVelocity) > 0.001f then
                    World.setBodyLinearVelocity releaseVelocity.V3 bodyId world

module [<AutoOpen>] M1BlobboExtensions =
    type Entity with
        member this.GetM1BodyCandidate world : M1BodyCandidate = this.Get (nameof this.M1BodyCandidate) world
        member this.SetM1BodyCandidate (value : M1BodyCandidate) world = this.Set (nameof this.M1BodyCandidate) value world
        member this.M1BodyCandidate = lens (nameof this.M1BodyCandidate) this this.GetM1BodyCandidate this.SetM1BodyCandidate
        member this.GetM1AppliedBodyCandidate world : M1BodyCandidate = this.Get (nameof this.M1AppliedBodyCandidate) world
        member this.SetM1AppliedBodyCandidate (value : M1BodyCandidate) world = this.Set (nameof this.M1AppliedBodyCandidate) value world
        member this.M1AppliedBodyCandidate = lens (nameof this.M1AppliedBodyCandidate) this this.GetM1AppliedBodyCandidate this.SetM1AppliedBodyCandidate
        member this.GetM1FixtureVersion world : int = this.Get (nameof this.M1FixtureVersion) world
        member this.SetM1FixtureVersion (value : int) world = this.Set (nameof this.M1FixtureVersion) value world
        member this.M1FixtureVersion = lens (nameof this.M1FixtureVersion) this this.GetM1FixtureVersion this.SetM1FixtureVersion
        member this.GetM1AppliedFixtureVersion world : int = this.Get (nameof this.M1AppliedFixtureVersion) world
        member this.SetM1AppliedFixtureVersion (value : int) world = this.Set (nameof this.M1AppliedFixtureVersion) value world
        member this.M1AppliedFixtureVersion = lens (nameof this.M1AppliedFixtureVersion) this this.GetM1AppliedFixtureVersion this.SetM1AppliedFixtureVersion
        member this.GetM1AppliedPosition world : Vector3 = this.Get (nameof this.M1AppliedPosition) world
        member this.SetM1AppliedPosition (value : Vector3) world = this.Set (nameof this.M1AppliedPosition) value world
        member this.M1AppliedPosition = lens (nameof this.M1AppliedPosition) this this.GetM1AppliedPosition this.SetM1AppliedPosition
        member this.GetM1BodyCenter world : PhysicsBodyTransform = this.Get (nameof this.M1BodyCenter) world
        member this.SetM1BodyCenter (value : PhysicsBodyTransform) world = this.Set (nameof this.M1BodyCenter) value world
        member this.M1BodyCenter = lens (nameof this.M1BodyCenter) this this.GetM1BodyCenter this.SetM1BodyCenter
        member this.GetM1BodyContour world : PhysicsBodyTransform array = this.Get (nameof this.M1BodyContour) world
        member this.SetM1BodyContour (value : PhysicsBodyTransform array) world = this.Set (nameof this.M1BodyContour) value world
        member this.M1BodyContour = lens (nameof this.M1BodyContour) this this.GetM1BodyContour this.SetM1BodyContour
        member this.GetM1VisualPull world : Vector2 = this.Get (nameof this.M1VisualPull) world
        member this.SetM1VisualPull (value : Vector2) world = this.Set (nameof this.M1VisualPull) value world
        member this.M1VisualPull = lens (nameof this.M1VisualPull) this this.GetM1VisualPull this.SetM1VisualPull

[<RequireQualifiedAccess>]
module M1BlobboVisual =

    let bodyPoints candidate (entity : Entity) world =
        match candidate with
        | LegacyGraph -> BlobboBodyModel.renderedPoints (entity.GetBlobboContour world)
        | SimplifiedRing ->
            let center = entity.GetM1BodyCenter world
            entity.GetM1BodyContour world
            |> Array.map (fun point ->
                let radial = point.BodyCenter - center.BodyCenter
                let length = radial.Length ()
                point.BodyCenter + if length > 0.0001f then radial * (M1BodyModel.RingNodeRadius / length) else v2Zero)
        | StableHull ->
            let center = entity.GetM1BodyCenter world
            let pull = entity.GetM1VisualPull world
            let deformationVector = pull * 0.012f + center.BodyLinearVelocity * 0.025f
            let deformation = min 0.24f (deformationVector.Length () / M1BodyModel.StableHullRadius)
            let direction = if deformationVector.LengthSquared () > 0.0001f then atan2 deformationVector.Y deformationVector.X else 0.0f
            let phase = single (world.UpdateTime % 360L) * 0.055f
            Array.init 24 (fun index ->
                let angle = single index * MathF.TWO_PI / 24.0f
                let directional = cos (2.0f * (angle - direction)) * deformation
                let wobble = sin (3.0f * angle + phase) * 0.025f
                let radius = M1BodyModel.StableHullRadius * (1.0f + directional + wobble)
                center.BodyCenter + v2 (cos angle * radius) (sin angle * radius))

    let sprite (position : Vector2) (size : Vector2) (elevation : single) (colorValue : Color) world =
        let image = Assets.Default.Ball
        let insetOpt : Box2 voption = ValueNone
        let clipOpt : Box2 voption = ValueNone
        let emission = colorZero
        let mutable transform =
            Transform.makeIntuitive false position.V3 v3One v3Zero size.V3 v3Zero elevation
        World.renderLayeredSpriteFast
            (transform.Elevation, transform.Horizon, image, &transform, &insetOpt, &clipOpt,
             image, &colorValue, Transparent, &emission, Unflipped, world)

    let contourCenter fallback (contour : PhysicsBodyTransform array) =
        if Array.isEmpty contour then fallback
        else
            let total =
                Array.fold
                    (fun total (transform : PhysicsBodyTransform) -> total + transform.BodyCenter)
                    v2Zero
                    contour
            total / single contour.Length

    let face (center : Vector2) (rotation : Quaternion) (velocity : Vector2) elevation world =
        let lookSource =
            if World.isMouseButtonDown MouseLeft world then
                World.getMousePosition2dWorld false world - center
            else velocity
        let look =
            if lookSource.LengthSquared () > 0.001f then Vector2.Normalize lookSource * 2.5f
            else v2Zero
        for index in 0 .. 1 do
            let eyeOffset = v2 (if index = 0 then -10.0f else 10.0f) 6.0f
            let orientedEyeOffset = Vector2.Transform (eyeOffset, rotation)
            let eye = center + orientedEyeOffset
            sprite eye (v2Dup 12.0f) (elevation + 1.0f) Color.White world
            sprite (eye + look) (v2Dup 5.0f) (elevation + 1.1f) (color 0.03f 0.05f 0.12f 1.0f) world

/// The legacy body keeps its original physics and contour rendering while receiving the shared M1 face.
type M1LegacyBlobboDispatcher () =
    inherit BlobboDispatcher ()

    override _.Render (renderPass, entity, world) =
        base.Render (renderPass, entity, world)
        let center = entity.GetBlobboCenter world
        let faceCenter = M1BlobboVisual.contourCenter center.BodyCenter (entity.GetBlobboContour world)
        M1BlobboVisual.face
            faceCenter
            center.BodyRotation
            center.BodyLinearVelocity
            (entity.GetElevation world)
            world

/// Experimental low-constraint Blobbo representations. LegacyGraph remains owned by BlobboDispatcher.
type M1BlobboDispatcher () =
    inherit Entity2dDispatcherImSim (true, false, false)

    static let initialCenter =
        { BodyCenter = v2Zero
          BodyRotation = Quaternion.Identity
          BodyLinearVelocity = v2Zero
          BodyAngularVelocity = v2Zero }

    static member Properties =
        [define Entity.M1BodyCandidate SimplifiedRing
         nonPersistent Entity.M1AppliedBodyCandidate SimplifiedRing
         nonPersistent Entity.M1FixtureVersion 0
         nonPersistent Entity.M1AppliedFixtureVersion 0
         nonPersistent Entity.M1AppliedPosition v3Zero
         define Entity.M1BodyCenter initialCenter
         define Entity.M1BodyContour M1BodyModel.initialRing
         define Entity.M1VisualPull v2Zero
         define Entity.AwakeTimeStamp 0
         nonPersistent Entity.PhysicsMotion ManualMotion]

    static member private MakeBodyProperties
        (radius : single)
        (bodyIndex : int)
        (collisionGroup : int)
        (transform : PhysicsBodyTransform) : BodyProperties =
        { Enabled = true
          Center = transform.BodyCenter.V3
          Rotation = transform.BodyRotation
          Scale = v3One
          BodyShape = SphereShape { Radius = radius; TransformOpt = None; PropertiesOpt = None }
          BodyType = Dynamic
          SleepingAllowed = true
          Friction = 0.55f
          Restitution = 0.18f
          LinearVelocity = transform.BodyLinearVelocity.V3
          LinearDamping = 0.22f
          AngularVelocity = transform.BodyAngularVelocity.V3
          AngularDamping = 0.35f
          AngularFactor = v3One
          KinematicPushLimitOpt = None
          Substance = Density 1.0f
          Gravity = GravityWorld
          CharacterProperties = PogoSpringCharacterProperties PogoSpringCharacterProperties.defaultProperties
          VehicleProperties = VehiclePropertiesAbsent
          CollisionDetection = Continuous
          CollisionGroup = collisionGroup
          CollisionCategories = Physics.categorizeCollisionMask "1"
          CollisionMask = Physics.categorizeCollisionMask Constants.Physics.CollisionWildcard
          Sensor = false
          BodyIndex = bodyIndex }

    static member private CreateDistanceJoint bodyA bodyB distance index (entity : Entity) world =
        let properties =
            { BodyJoint =
                Box2dNetBodyJoint
                    { CreateBodyJoint = fun toPhysics _ a b physicsWorld ->
                        let mutable definition = B2Joints.b2DefaultDistanceJointDef ()
                        definition.``base``.bodyIdA <- a
                        definition.``base``.bodyIdB <- b
                        definition.length <- toPhysics distance
                        definition.enableSpring <- true
                        definition.hertz <- 7.0f
                        definition.dampingRatio <- 0.78f
                        B2Joints.b2CreateDistanceJoint (physicsWorld, &definition) }
              BodyJointTarget = bodyA
              BodyJointTarget2 = bodyB
              BodyJointEnabled = true
              BreakingPointOpt = None
              Broken = false
              CollideConnected = false
              BodyJointIndex = index }
        World.createBodyJoint2d entity properties world

    override _.RegisterPhysics (entity, world) =
        let candidate = entity.GetM1BodyCandidate world
        let spawn = entity.GetPosition world
        let center = { initialCenter with BodyCenter = spawn.V2 }
        entity.SetM1BodyCenter center world
        match candidate with
        | StableHull ->
            let bodyId = { BodySource = entity; BodyIndex = M1BodyModel.CenterBodyIndex }
            World.createBody2d bodyId (M1BlobboDispatcher.MakeBodyProperties M1BodyModel.StableHullRadius M1BodyModel.CenterBodyIndex 0 center) world
        | SimplifiedRing ->
            let contour =
                M1BodyModel.initialRing
                |> Array.map (fun point -> { point with BodyCenter = point.BodyCenter + spawn.V2 })
            entity.SetM1BodyContour contour world
            for index in 0 .. contour.Length - 1 do
                let bodyId = { BodySource = entity; BodyIndex = index }
                World.createBody2d bodyId (M1BlobboDispatcher.MakeBodyProperties M1BodyModel.RingNodeRadius index (-117) contour[index]) world
            let centerBodyId = { BodySource = entity; BodyIndex = M1BodyModel.CenterBodyIndex }
            World.createBody2d centerBodyId (M1BlobboDispatcher.MakeBodyProperties M1BodyModel.RingCenterRadius M1BodyModel.CenterBodyIndex (-117) center) world
            for index in 0 .. contour.Length - 1 do
                let next = (index + 1) % contour.Length
                M1BlobboDispatcher.CreateDistanceJoint
                    { BodySource = entity; BodyIndex = index }
                    { BodySource = entity; BodyIndex = M1BodyModel.CenterBodyIndex }
                    M1BodyModel.RingRadius
                    index
                    entity
                    world
                let edgeDistance = Vector2.Distance (M1BodyModel.initialRing[index].BodyCenter, M1BodyModel.initialRing[next].BodyCenter)
                M1BlobboDispatcher.CreateDistanceJoint
                    { BodySource = entity; BodyIndex = index }
                    { BodySource = entity; BodyIndex = next }
                    edgeDistance
                    (M1BodyModel.RingNodeCount + index)
                    entity
                    world
        | LegacyGraph -> ()
        entity.SetM1AppliedBodyCandidate candidate world
        entity.SetM1AppliedFixtureVersion (entity.GetM1FixtureVersion world) world
        entity.SetM1AppliedPosition spawn world

    override _.UnregisterPhysics (entity, world) =
        match entity.GetM1AppliedBodyCandidate world with
        | StableHull ->
            World.destroyBody2d { BodySource = entity; BodyIndex = M1BodyModel.CenterBodyIndex } world
        | SimplifiedRing ->
            for index in 0 .. M1BodyModel.RingNodeCount - 1 do
                let next = (index + 1) % M1BodyModel.RingNodeCount
                World.destroyBodyJoint2d
                    { BodySource = entity; BodyIndex = index }
                    { BodySource = entity; BodyIndex = M1BodyModel.CenterBodyIndex }
                    { BodyJointSource = entity; BodyJointIndex = index }
                    world
                World.destroyBodyJoint2d
                    { BodySource = entity; BodyIndex = index }
                    { BodySource = entity; BodyIndex = next }
                    { BodyJointSource = entity; BodyJointIndex = M1BodyModel.RingNodeCount + index }
                    world
            World.destroyBody2d { BodySource = entity; BodyIndex = M1BodyModel.CenterBodyIndex } world
            World.destroyBodies2d
                [for index in 0 .. M1BodyModel.RingNodeCount - 1 do
                     yield { BodySource = entity; BodyIndex = index }]
                world
        | LegacyGraph -> ()

    override this.Process (entity, world) =
        let candidate = entity.GetM1BodyCandidate world
        if candidate <> entity.GetM1AppliedBodyCandidate world ||
           entity.GetM1FixtureVersion world <> entity.GetM1AppliedFixtureVersion world ||
           entity.GetPosition world <> entity.GetM1AppliedPosition world then
            this.UnregisterPhysics (entity, world)
            this.RegisterPhysics (entity, world)
        let contour = Array.copy (entity.GetM1BodyContour world)
        for event in World.doSubscriptionToBodyEvents "M1BodyEvents" entity world do
            match event with
            | BodyTransformData transform when transform.BodyId.BodyIndex = M1BodyModel.CenterBodyIndex ->
                entity.SetM1BodyCenter
                    { BodyCenter = transform.BodyCenter.V2
                      BodyRotation = transform.BodyRotation
                      BodyLinearVelocity = transform.BodyLinearVelocity.V2
                      BodyAngularVelocity = transform.BodyAngularVelocity.V2 }
                    world
            | BodyTransformData transform when candidate = SimplifiedRing && transform.BodyId.BodyIndex >= 0 && transform.BodyId.BodyIndex < contour.Length ->
                contour[transform.BodyId.BodyIndex] <-
                    { BodyCenter = transform.BodyCenter.V2
                      BodyRotation = transform.BodyRotation
                      BodyLinearVelocity = transform.BodyLinearVelocity.V2
                      BodyAngularVelocity = transform.BodyAngularVelocity.V2 }
            | _ -> ()
        if candidate = SimplifiedRing then entity.SetM1BodyContour contour world

        let center = entity.GetM1BodyCenter world
        let perimeter =
            match candidate with
            | StableHull ->
                box2 (center.BodyCenter - v2Dup M1BodyModel.StableHullRadius) (v2Dup (M1BodyModel.StableHullRadius * 2.0f))
            | SimplifiedRing ->
                contour
                |> Array.fold
                    (fun bounds point ->
                        bounds.Combine
                            (box2
                                (point.BodyCenter - v2Dup M1BodyModel.RingNodeRadius)
                                (v2Dup (M1BodyModel.RingNodeRadius * 2.0f))))
                    (box2
                        (center.BodyCenter - v2Dup M1BodyModel.RingCenterRadius)
                        (v2Dup (M1BodyModel.RingCenterRadius * 2.0f)))
            | LegacyGraph -> box2Zero
        entity.SetPerimeter perimeter.Box3 world
        // Publishing the physical perimeter also changes Position; it is not an external fixture reset.
        entity.SetM1AppliedPosition (entity.GetPosition world) world

    override _.Render (_, entity, world) =
        let candidate = entity.GetM1BodyCandidate world
        let center = entity.GetM1BodyCenter world
        let position = entity.GetPosition world
        let size =
            let size = (entity.GetSize world).V2
            v2 (max 0.0001f size.X) (max 0.0001f size.Y)
        let points = M1BlobboVisual.bodyPoints candidate entity world

        let renderFilled elevation colorValue scale =
            if points.Length >= 3 then
                let scaled = points |> Array.map (fun point -> center.BodyCenter + (point - center.BodyCenter) * scale)
                let local = scaled |> Array.map (fun point -> (point - position.V2) / size)
                let commands = Array.zeroCreate<ContourCommand> (local.Length + 1)
                commands[0] <- MoveTo local[0]
                for index in 1 .. local.Length - 1 do commands[index] <- LineTo local[index]
                commands[local.Length] <- CloseContour
                let contour =
                    Contour.make
                        (ContourFill.ofColorWinding colorValue ContourWinding.NonZero)
                        ContourStroke.none
                        commands
                        size
                let mutable transform = entity.GetTransform world
                transform.Rotation <- Quaternion.Identity
                transform.Scale <- v3One
                transform.Elevation <- elevation
                World.renderContour { Transform = transform; ClipOpt = ValueNone; Contour = contour } world

        match candidate with
        | SimplifiedRing ->
            renderFilled (entity.GetElevation world - 0.01f) (color 0.12f 0.95f 0.86f 0.2f) 1.18f
            renderFilled (entity.GetElevation world) (color 0.08f 0.78f 0.7f 1.0f) 1.0f
        | StableHull ->
            renderFilled (entity.GetElevation world - 0.01f) (color 1.0f 0.38f 0.5f 0.22f) 1.16f
            renderFilled (entity.GetElevation world) (color 0.98f 0.3f 0.46f 1.0f) 1.0f
        | LegacyGraph -> ()

        if candidate = SimplifiedRing then
            for point in entity.GetM1BodyContour world do
                M1BlobboVisual.sprite
                    point.BodyCenter
                    (v2Dup 5.0f)
                    (entity.GetElevation world + 0.2f)
                    (color 0.65f 1.0f 0.92f 0.75f)
                    world

        let faceCenter =
            match candidate with
            | SimplifiedRing -> M1BlobboVisual.contourCenter center.BodyCenter (entity.GetM1BodyContour world)
            | StableHull -> center.BodyCenter
            | LegacyGraph -> center.BodyCenter
        M1BlobboVisual.face
            faceCenter
            center.BodyRotation
            center.BodyLinearVelocity
            (entity.GetElevation world)
            world