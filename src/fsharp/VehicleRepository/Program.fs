open System

open VehicleRepository
open VehicleRepository.App
open VehicleRepository.Messaging

let readCommand () =
    printf "> "
    Console.ReadLine().Trim().ToLowerInvariant()

[<EntryPoint>]
let main _ =

    //TODO read from settings
    let config = {
       ConnectionString = """Server=(LocalDB)\messaging;Initial Catalog=messaging_samples;Persist Security Info=False;Integrated security=False;User ID=messaging;Password=Vo60&8cV7erE;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"""
       Messaging = {
           Host = "localhost"
           Port = 5672
           VHost = "/"
           Username = "guest"
           Password = "guest"
       }
    }
    
    //TODO App.create instead?

    use rabbitConnection = RabbiMq.connect config.Messaging
    use eventPublisher = RabbiMq.createPublisher rabbitConnection ()
    
    let mutable cmd = ""
    
    while cmd <> "exit" do
        cmd <- readCommand ()
        try
            match cmd with
            | "add" -> App.addRandomVehicle config eventPublisher () |> printfn "%A"
            | _ -> ()
        with
        | ex -> printfn "Error: %A" ex
    
    
    0 
