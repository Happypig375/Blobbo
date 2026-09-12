namespace BlobboPlayground.Tests
open System.Numerics
open NUnit.Framework
open Prime
open Nu
open BlobboPlayground

// Keep the real World lifecycle without starting the participant scene during this regression.
type RewindLifecycleGameDispatcher () =
    inherit GameDispatcher ()

type RewindLifecyclePlugin () =
    inherit NuPlugin ()

module RewindableTests =

    let private attachRewindable (entity : Entity) world =
        match World.trySetEntityFacetNames (set [nameof RewindableFacet]) entity world with
        | Right changed -> Assert.That (changed, Is.True)
        | Left error -> Assert.Fail ("Rewindable attachment failed: " + error)
        Assert.That (entity.Has<RewindableFacet> world, Is.True)

    [<Test; NonParallelizable; Category "Integration">]
    let ``Rewind retains one rigid body through live motion preview commit and recreation`` () =
        Nu.init ()
        let worldConfig = { WorldConfig.defaultConfig with Accompanied = true }
        let windowSize = Globals.Render.DisplayVirtualResolution * Globals.Render.DisplayScalar
        match SdlDeps.tryMake worldConfig.SdlConfig false windowSize with
        | Right sdlDeps ->
            use _ = sdlDeps
            let windowViewport = Viewport.makeWindow1 windowSize
            let geometryViewport = Viewport.makeGeometry windowViewport.Bounds.Size
            let world = World.make (constant None) sdlDeps worldConfig windowSize geometryViewport windowViewport (RewindLifecyclePlugin ())
            try
                // Register the game-owned facet explicitly; the isolated test plugin lives in another assembly.
                World.updateLateBindings false [|typeof<RewindableFacet>.Assembly|] world
                Assert.That (Map.containsKey (nameof RewindableFacet) (World.getFacets world), Is.True)
                let screen = World.createScreen<ScreenDispatcher> (Some "Rewind lifecycle") world
                let group = World.createGroup<GroupDispatcher> (Some "Props") screen world
                World.selectScreen (IdlingState GameTime.zero) screen world
                let subject = World.createEntity<BoxBody2dDispatcher> None DefaultOverlay (Some [|"Subject"|]) group world
                subject.SetSize (v3Dup 16.0f) world
                subject.SetGravity GravityIgnore world
                let originalBodyId = subject.GetBodyId world
                Assert.That (World.getBodyExists originalBodyId world, Is.True)

                // This matches ImSim: its dispatcher body exists before extrinsic facets are attached.
                attachRewindable subject world
                Assert.That (subject.GetBodyId world, Is.EqualTo originalBodyId)
                Assert.That (subject.GetPhysicsMotion world, Is.EqualTo ManualMotion)
                Assert.That (World.getBodyExists { originalBodyId with BodyIndex = 0 } world, Is.False)
                subject.SetFacetNames Set.empty world
                Assert.That (subject.GetPhysicsMotion world, Is.EqualTo SynchronizedMotion)
                Assert.That (subject.GetBodyId world, Is.EqualTo originalBodyId)
                Assert.That (World.getBodyExists originalBodyId world, Is.True)
                attachRewindable subject world
                Assert.That (subject.GetPhysicsMotion world, Is.EqualTo ManualMotion)
                subject.SetLinearVelocity (v3 96.0f 0.0f 0.0f) world
                Assert.That (World.getBodyLinearVelocity originalBodyId world, Is.EqualTo (v3 96.0f 0.0f 0.0f))

                let mutable frame = 0
                let mutable beforePreview = v3Zero
                let afterFrame world =
                    frame <- frame + 1
                    match frame with
                    | 3 ->
                        Assert.That ((subject.GetPosition world).X, Is.GreaterThan 0.0f)
                        Assert.That ((subject.GetRewindHistory world).Length, Is.GreaterThan 0)
                        beforePreview <- subject.GetPosition world
                        World.setTimeAdvancing false world
                        subject.SetRewindPreview (Some (GameTime.ofUpdates 2L)) world
                    | 4 ->
                        subject.SetRewindPreview (Some (GameTime.ofUpdates 3L)) world
                        World.publish { RewindAnchorOpt = ValueNone; RewindTime = GameTime.ofUpdates 3L } subject.RewindPreviewEvent subject world
                        Assert.That (subject.GetPosition world, Is.EqualTo beforePreview)
                        Assert.That (subject.GetBodyId world, Is.EqualTo originalBodyId)
                        Assert.That (World.getBodyExists originalBodyId world, Is.True)
                    | 5 ->
                        subject.SetRewindPreview None world
                        Assert.That (subject.GetPosition world, Is.EqualTo beforePreview)
                        World.setTimeAdvancing true world
                    | 7 ->
                        // A direct commit also exercises manual pose/velocity synchronization without an enabled-body rebuild.
                        World.publish { RewindAnchorOpt = ValueNone; RewindTime = GameTime.ofUpdates 100L } subject.RewindEvent subject world
                        Assert.That (Vector3.Distance (subject.GetPosition world, v3Zero), Is.LessThan 0.001f)
                        Assert.That (World.getBodyLinearVelocity originalBodyId world, Is.EqualTo v3Zero)
                        Assert.That (subject.GetBodyId world, Is.EqualTo originalBodyId)
                    | 10 ->
                        Assert.That (Vector3.Distance (subject.GetPosition world, v3Zero), Is.LessThan 0.001f)
                        World.destroyEntityImmediate subject world
                        Assert.That (World.getBodyExists originalBodyId world, Is.False)
                        Assert.That (World.getBodyExists { originalBodyId with BodyIndex = 0 } world, Is.False)
                        let replacement = World.createEntity<BoxBody2dDispatcher> None DefaultOverlay (Some [|"Subject"|]) group world
                        attachRewindable replacement world
                        Assert.That (replacement.GetBodyId world, Is.EqualTo originalBodyId)
                        Assert.That (World.getBodyExists originalBodyId world, Is.True)
                        World.destroyEntityImmediate replacement world
                        Assert.That (World.getBodyExists originalBodyId world, Is.False)
                    | _ -> ()
                World.runWithoutCleanUp (fun _ -> frame < 10) ignore ignore afterFrame ignore ignore (Some ignore) world
                Assert.That (frame, Is.EqualTo 10)
            finally World.cleanUp world
        | Left error -> Assert.Fail (string error)