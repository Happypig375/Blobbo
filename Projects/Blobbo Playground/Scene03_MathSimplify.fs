namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu
open BlobboPlayground

// this is the dispatcher that defines the behavior of the screen where gameplay takes place.
type Scene03_MathSimplifyDispatcher () =
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

            // The math bodies are dynamic glyph-sized rigid bodies, so give gravity a
            // room-local floor above the footer instead of relying on the screen border.
            World.doBlockBody2d "Math room floor"
                [Entity.BodyType .= Static
                 Entity.Position .= v3 0f -110f 0f
                 Entity.Size .= v3 460f 10f 0f] world |> ignore
            for (name, x) in [("Math room left wall", -225f); ("Math room right wall", 225f)] do
                World.doBlockBody2d name
                    [Entity.BodyType .= Static
                     Entity.Position .= v3 x 0f 0f
                     Entity.Size .= v3 10f 220f 0f] world |> ignore

            World.doEntity<MathObjectDispatcher> "Math1"
                [Entity.Position |= v3 -100f 0f 0f
                 Entity.MathFontSize .= 24f
                 Entity.LaTeX .= @"\square + \square"] world |> ignore
            World.doBallBody2d "Ball1"
                [Entity.Position |= v3 -80f 0f 0f
                 Entity.Size .= v3Dup 16f] world |> ignore

            World.doEntity<MathObjectDispatcher> "Math2"
                [Entity.Position |= v3 0f 0f 0f
                 Entity.MathFontSize .= 24f
                 Entity.LaTeX .= @"3x"] world |> ignore
            World.doBallBody2d "Ball2"
                [Entity.Position |= v3 30f 0f 0f
                 Entity.Size .= v3Dup 16f] world |> ignore
            World.doEntity<MathObjectDispatcher> "Math3"
                [Entity.Position |= v3 100f 0f 0f
                 Entity.MathFontSize .= 24f
                 Entity.LaTeX .= @"4x+6"] world |> ignore
   
            World.doBlockBody2d "Block"
                [Entity.BodyType .= Kinematic
                 // Keep the moving-wall experiment in the room above the footer and Quit control.
                 Entity.Position .= v3 -180f -52f 0f
                 Entity.Size .= v3 10f 100f 0f
                 Entity.LinearVelocity .= v3 40f 0f 0f] world |> ignore
            let block = world.DeclaredEntity
            if (block.GetPosition world).X > 90f then
                block.SetLinearVelocity v3Zero world |> ignore

            World.doText "Instructions"
                [Entity.Position .= v3 0.0f 164.0f 0.0f
                 Entity.Size .= v3 620.0f 22.0f 0.0f
                 Entity.Absolute .= true
                 Entity.Elevation .= 10.0f
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.FontSizing .= Some 8.0f
                 Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)
                 Entity.Text .= "OBSERVE: the moving wall drives collisions. Watch math expressions combine."] world

            // declare quit button
            if World.doButton "Quit" [Entity.Position .= v3 232.0f -144.0f 0.0f; Entity.Text .= "Quit"] world then
                screen.SetGameplayState Quit world

            World.endGroup world
