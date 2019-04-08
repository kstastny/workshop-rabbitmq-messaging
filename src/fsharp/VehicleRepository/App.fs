module VehicleRepository.App

open System
open System.Transactions

open FSharp.Control.Rop

open VehicleRepository.Db
open VehicleRepository.Domain
open VehicleRepository.Messaging

type AppError =
    | DataError of DataError
    

type Configuration = {
    ConnectionString: string
    Messaging: Messaging.Configuration
    DbFailureRate: float
    MessagingFailureRate: float
}

let randomFailure name (rnd: Random) rate x =
    if rnd.NextDouble () < rate then
        failwithf "Random failure at %s" name
    else
        x

let random = Random()    
    
    
//TODO alternative examples
    // save, commit, publish - might save and not publish - lost send
    // save, publish, commit - might publish and not save. or both fail if Messaging write fails - current example. messaging stops business operation
    // save, publish with fire and forget (aka eat exception, not check result code...), commit - premature send
let inDbContext (conf: Configuration) f x =
    // http://fsprojects.github.io/FSharp.Data.SqlClient/transactions.html
    use tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
    
    let ctx = Db.createContext conf.ConnectionString
    
    let fResult = f ctx x
    match fResult with
    | Ok _ ->
        randomFailure "DB" random conf.DbFailureRate ()
        ctx.SubmitUpdates ()
        tran.Complete()
    | Error _ -> ()
    fResult
    
    

    
    
let addRandomVehicle conf (publisher: IPublisher) () =
    Generator.generateVehicle ()
    |> inDbContext conf (fun ctx vehicle ->
        
        Vehicles.createVehicle ctx vehicle
        <!> VehicleAddedEvent
        <!> (randomFailure "messaging" random conf.MessagingFailureRate >> publisher.PublishEvent)
        <!> (fun _ -> vehicle)
        //>>= (fun _ -> DataError.DuplicateKey "whatever" |> Error)
        )
    
    
let readCommand () =
    printf "> "
    Console.ReadLine().Trim().ToLowerInvariant()
    
    
let run (config: Configuration) =
    
    use rabbitConnection = RabbiMq.connect config.Messaging
    
    //TODO alternative publisher - Outbox
    use eventPublisher = RabbiMq.createPublisher rabbitConnection ()
    
    let mutable cmd = ""
    
    while cmd <> "exit" do
        cmd <- readCommand ()
        try
            match cmd with
            | "add" -> addRandomVehicle config eventPublisher () |> printfn "%A"
            | _ -> ()
        with
        | ex -> printfn "Error: %s" ex.Message
