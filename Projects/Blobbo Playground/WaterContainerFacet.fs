namespace BlobboPlayground
open System.Numerics
open Prime
open Nu

[<RequireQualifiedAccess>]
module WaterParticles =

    /// Fixed lattice bursts make reset experiments independent of process-global randomness.
    let burst particleConfig (center : Vector2) (velocity : Vector2) spread count =
        let count = max 0 count
        let width = min 8 (max 1 (int (ceil (sqrt (single count)))))
        let mutable offsetSum = v2Zero
        for index in 0 .. count - 1 do
            offsetSum <- offsetSum + v2 (single (index % width)) (single (index / width))
        let meanOffset = if count > 0 then offsetSum / single count else v2Zero
        SArray.init count (fun index ->
            let offset = (v2 (single (index % width)) (single (index / width)) - meanOffset) * spread
            { FluidParticlePosition = (center + offset).V3
              FluidParticleVelocity = velocity.V3
              FluidParticleConfig = particleConfig })

    /// Vent above the current solid envelope so an intact container cannot trap its own gas.
    let smokeAbovePerimeter (perimeter : Box2) count =
        let particles = burst "Smoke" v2Zero (v2 0.0f 36.0f) 2.0f count
        if particles.Length = 0 then particles
        else
            let minimumY = particles |> Seq.map (fun particle -> particle.FluidParticlePosition.Y) |> Seq.min
            let clearance = FluidParticleConfig.smokeConfig.Radius + 2.0f
            let offset = v3 perimeter.Center.X (perimeter.Max.Y + clearance - minimumY) 0.0f
            particles |> SArray.map (fun particle -> { particle with FluidParticlePosition = particle.FluidParticlePosition + offset })

module [<AutoOpen>] WaterContainerFacet =

    type Entity with
        member this.GetWaterContent world : int = this.Get (nameof this.WaterContent) world
        member this.SetWaterContent (value : int) world = this.Set (nameof this.WaterContent) value world
        member this.WaterContent = lens (nameof this.WaterContent) this this.GetWaterContent this.SetWaterContent
        member this.GetWorldFluidEmitter world : Entity Address = this.Get (nameof this.WorldFluidEmitter) world
        member this.SetWorldFluidEmitter (value : Entity Address) world = this.Set (nameof this.WorldFluidEmitter) value world
        member this.WorldFluidEmitter = lens (nameof this.WorldFluidEmitter) this this.GetWorldFluidEmitter this.SetWorldFluidEmitter
        member this.HeatEvent = stoa<unit> "Heat/Event" --> this

    /// Facet that gives an entity water container capabilities:
    /// tracks water content (measured in particle count), connects to a fluid emitter,
    /// and responds to Heat events by converting stored water to smoke particles
    /// via the world fluid emitter.
    type WaterContainerFacet () =
        inherit Facet (false, false, false)

        let handleHeat (evt : Event<unit, Entity>) world =
            let entity = evt.Subscriber
            let waterContent = entity.GetWaterContent world
            if world.TimeAdvancing && waterContent > 0 then
                match tryResolve (entity.GetWorldFluidEmitter world) entity with
                | Some emitter ->
                    let smokeCount = waterContent |> max 1
                    World.emitFluidParticles
                        (WaterParticles.smokeAbovePerimeter (entity.GetPerimeter world).Box2 smokeCount)
                        (emitter.GetFluidEmitterId world) world
                | None -> ()
                entity.SetWaterContent 0 world
            Cascade

        static member Properties =
            [define Entity.WaterContent 0
             define Entity.WorldFluidEmitter Address.empty]

        override this.Register (entity, world) =
            World.sense handleHeat entity.HeatEvent entity (nameof WaterContainerFacet) world