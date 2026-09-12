namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu
open BlobboPlayground

[<AutoOpen>]
module SquareExtensions =
    type Entity with
        member this.GetEyeSocketTessellation world : Contour = this.Get (nameof Entity.EyeSocketTessellation) world
        member this.SetEyeSocketTessellation (value : Contour) world = this.Set (nameof Entity.EyeSocketTessellation) value world
        member this.EyeSocketTessellation = lens (nameof Entity.EyeSocketTessellation) this this.GetEyeSocketTessellation this.SetEyeSocketTessellation

type SquareDispatcher () =
    inherit Entity2dDispatcher (true, false, false)
    static let eyeSocketLeftX = 0.05f
    static let eyeSocketRightX = 0.45f
    static let eyeSocketTopY = 0.25f
    static let eyeSocketBottomY = -0.25f
    static let eyeSocketCenterX = (eyeSocketLeftX + eyeSocketRightX) * 0.5f
    static let eyeDirections = [-1; 1]
    
    static let updateEyeSocketTessellation (entity : Entity) world =
        let eyeSocketTessellation =
            Contour.make
                (ContourFill.ofColor Color.White)
                (ContourStroke.ofColorThickness Color.Black 1f)
                (seq {
                    for direction in eyeDirections do
                        MoveTo (v2 (single direction * eyeSocketLeftX) eyeSocketTopY)
                        LineTo (v2 (single direction * eyeSocketRightX) eyeSocketTopY)
                        LineTo (v2 (single direction * eyeSocketRightX) eyeSocketBottomY)
                        LineTo (v2 (single direction * eyeSocketLeftX) eyeSocketBottomY)
                        CloseContour })
                (entity.GetSize world * entity.GetScale world).V2
        entity.SetEyeSocketTessellation eyeSocketTessellation world
        Cascade

    static let getEyeTessellation (entity : Entity) world =
        let linearVelocity = (entity.GetLinearVelocity world).V2
        let eyeDirection =
            if linearVelocity.LengthSquared () > 0.0001f
            then Vector2.Normalize linearVelocity
            else v2Zero
        let clamp value minValue maxValue = value |> max minValue |> min maxValue
        let eyeOffset = v2 (eyeDirection.X * 0.12f) (eyeDirection.Y * 0.18f)
        Contour.make
            (ContourFill.ofColor Color.Black)
            (ContourStroke.none)
            (seq {
              for direction in eyeDirections do
                  let socketX1 = single direction * eyeSocketLeftX
                  let socketX2 = single direction * eyeSocketRightX
                  let socketMinX = min socketX1 socketX2
                  let socketMaxX = max socketX1 socketX2
                  let socketMinY = eyeSocketBottomY
                  let socketMaxY = eyeSocketTopY
                  let margin = 0.00f
                  let halfExtent = 0.15f
                  let unclampedCenter = v2 (single direction * eyeSocketCenterX + eyeOffset.X) eyeOffset.Y
                  let center =
                      v2
                          (clamp unclampedCenter.X (socketMinX + halfExtent + margin) (socketMaxX - halfExtent - margin))
                          (clamp unclampedCenter.Y (socketMinY + halfExtent + margin) (socketMaxY - halfExtent - margin))
                  MoveTo (v2 (center.X - halfExtent) (center.Y - halfExtent))
                  LineTo (v2 (center.X + halfExtent) (center.Y - halfExtent))
                  LineTo (v2 (center.X + halfExtent) (center.Y + halfExtent))
                  LineTo (v2 (center.X - halfExtent) (center.Y + halfExtent))
                  CloseContour })
            (entity.GetSize world * entity.GetScale world).V2

    static let updateBasicParticleSeed (entity : Entity) world =
        let fillColor = entity.GetFillColor world
        let size = entity.GetSize world * entity.GetScale world
        let seed = entity.GetBasicParticleSeed world
        entity.SetBasicParticleSeed { seed with Color = fillColor; Size = size } world

    static let updateTrailEmitterTransform (entity : Entity) world =
        let position = entity.GetPosition world
        let angles = entity.GetAngles world
        let elevation = entity.GetElevation world |> Single.BitDecrement
        let particleSystem = entity.GetParticleSystem world
        match Map.tryFind typeof<Particles.BasicStaticSpriteEmitter>.Name particleSystem.Emitters with
        | Some (:? Particles.BasicStaticSpriteEmitter as emitter) ->
            let body = emitter.Body
            if body.Position <> position || body.Angles <> angles || emitter.Elevation <> elevation then
                entity.SetParticleSystem
                    { particleSystem with
                        Emitters =
                            Map.add typeof<Particles.BasicStaticSpriteEmitter>.Name
                                ({ emitter with
                                    Body = { body with Position = position; Angles = angles }
                                    Elevation = elevation } :> Particles.Emitter) particleSystem.Emitters } world
        | _ -> ()

    static member Facets =
        [typeof<RigidBodyFacet>
         typeof<RectangleContour2dFacet>
         typeof<BasicStaticSpriteEmitterFacet>]

    static member Properties =
        [define Entity.MountOpt None
         define Entity.BodyType Dynamic
         define Entity.BodyShape (BoxShape { Size = v3One; TransformOpt = None; PropertiesOpt = None })
         define Entity.FillColor Color.Green
         define Entity.StrokeColor Color.Black
         define Entity.StrokeThickness 2.0f
         define Entity.Gravity GravityIgnore
         define Entity.AngularFactor v3Zero // No rotation
         define Entity.Friction 0f
         define Entity.Restitution 1f // Perfectly elastic collisions
         define Entity.EmitterImage Assets.Default.White
         define Entity.EmitterStyle "SquareTrail"
         define Entity.EmitterLifeTimeOpt GameTime.zero
         define Entity.ParticleLifeTimeMaxOpt (GameTime.ofSeconds 1.0)
         define Entity.ParticleRate (match Constants.GameTime.DesiredFrameRate with StaticFrameRate _ -> 1.0f | DynamicFrameRate _ -> 60.0f)
         define Entity.ParticleMax 60
         nonPersistent Entity.EyeSocketTessellation Contour.empty]

    override this.Register (entity, world) =
        for propertyName in
            [nameof Entity.Size; nameof Entity.Scale] do
            World.monitor (constant $ updateEyeSocketTessellation entity) (entity.ChangeEvent propertyName) entity world
        updateEyeSocketTessellation entity world |> ignore<Handling>
        for propertyName in
            [nameof Entity.FillColor; nameof Entity.Size; nameof Entity.Scale] do
            World.monitor (fun _ world -> updateBasicParticleSeed entity world; Cascade) (entity.ChangeEvent propertyName) entity world
        World.monitor (fun evt world -> updateTrailEmitterTransform evt.Subscriber world; Cascade) entity.BodyTransformEvent entity world
        updateBasicParticleSeed entity world
        updateTrailEmitterTransform entity world

    override this.Physics (_, _, _, _, entity, world) =
        updateTrailEmitterTransform entity world

    override this.Render (_, entity, world) = 
        let mutable transform = entity.GetTransform world
        let clipOpt = entity.GetClipOpt world |> Option.toValueOption
        transform.Elevation <- Single.BitIncrement transform.Elevation
        World.renderContour
            { Transform = transform
              ClipOpt = clipOpt
              Contour = entity.GetEyeSocketTessellation world } world
        transform.Elevation <- Single.BitIncrement transform.Elevation
        World.renderContour
            { Transform = transform
              ClipOpt = clipOpt
              Contour = getEyeTessellation entity world } world

// this is the dispatcher that defines the behavior of the screen where gameplay takes place.
type Scene04_SquareRaceDispatcher () =
    inherit ScreenDispatcherImSim ()

    // here we define default property values
    static member Properties =
        [define Screen.GameplayState Quit]

    // here we define the behavior of our gameplay
    override this.Process (_, screen, world) =

        if screen.GetSelected world then
            World.beginGroup "Group" [] world
            // declare border
            World.doBlockBody2d "Border"
                [Entity.Position .= v3 0.0f 20.0f 0.0f
                 Entity.Size .= v3 620.0f 260.0f 0.0f
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

            World.doEntity<SquareDispatcher> "Green"
                [Entity.Position |= v3 -100f 0f 0f
                 Entity.LinearVelocity |= v3 32f 32f 0f] world |> ignore
            World.doEntity<SquareDispatcher> "Red"
                [Entity.Position |= v3 -132f 0f 0f
                 Entity.LinearVelocity |= v3 -32f 32f 0f
                 Entity.FillColor .= Color.Red] world |> ignore
            World.doEntity<SquareDispatcher> "Yellow"
                [Entity.Position |= v3 -100f -32f 0f
                 Entity.LinearVelocity |= v3 32f -32f 0f
                 Entity.FillColor .= Color.Yellow] world |> ignore
            World.doEntity<SquareDispatcher> "Blue"
                [Entity.Position |= v3 -132f -32f 0f
                 Entity.LinearVelocity |= v3 -32f -32f 0f
                 Entity.FillColor .= Color.Blue] world |> ignore
            // https://www.shadertoy.com/view/lsfBWs
            let rainbow level =
                match level with
                | 0 -> color 1.0f 0.0f 0.0f 1.0f // red
                | 1 -> color 1.0f 0.5f 0.0f 1.0f // orange
                | 2 -> color 1.0f 1.0f 0.0f 1.0f // yellow
                | 3 -> color 0.0f 0.5f 0.0f 1.0f // green
                | 4 -> color 0.0f 0.0f 1.0f 1.0f // blue
                | _ -> color 0.5f 0.0f 0.5f 1.0f // purple
            let smoothRainbow x =
                let rainbowLevel = x * 6.0f
                let level1 = int rainbowLevel % 6
                let level2 = (level1 + 1) % 6
                let a = rainbow level1
                let b = rainbow level2
                Color.Lerp (a, b, rainbowLevel - floor rainbowLevel)
            World.doEntity<SquareDispatcher> "Rainbow"
                [Entity.Position |= v3 -68f -32f 0f
                 Entity.LinearVelocity |= v3 -32f -0f 0f
                 Entity.FillColor @= smoothRainbow (world.ClockTime / 2.0f)] world |> ignore

            World.doText "Instructions"
                [Entity.Position .= v3 0.0f 164.0f 0.0f
                 Entity.Size .= v3 620.0f 22.0f 0.0f
                 Entity.Absolute .= true
                 Entity.Elevation .= 10.0f
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.FontSizing .= Some 8.0f
                 Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)
                 Entity.Text .= "OBSERVE: pupils follow velocity; trails show motion. No drag controls in this room."] world

            // declare quit button
            if World.doButton "Quit" [Entity.Position .= v3 232.0f -144.0f 0.0f; Entity.Text .= "Quit"] world then
                screen.SetGameplayState Quit world

            World.endGroup world
