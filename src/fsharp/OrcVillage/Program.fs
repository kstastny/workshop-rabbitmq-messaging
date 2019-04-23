open System

open OrcVillage
open OrcVillage.App

[<EntryPoint>]
let main _ =

    //TODO read from settings
    let config = {
       ConnectionString = """Server=(LocalDB)\messaging;Initial Catalog=messaging_samples;Persist Security Info=False;Integrated security=False;User ID=messaging;Password=Vo60&8cV7erE;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"""
       Messaging = {
           Host = "stastnyk"
           Port = 5672
           VHost = "/"
           Username = "workshop"
           Password = "inasproboate"
       }
       DbFailureRate = 0.0
       MessagingFailureRate = 0.0
    }
    
    App.run config
    
    0 
