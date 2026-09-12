namespace BlobboPlayground
open System
open System.Numerics
open Prime
open Nu
open BlobboPlayground

// this determines what state the game is in. To learn about ImSim in Nu, see -
// https://github.com/bryanedds/Nu/wiki/Immediate-Mode-for-Games-via-ImSim
type GameState =
    | Splash
    | Title
    | Scene01_BlobboThrow
    | Scene02_BoxRewind
    | Scene03_MathSimplify
    | Scene04_SquareRace
    | Scene05_HeaterCooler
    | Scene06_M1ControlStudy
// this extends the Game API to expose the above ImSim model as a property.
[<AutoOpen>]
module BlobboPlaygroundExtensions =
    type Game with
        member this.GetGameState world : GameState = this.Get (nameof Game.GameState) world
        member this.SetGameState (value : GameState) world = this.Set (nameof Game.GameState) value world
        member this.GameState = lens (nameof Game.GameState) this this.GetGameState this.SetGameState

// this is the dispatcher that customizes the top-level behavior of our game.
type BlobboPlaygroundDispatcher () =
    inherit GameDispatcherImSim ()

    // here we define default property values
    static member Properties =
        [define Game.GameState (if M1Launch.Direct || M1Launch.Combined then Scene06_M1ControlStudy else Splash)]

    // here we define the game's top-level behavior
    override this.Process (game, world) =

        // declare splash screen
        let behavior = Slide (Constants.Dissolve.Default, Constants.Slide.Default, None, Simulants.Title)
        let results = World.beginScreen Simulants.Splash.Name (game.GetGameState world = Splash) behavior [] world
        if FQueue.contains Deselecting results && game.GetGameState world = Splash then game.SetGameState Title world
        World.endScreen world

        // declare title screen
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        World.beginScreen Simulants.Title.Name (game.GetGameState world = Title) behavior [] world |> ignore
        World.beginGroup "Gui" [] world
        World.beginPanel "Panel"
            [Entity.Size .= (World.getDisplayVirtualResolution ()).V3
             Entity.Layout .= Flow (FlowDownward, FlowUnlimited)
             Entity.LayoutMargin .= v2 30.0f 6.0f] world
        World.doText "MenuTitle"
            [Entity.Size .= v3 580.0f 32.0f 0.0f
             Entity.Text .= "BLOBBO PLAYGROUND - CHOOSE A ROOM"
             Entity.FontSizing .= Some 14.0f] world
        let button name label =
            World.doButton name
                [Entity.Size .= v3 580.0f 32.0f 0.0f
                 Entity.Text .= label
                 Entity.FontSizing .= Some 11.0f] world
        let select state =
            World.setTimeAdvancing true world
            World.setEye2dSize (World.getDisplayVirtualResolution ()).V2 world
            game.SetGameState state world
        if button "CombinedRoom" "Combined: water, heat and machines + M1 controls" then
            M1Launch.Direct <- false
            M1Launch.Combined <- true
            select Scene06_M1ControlStudy
        if button "M1Focused" "M1 focused protocol (Toy / Target)" then
            M1Launch.Direct <- true
            M1Launch.Combined <- false
            select Scene06_M1ControlStudy
        if button "Scene01_BlobboThrow" "01 — Blobbo throw + water balloons" then select Scene01_BlobboThrow
        if button "Scene02_BoxRewind" "02 — Box rewind" then select Scene02_BoxRewind
        if button "Scene03_MathSimplify" "03 — Math collision simplifier" then select Scene03_MathSimplify
        if button "Scene04_SquareRace" "04 — Velocity eyes and trails" then select Scene04_SquareRace
        if button "Scene05_HeaterCooler" "05 — Heater / cooler / fan" then select Scene05_HeaterCooler
        if button "Exit" "Exit" && world.Unaccompanied then World.exit world
        World.endPanel world
        World.endGroup world
        World.endScreen world

        // declare scene 01
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        let results = World.beginScreen<Scene01_BlobboThrowDispatcher> Simulants.Scene01_BlobboThrow.Name (game.GetGameState world = Scene01_BlobboThrow) behavior [] world
        if FQueue.contains Select results then Simulants.Scene01_BlobboThrow.SetGameplayState Playing world
        if FQueue.contains Deselecting results then Simulants.Scene01_BlobboThrow.SetGameplayState Quit world
        if Simulants.Scene01_BlobboThrow.GetSelected world && Simulants.Scene01_BlobboThrow.GetGameplayState world = Quit then
            World.setTimeAdvancing true world
            game.SetGameState Title world
        World.endScreen world

        // declare scene 02
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        let results = World.beginScreen<Scene02_BoxRewindDispatcher> Simulants.Scene02_BoxRewind.Name (game.GetGameState world = Scene02_BoxRewind) behavior [] world
        if FQueue.contains Select results then Simulants.Scene02_BoxRewind.SetGameplayState Playing world
        if FQueue.contains Deselecting results then Simulants.Scene02_BoxRewind.SetGameplayState Quit world
        if Simulants.Scene02_BoxRewind.GetSelected world && Simulants.Scene02_BoxRewind.GetGameplayState world = Quit then
            World.setTimeAdvancing true world
            game.SetGameState Title world
        World.endScreen world

        // declare scene 03
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        let results = World.beginScreen<Scene03_MathSimplifyDispatcher> Simulants.Scene03_MathSimplify.Name (game.GetGameState world = Scene03_MathSimplify) behavior [] world
        if FQueue.contains Select results then Simulants.Scene03_MathSimplify.SetGameplayState Playing world
        if FQueue.contains Deselecting results then Simulants.Scene03_MathSimplify.SetGameplayState Quit world
        if Simulants.Scene03_MathSimplify.GetSelected world && Simulants.Scene03_MathSimplify.GetGameplayState world = Quit then
            World.setTimeAdvancing true world
            game.SetGameState Title world
        World.endScreen world

        // declare scene 04
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        let results = World.beginScreen<Scene04_SquareRaceDispatcher> Simulants.Scene04_SquareRace.Name (game.GetGameState world = Scene04_SquareRace) behavior [] world
        if FQueue.contains Select results then Simulants.Scene04_SquareRace.SetGameplayState Playing world
        if FQueue.contains Deselecting results then Simulants.Scene04_SquareRace.SetGameplayState Quit world
        if Simulants.Scene04_SquareRace.GetSelected world && Simulants.Scene04_SquareRace.GetGameplayState world = Quit then
            World.setTimeAdvancing true world
            game.SetGameState Title world
        World.endScreen world

        // declare scene 05
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        let results = World.beginScreen<Scene05_HeaterCoolerDispatcher> Simulants.Scene05_HeaterCooler.Name (game.GetGameState world = Scene05_HeaterCooler) behavior [] world
        if FQueue.contains Select results then Simulants.Scene05_HeaterCooler.SetGameplayState Playing world
        if FQueue.contains Deselecting results then Simulants.Scene05_HeaterCooler.SetGameplayState Quit world
        if Simulants.Scene05_HeaterCooler.GetSelected world && Simulants.Scene05_HeaterCooler.GetGameplayState world = Quit then
            World.setTimeAdvancing true world
            game.SetGameState Title world
        World.endScreen world

        // declare M1 body and control study
        let behavior = Dissolve (Constants.Dissolve.Default, None)
        let results = World.beginScreen<Scene06_M1ControlStudyDispatcher> Simulants.Scene06_M1ControlStudy.Name (game.GetGameState world = Scene06_M1ControlStudy) behavior [] world
        if FQueue.contains Select results then
            let screen = Simulants.Scene06_M1ControlStudy
            screen.SetGameplayState Playing world
            screen.SetM1SceneState (M1SceneState.enter M1Launch.Combined (screen.GetM1SceneState world)) world
        if FQueue.contains Deselecting results then Simulants.Scene06_M1ControlStudy.SetGameplayState Quit world
        if Simulants.Scene06_M1ControlStudy.GetSelected world &&
           Simulants.Scene06_M1ControlStudy.GetGameplayState world = Quit then
            World.setTimeAdvancing true world
            game.SetGameState Title world
        World.endScreen world

        // handle Alt+F4 when not in editor
        if  World.isKeyboardAltDown world &&
            World.isKeyboardKeyDown KeyboardKey.F4 world &&
            world.Unaccompanied then
            World.exit world