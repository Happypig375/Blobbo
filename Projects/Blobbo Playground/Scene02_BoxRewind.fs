namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu
open BlobboPlayground

type BoxRewindState =
    { ResumeTimeAdvancing : bool }

module BoxRewindState =

    let initial = { ResumeTimeAdvancing = true }

module [<AutoOpen>] BoxRewindExtensions =

    type Screen with
        member this.GetBoxRewindState world : BoxRewindState = this.Get (nameof Screen.BoxRewindState) world
        member this.SetBoxRewindState (value : BoxRewindState) world = this.Set (nameof Screen.BoxRewindState) value world
        member this.BoxRewindState = lens (nameof Screen.BoxRewindState) this this.GetBoxRewindState this.SetBoxRewindState

// this is the dispatcher that defines the behavior of the screen where gameplay takes place.
type Scene02_BoxRewindDispatcher () =
    inherit ScreenDispatcherImSim ()

    // here we define default property values
    static member Properties =
        [define Screen.GameplayState Quit
         define Screen.BoxRewindState BoxRewindState.initial]

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

            World.doBoxBody2d "Box"
                    [Entity.Position |= v3 -90f 0f 0f
                     Entity.Size .= v3Dup 16f
                     Entity.LinearVelocity |= v3 100f 0f 0f
                     Entity.Friction .= 0f
                     Entity.StaticImage .= Assets.Default.White
                     Entity.Color .= color 0.2f 0.9f 1.0f 1.0f
                     Entity.FacetNames .= Set.ofList [nameof RewindableFacet]] world |> ignore
            let rewindable = world.DeclaredEntity
            
            World.setEye2dCenter v2Zero world
        
            if not world.TimeAdvancing then
                World.doStaticSprite "Overlay" 
                    [Entity.Position .= v3 0f 0f 0.1f
                     Entity.Size .= (World.getDisplayVirtualResolution ()).V3
                     Entity.StaticImage .= Assets.Default.White
                     Entity.Color .= color 0.5f 0.5f 0.5f 0.5f] world |> ignore
            World.doBoxBody2d "Box2"
                    [Entity.Position |= v3 90f 0f 0f
                     Entity.Size .= v3Dup 16f
                     Entity.Friction .= 0f
                     Entity.FacetNames .= Set.ofList [nameof RewindableFacet]] world |> ignore

            let state = screen.GetBoxRewindState world
            let previewOpt = rewindable.GetRewindPreview world
            let previewing = previewOpt.IsSome
            let previewButton =
                if not previewing then
                    World.doButton "Preview rewind"
                        [Entity.Position .= v3 -112.0f -144.0f 0.0f
                         Entity.Absolute .= true
                         Entity.Elevation .= 32.0f
                         Entity.Size .= v3 112.0f 22.0f 0.0f
                         Entity.FontSizing .= Some 8.0f
                         Entity.Text .= "Preview (Down)"] world
                else false
            let commitButton =
                if previewing then
                    World.doButton "Commit rewind"
                        [Entity.Position .= v3 -60.0f -144.0f 0.0f
                         Entity.Absolute .= true
                         Entity.Elevation .= 32.0f
                         Entity.Size .= v3 112.0f 22.0f 0.0f
                         Entity.FontSizing .= Some 8.0f
                         Entity.Text .= "Commit (Enter)"] world
                else false
            let cancelButton =
                if previewing then
                    World.doButton "Cancel rewind"
                        [Entity.Position .= v3 60.0f -144.0f 0.0f
                         Entity.Absolute .= true
                         Entity.Elevation .= 32.0f
                         Entity.Size .= v3 112.0f 22.0f 0.0f
                         Entity.FontSizing .= Some 8.0f
                         Entity.Text .= "Cancel (Esc)"] world
                else false
            let beginPreview () =
                screen.SetBoxRewindState { ResumeTimeAdvancing = world.TimeAdvancing } world
                World.setTimeAdvancing false world
                rewindable.SetRewindPreview (Some GameTime.zero) world
            let commitPreview () =
                match rewindable.GetRewindPreview world with
                | Some rewindPreview ->
                    World.publish
                        { RewindAnchorOpt = ValueNone; RewindTime = rewindPreview }
                        rewindable.RewindEvent rewindable world
                    rewindable.SetRewindPreview None world
                    World.setTimeAdvancing state.ResumeTimeAdvancing world
                | None -> ()
            let cancelPreview () =
                rewindable.SetRewindPreview None world
                World.setTimeAdvancing state.ResumeTimeAdvancing world

            // Space / Down enter the same paused preview mode. Enter / Space commits it,
            // while Escape restores the exact advancing state from before the preview.
            let previewAction =
                if previewing then
                    if commitButton || World.isKeyboardKeyPressed KeyboardKey.Enter world || World.isKeyboardKeyPressed KeyboardKey.Space world then
                        commitPreview ()
                    elif cancelButton || World.isKeyboardKeyPressed KeyboardKey.Escape world then
                        cancelPreview ()
                    else
                        if World.isKeyboardKeyDown KeyboardKey.Left world then
                            rewindable.RewindPreview.Map (Option.map (fun rewindTime -> rewindTime + GameTime.ofUpdates 1L)) world
                        if World.isKeyboardKeyDown KeyboardKey.Right world then
                            rewindable.RewindPreview.Map
                                (Option.map (fun rewindTime -> max GameTime.zero (rewindTime - GameTime.ofUpdates 1L))) world
                    true
                elif previewButton || World.isKeyboardKeyPressed KeyboardKey.Down world || World.isKeyboardKeyPressed KeyboardKey.Space world then
                    beginPreview ()
                    true
                else false

            let pauseButton =
                if not previewing then
                    World.doButton "Pause"
                        [Entity.Position .= v3 -232.0f -144.0f 0.0f
                         Entity.Absolute .= true
                         Entity.Elevation .= 32.0f
                         Entity.Size .= v3 112.0f 22.0f 0.0f
                         Entity.FontSizing .= Some 8.0f
                         Entity.Text @= (if world.TimeAdvancing then "Pause" else "Resume")]
                        world
                else false
            if pauseButton && not previewAction then
                World.setTimeAdvancing (not world.TimeAdvancing) world

            let previewOpt = rewindable.GetRewindPreview world
            World.doText "Instructions"
                [Entity.Position .= v3 0.0f 144.0f 0.0f
                 Entity.Absolute .= true
                 Entity.Elevation .= 31.0f
                 Entity.Size .= v3 620.0f 22.0f 0.0f
                 Entity.BackdropImageOpt .= Some Assets.Default.Label
                 Entity.FontSizing .= Some 8.0f
                 Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)
                 Entity.TextColor @= (if previewOpt.IsSome then color 1.0f 0.85f 0.3f 1.0f else color 0.85f 0.9f 1.0f 1.0f)
                 Entity.Text @=
                    (match previewOpt with
                     | Some rewindTime -> sprintf "CYAN BOX: %.1fs EARLIER | LEFT / RIGHT: SCRUB | ENTER / SPACE: COMMIT | ESC: CANCEL" rewindTime.Seconds
                     | None -> "CYAN BOX REWINDS | DOWN / SPACE: PREVIEW | PAUSE BUTTON: STOP / RESUME")]
                world |> ignore

            // declare quit button
            if World.doButton "Quit"
                [Entity.Position .= v3 232.0f -144.0f 0.0f
                 Entity.Absolute .= true
                 Entity.Elevation .= 32.0f
                 Entity.Size .= v3 112.0f 22.0f 0.0f
                 Entity.FontSizing .= Some 8.0f
                 Entity.Text .= "Menu"] world then
                screen.SetGameplayState Quit world

            World.endGroup world
