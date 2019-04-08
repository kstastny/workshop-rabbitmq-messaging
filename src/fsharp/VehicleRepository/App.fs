module VehicleRepository.App

open System.Transactions

open FSharp.Control.Rop

open VehicleRepository.Db
open VehicleRepository.Domain
open VehicleRepository.Messaging

type Configuration = {
    ConnectionString: string
    Messaging: Messaging.Configuration
}

let inDbContext (conf: Configuration) f x =
    // http://fsprojects.github.io/FSharp.Data.SqlClient/transactions.html
    use tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
    
    let ctx = Db.createContext conf.ConnectionString
    
    let fResult = f ctx x
    match fResult with
    | Ok _ ->
        ctx.SubmitUpdates ()
        tran.Complete()
    | Error _ -> ()
    fResult
    
    
    
//TODO inject inDbContext from outside (enriches function with ctx)    
let addRandomVehicle conf (publisher: IPublisher) () =
    Generator.generateVehicle ()
    |> inDbContext conf (fun ctx vehicle ->
        
        Vehicles.createVehicle ctx vehicle
        <!> VehicleAddedEvent
        <!> publisher.PublishEvent
        <!> (fun _ -> vehicle)
        //>>= (fun _ -> DataError.DuplicateKey "whatever" |> Error)
        )