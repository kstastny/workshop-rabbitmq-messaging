open System

open VehicleRepository
open VehicleRepository.App

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
       DbFailureRate = 0.5
       MessagingFailureRate = 0.5
    }
    
    App.run config
    
    0 
