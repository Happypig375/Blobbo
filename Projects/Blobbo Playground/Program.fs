namespace BlobboPlayground
open System
open System.IO
open SDL
open Nu
module Program =

    // this the entry point for your Nu application
    let [<EntryPoint; STAThread>] main args =

        if Array.contains "--verify-m1" args then
            M1Verification.report ()
        else
            M1Launch.Direct <- Array.contains "--m1" args
            M1Launch.Combined <- Array.contains "--combined" args || not M1Launch.Direct

            // this points the current working directory at application's base directory
            Directory.SetCurrentDirectory AppContext.BaseDirectory

            // this initializes Nu before other Nu code is run
            Nu.init ()
            if M1Launch.Direct then Globals.Render.DisplayScalar <- 1

            // this specifies the window configuration used to display the game
            let windowTitle =
                if M1Launch.Direct then "Blobbo M1 • Body × Control Lab"
                elif M1Launch.Combined then "Blobbo Playground • Combined Interaction Room"
                else "Blobbo Playground"
            let sdlWindowConfig =
                { SdlWindowConfig.defaultConfig with
                    WindowTitle = windowTitle
                    WindowFlags = SdlWindowConfig.defaultConfig.WindowFlags &&& ~~~SDL_WindowFlags.SDL_WINDOW_RESIZABLE }

            // this specifies the configuration of the game engine's use of SDL
            let sdlConfig = { SdlConfig.defaultConfig with WindowConfig = sdlWindowConfig }

            // this specifies the world config using the above SDL config
            let worldConfig = { WorldConfig.defaultConfig with SdlConfig = sdlConfig }

            // this runs the engine with the given config and plugin, starting the game
            World.run ignore worldConfig (BlobboPlaygroundPlugin ())