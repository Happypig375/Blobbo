namespace BlobboPlayground
open System
open System.Collections.Generic
open System.Numerics
open Prime
open Nu

type RewindRecord = { PropertyName : string; PreviousValue : Symbol; TimePassed : GameTime }
type InteractionRecord = { WithOther : Entity Address; WorldTime : GameTime }
type Rewind = { RewindAnchorOpt : InteractionRecord voption; RewindTime : GameTime }
module Rewind =
    let [<Literal>] EventName = "Rewind/Event"
    let Event = stoa<Rewind> EventName
    let PreviewEvent = stoa<Rewind> "RewindPreview/Event"
module [<AutoOpen>] RewindableExtensions =
    type Entity with
        member this.GetRewindPreview world : GameTime option = this.Get (nameof this.RewindPreview) world
        member this.SetRewindPreview (value : GameTime option) world = this.Set (nameof this.RewindPreview) value world
        member this.RewindPreview = lens (nameof this.RewindPreview) this this.GetRewindPreview this.SetRewindPreview
        member this.GetRewindHistory world : RewindRecord list = this.Get (nameof this.RewindHistory) world
        member this.SetRewindHistory (value : RewindRecord list) world = this.Set (nameof this.RewindHistory) value world
        member this.RewindHistory = lens (nameof this.RewindHistory) this this.GetRewindHistory this.SetRewindHistory
        member this.GetTimeSinceLastHistoryEntry world : GameTime = this.Get (nameof this.TimeSinceLastHistoryEntry) world
        member this.SetTimeSinceLastHistoryEntry (value : GameTime) world = this.Set (nameof this.TimeSinceLastHistoryEntry) value world
        member this.TimeSinceLastHistoryEntry = lens (nameof this.TimeSinceLastHistoryEntry) this this.GetTimeSinceLastHistoryEntry this.SetTimeSinceLastHistoryEntry
        member this.GetRewindHistoryActiveInternal world : bool = this.Get (nameof this.RewindHistoryActiveInternal) world
        member this.SetRewindHistoryActiveInternal (value : bool) world = this.Set (nameof this.RewindHistoryActiveInternal) value world
        member this.RewindHistoryActiveInternal = lens (nameof this.RewindHistoryActiveInternal) this this.GetRewindHistoryActiveInternal this.SetRewindHistoryActiveInternal
        member this.RewindEvent = Rewind.Event --> this
        member this.RewindPreviewEvent = Rewind.PreviewEvent --> this

/// Register this after initialization
type RewindableFacet () =
    inherit Facet (false, false, false)

    static let synchronizeBody (entity : Entity) world =
        let bodyId = entity.GetBodyId world
        if World.getBodyExists bodyId world then
            World.setBodyCenter (entity.GetPerimeterCenter world) bodyId world
            World.setBodyRotation (entity.GetRotation world) bodyId world
            World.setBodyLinearVelocity (entity.GetLinearVelocity world) bodyId world
            World.setBodyAngularVelocity (entity.GetAngularVelocity world) bodyId world

    static member Facets =
        [typeof<RigidBodyFacet>]

    static member Properties =
        [define Entity.RewindPreview None
         define Entity.RewindHistory []
         define Entity.TimeSinceLastHistoryEntry GameTime.zero
         nonPersistent Entity.RewindHistoryActiveInternal true]

    override _.Register (entity, world) =
        // Extrinsic facet attachment preserves existing properties, so adopt manual motion explicitly.
        let physicsMotion = entity.GetPhysicsMotion world
        entity.SetPhysicsMotion ManualMotion world
        World.sense (fun event world ->
            if Set.contains (nameof RewindableFacet) (event.Data.Previous :?> string Set) &&
               not (Set.contains (nameof RewindableFacet) (event.Data.Value :?> string Set)) then
                entity.SetPhysicsMotion physicsMotion world
            Cascade) entity.FacetNames.ChangeEvent entity (nameof RewindableFacet) world

        // sense rewind event
        let (~-) = GameTime.unary ((~-) >> UpdateTime) ((~-) >> TickTime)
        World.sense (fun event world ->
            let entity = event.Subscriber
            let mutable remainingGameTime = event.Data.RewindTime
            let mutable rewindAnchorOpt = event.Data.RewindAnchorOpt
            if rewindAnchorOpt.IsNone &&
               (remainingGameTime <- remainingGameTime - entity.GetTimeSinceLastHistoryEntry world;
                remainingGameTime <= GameTime.zero)
            then entity.SetTimeSinceLastHistoryEntry -remainingGameTime world
            else
                let rewindProperties = Dictionary ()
                let mutable continued = true
                while continued do
                    match entity.GetRewindHistory world with
                    | rewindRecord :: rest ->
                        entity.SetRewindHistory rest world // NOTE: entity property must be updated before recursing via event publish
                        if rewindRecord.PropertyName <> Rewind.EventName then
                            rewindProperties[rewindRecord.PropertyName] <- rewindRecord.PreviousValue
                        else
                            let interactionRecord = rewindRecord.PreviousValue |> symbolToValue<InteractionRecord>
                            if rewindAnchorOpt = ValueSome interactionRecord then
                                rewindAnchorOpt <- ValueNone
                            elif rewindAnchorOpt.IsNone then
                                World.publishUnsorted
                                    { RewindAnchorOpt = ValueSome { WithOther = entity.EntityAddress; WorldTime = interactionRecord.WorldTime }
                                      RewindTime = remainingGameTime }
                                    (acatf Rewind.Event interactionRecord.WithOther)
                                    entity
                                    world
                        if rewindAnchorOpt.IsNone then
                            remainingGameTime <- remainingGameTime - rewindRecord.TimePassed
                            if remainingGameTime <= GameTime.zero then
                                continued <- false
                                entity.SetTimeSinceLastHistoryEntry -remainingGameTime world
                    | [] -> continued <- false // TODO: rewinding past object spawning?
                entity.SetXtensionPropertyWithoutEvent (nameof Entity.AwakeTimeStamp) world.UpdateTime world // otherwise it would sleep
                entity.SetRewindHistoryActiveInternal false world
                for KeyValue (property, value) in rewindProperties do
                    let prop = entity.TryGetProperty property world |> Option.get
                    entity.SetProperty property { prop with PropertyValue = System.ComponentModel.TypeDescriptor.GetConverter(prop.PropertyType).ConvertFrom value } world
                synchronizeBody entity world
                entity.SetRewindHistoryActiveInternal true world
            Cascade) entity.RewindEvent entity (nameof RewindableFacet) world

        // sense rewind preview event
        World.sense (fun event world ->
            let entity = event.Subscriber
            let mutable remainingGameTime = event.Data.RewindTime
            let mutable rewindAnchorOpt = event.Data.RewindAnchorOpt
            if rewindAnchorOpt.IsNone then remainingGameTime <- remainingGameTime - entity.GetTimeSinceLastHistoryEntry world
            if remainingGameTime > GameTime.zero then
                // render from history for the frame
                let mutable continued = true
                let mutable rewindHistory = entity.GetRewindHistory world
                let restoreTransform = entity.GetTransform world
                let restoreXtensions = Dictionary ()
                let mutable rewindTransform = restoreTransform
                while continued do
                    match rewindHistory with
                    | rewindRecord :: rest ->
                        rewindHistory <- rest
                        match rewindRecord.PropertyName with
                        // Intrinsic property list sync point 2/2
                        | nameof Entity.Position -> rewindTransform.Position <- symbolToValue rewindRecord.PreviousValue
                        | nameof Entity.Rotation -> rewindTransform.Rotation <- symbolToValue rewindRecord.PreviousValue
                        | nameof Entity.Size -> rewindTransform.Size <- symbolToValue rewindRecord.PreviousValue
                        | nameof Entity.Scale -> rewindTransform.Scale <- symbolToValue rewindRecord.PreviousValue
                        | nameof Entity.Elevation -> rewindTransform.Elevation <- symbolToValue rewindRecord.PreviousValue
                        | Rewind.EventName ->
                            let interactionRecord = rewindRecord.PreviousValue |> symbolToValue<InteractionRecord>
                            if rewindAnchorOpt = ValueSome interactionRecord then
                                rewindAnchorOpt <- ValueNone
                            elif rewindAnchorOpt.IsNone then
                                World.publishUnsorted
                                    { RewindAnchorOpt = ValueSome { WithOther = entity.EntityAddress; WorldTime = interactionRecord.WorldTime }
                                      RewindTime = remainingGameTime }
                                    (acatf Rewind.PreviewEvent interactionRecord.WithOther)
                                    entity
                                    world
                        | _ ->
                            if not (restoreXtensions.ContainsKey rewindRecord.PropertyName) then
                                restoreXtensions[rewindRecord.PropertyName] <- entity.TryGetProperty rewindRecord.PropertyName world |> Option.get
                            let prop = restoreXtensions[rewindRecord.PropertyName]
                            World.setEntityXtensionPropertyWithoutEvent rewindRecord.PropertyName
                                { prop with PropertyValue = System.ComponentModel.TypeDescriptor.GetConverter(prop.PropertyType).ConvertFrom rewindRecord.PreviousValue }
                                entity world |> ignore<struct (bool * bool)>

                        if rewindAnchorOpt.IsNone && rewindRecord.TimePassed > GameTime.zero then // only render right before history has a time skip
                            remainingGameTime <- remainingGameTime - rewindRecord.TimePassed
                            continued <- remainingGameTime > GameTime.zero
                            entity.SetTransformByRefWithoutEvent (&rewindTransform, world)
                            let color = entity.GetColor world
                            entity.SetXtensionPropertyWithoutEvent (nameof Entity.Color) (color.MapA ((*) 0.2f)) world

                            // World.renderEntity using NormalPass but omit this facet
                            for facet in entity.GetFacets world do
                                if facet.GetType () <> typeof<RewindableFacet> then
                                    facet.Render (NormalPass, entity, world)
                            (entity.GetDispatcher world).Render (NormalPass, entity, world)

                            entity.SetXtensionPropertyWithoutEvent (nameof Entity.Color) color world
                    | [] -> continued <- false // TODO: rewinding past object spawning?
                for KeyValue (name, xtension) in restoreXtensions do
                    World.setEntityXtensionPropertyWithoutEvent name xtension entity world |> ignore<struct (bool * bool)>
                entity.SetTransformByRefWithoutEvent (&restoreTransform, world)
            Cascade) entity.RewindPreviewEvent entity (nameof RewindableFacet) world
          
        // sense change events - property assignments
        let senseChangeEvent (changeProperty : Lens<'a, Entity>) =
            World.sense (fun event world ->
                let entity = event.Subscriber
                if entity.GetRewindHistoryActiveInternal world then
                    entity.RewindHistory.Map (List.cons { PropertyName = changeProperty.Name; PreviousValue = valueToSymbol event.Data.Previous; TimePassed = entity.GetTimeSinceLastHistoryEntry world }) world
                    entity.SetTimeSinceLastHistoryEntry GameTime.zero world
                    match changeProperty.Name with
                    | nameof Entity.Position | nameof Entity.Rotation | nameof Entity.LinearVelocity | nameof Entity.AngularVelocity ->
                        synchronizeBody entity world
                    | _ -> ()
                Cascade) changeProperty.ChangeEvent entity (nameof RewindableFacet) world
        senseChangeEvent entity.LinearVelocity
        senseChangeEvent entity.AngularVelocity
        // Intrinsic property list sync point 1/2
        senseChangeEvent entity.Position
        senseChangeEvent entity.Rotation
        senseChangeEvent entity.Size
        senseChangeEvent entity.Scale

        // record interactions
        World.sense (fun event world ->
            let entity = event.Subscriber
            let collidedWith = event.Data.BodyShapeSeparatee.BodyId.BodySource :?> Entity
            if collidedWith.Has<RewindableFacet> world then
                entity.RewindHistory.Map (List.cons
                    { PropertyName = Rewind.EventName
                      PreviousValue = { WithOther = collidedWith.EntityAddress
                                        WorldTime = world.GameTime } |> valueToSymbol
                      TimePassed = entity.GetTimeSinceLastHistoryEntry world }) world
                entity.SetTimeSinceLastHistoryEntry GameTime.zero world
            Cascade) entity.BodySeparationExplicitEvent entity (nameof RewindableFacet) world

        // sense change events - body transform
        // ManualMotion receives the normal rigid body's transform and owns live entity/render synchronization.
        World.sense (fun event world ->
            let entity = event.Subscriber
            if entity.GetRewindHistoryActiveInternal world && (entity.GetRewindPreview world).IsNone then
                entity.RewindHistory.Map (fun rewindHistory ->
                    [{ PropertyName = nameof Entity.Position; PreviousValue = valueToSymbol <| entity.GetPosition world; TimePassed = GameTime.zero }
                     { PropertyName = nameof Entity.Rotation; PreviousValue = valueToSymbol <| entity.GetRotation world; TimePassed = GameTime.zero }
                     { PropertyName = nameof Entity.LinearVelocity; PreviousValue = valueToSymbol <| entity.GetLinearVelocity world; TimePassed = GameTime.zero }
                     { PropertyName = nameof Entity.AngularVelocity; PreviousValue = valueToSymbol <| entity.GetAngularVelocity world; TimePassed = entity.GetTimeSinceLastHistoryEntry world }
                     yield! rewindHistory]) world
                entity.SetTimeSinceLastHistoryEntry GameTime.zero world
            entity.Physics event.Data.BodyCenter event.Data.BodyRotation event.Data.BodyLinearVelocity event.Data.BodyAngularVelocity world
            Cascade) entity.BodyTransformEvent entity (nameof RewindableFacet) world
            
        // Stop the existing body during preview; historical renders never register separate physics bodies.
        World.sense (fun event world ->
            let entity : Entity = event.Subscriber
            let notRewinding = event.Data.Value :?> GameTime option |> Option.isNone
            entity.SetBodyEnabled notRewinding world
            entity.SetPresence (if notRewinding then Exterior else Omnipresent) world
            Cascade) entity.RewindPreview.ChangeEvent entity (nameof RewindableFacet) world

        // increment time since last history entry (use group Update because entity Update only runs when entity is on screen - we don't skip rewind steps when entity is off screen!)
        World.sense (fun event world ->
            let entity : Entity = event.Subscriber
            if (entity.GetRewindPreview world).IsNone then
                entity.TimeSinceLastHistoryEntry.Map ((+) world.GameDelta) world
            Cascade) entity.Group.UpdateEvent entity (nameof RewindableFacet) world

    override _.Render (_, entity, world) =
        match entity.GetRewindPreview world with
        | Some rewindPreview ->
            World.publish { RewindAnchorOpt = ValueNone; RewindTime = rewindPreview } entity.RewindPreviewEvent entity world
        | None -> ()