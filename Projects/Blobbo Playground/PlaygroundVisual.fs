namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu

[<RequireQualifiedAccess>]
module PlaygroundVisual =

    let sprite name (position : Vector2) (size : Vector2) elevation colorValue image world =
        World.doStaticSprite name
            [Entity.Position @= position.V3
             Entity.Size @= size.V3
             Entity.Elevation .= elevation
             Entity.StaticImage .= image
             Entity.Color @= colorValue] world |> ignore

    let panel name (position : Vector2) (size : Vector2) world =
        World.doStaticSprite name
            [Entity.Position .= position.V3
             Entity.Size .= size.V3
             Entity.Absolute .= true
             Entity.Elevation .= 29.0f
             Entity.StaticImage .= Assets.Default.White
             Entity.Color .= color 0.025f 0.04f 0.1f 0.97f] world |> ignore

    let segment name (startPoint : Vector2) (stopPoint : Vector2) thickness elevation colorValue world =
        let delta = stopPoint - startPoint
        let length = delta.Length ()
        if length > 0.001f then
            World.doStaticSprite name
                [Entity.Position @= ((startPoint + stopPoint) * 0.5f).V3
                 Entity.Size @= v3 length thickness 0.0f
                 Entity.Rotation @= Quaternion.CreateFromAxisAngle (Vector3.UnitZ, atan2 delta.Y delta.X)
                 Entity.Elevation .= elevation
                 Entity.StaticImage .= Assets.Default.White
                 Entity.Color @= colorValue] world |> ignore

    let text name absolute (position : Vector2) (size : Vector2) fontSize colorValue value world =
        World.doText name
            [Entity.Position .= position.V3
             Entity.Size .= size.V3
             Entity.Absolute .= absolute
             Entity.Elevation .= (if absolute then 31.0f else -0.25f)
             Entity.FontSizing .= Some fontSize
             Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)
             Entity.TextColor @= colorValue
             Entity.Text @= value] world

    let button name text (position : Vector2) width world =
        World.doButton name
            [Entity.Position .= position.V3
             Entity.Size .= v3 width 22.0f 0.0f
             Entity.Elevation .= 32.0f
             Entity.FontSizing .= Some 8.0f
             Entity.Text @= text] world

    let configureFluidAppearance (emitter : Entity) (world : World) =
        if world.DeclaredInitializing || world.ImSimReinitializing then
            let renders = emitter.GetFluidParticleRenders world
            let smoke = renders["Smoke"]
            let mutable transform = smoke.Transform
            transform.Size <- v3 48.0f 48.0f 0.0f
            transform.Elevation <- -0.75f
            let smoke = { smoke with Transform = transform; Color = color 0.72f 0.8f 0.88f 0.5f }
            emitter.SetFluidParticleRenders (Map.add "Smoke" smoke renders) world

[<RequireQualifiedAccess>]
module ThermalStationVisual =

    let heatStatus heatEnabled contacts =
        if not heatEnabled then "Heat OFF (H)"
        elif contacts > 0 then sprintf "Heat ON / contact %d (H)" contacts
        else "Heat ON / ready (H)"

    let heater name (plateBounds : Box2) (heatBounds : Box2) heatEnabled contacts world =
        let heatingContact = heatEnabled && contacts > 0
        let coilTint =
            if heatingContact then color 1.0f 0.85f 0.35f 1.0f
            elif heatEnabled then color 1.0f 0.3f 0.07f 1.0f
            else color 0.38f 0.4f 0.45f 1.0f
        let zoneTint =
            if heatingContact then color 1.0f 0.65f 0.15f 0.25f
            elif heatEnabled then color 1.0f 0.28f 0.08f 0.1f
            else color 0.38f 0.4f 0.45f 0.07f
        PlaygroundVisual.sprite (name + " Plate") plateBounds.Center plateBounds.Size 0.0f
            (color 0.16f 0.18f 0.22f 1.0f) Assets.Default.White world
        PlaygroundVisual.sprite (name + " Zone") heatBounds.Center heatBounds.Size -0.5f zoneTint Assets.Default.White world
        for (edgeName, start, stop) in
            [(" Bottom", heatBounds.Min, v2 heatBounds.Max.X heatBounds.Min.Y)
             (" Top", v2 heatBounds.Min.X heatBounds.Max.Y, heatBounds.Max)
             (" Left", heatBounds.Min, v2 heatBounds.Min.X heatBounds.Max.Y)
             (" Right", v2 heatBounds.Max.X heatBounds.Min.Y, heatBounds.Max)] do
            PlaygroundVisual.segment (name + edgeName) start stop 1.0f 0.5f coilTint world
        for burner in 0 .. 2 do
            let center = plateBounds.Center + v2 (single (burner - 1) * plateBounds.Size.X * 0.3f) 0.0f
            for index in 0 .. 15 do
                let angle = single index * MathF.TWO_PI / 16.0f
                let nextAngle = single (index + 1) * MathF.TWO_PI / 16.0f
                let radius = v2 (plateBounds.Size.X * 0.1f) (plateBounds.Size.Y * 0.25f)
                let start = center + v2 (cos angle * radius.X) (sin angle * radius.Y)
                let stop = center + v2 (cos nextAngle * radius.X) (sin nextAngle * radius.Y)
                PlaygroundVisual.segment (sprintf "%s Coil %d %d" name burner index) start stop 1.6f 1.0f coilTint world
            if heatEnabled then
                let rise = heatBounds.Size.Y * 0.18f
                for index in 0 .. 2 do
                    let start =
                        v2 (center.X + (if index % 2 = 0 then -2.0f else 2.0f)) (heatBounds.Min.Y + rise * single (index + 1))
                    let stop = v2 (center.X + (if index % 2 = 0 then 2.0f else -2.0f)) (start.Y + rise)
                    PlaygroundVisual.segment (sprintf "%s Rise %d %d" name burner index) start stop 1.0f 0.5f coilTint world
        // World labels sit behind bodies; live status remains in the room's unobstructed heat button.
        PlaygroundVisual.text (name + " Label") false (v2 heatBounds.Center.X (heatBounds.Max.Y + 9.0f))
            (v2 (max 178.0f heatBounds.Size.X) 14.0f) 7.5f coilTint "HEAT: WATER TO SMOKE" world

    let cooler name (bounds : Box2) world =
        PlaygroundVisual.sprite (name + " Zone") bounds.Center bounds.Size -0.5f
            (color 0.12f 0.55f 0.8f 0.65f) Assets.Default.White world
        PlaygroundVisual.segment (name + " Lower edge") bounds.Min (v2 bounds.Max.X bounds.Min.Y) 2.0f 0.5f Color.Cyan world
        PlaygroundVisual.text (name + " Label") false bounds.Center (v2 (max 124.0f bounds.Size.X) 16.0f) 7.5f Color.White
            "COOL: SMOKE TO WATER" world