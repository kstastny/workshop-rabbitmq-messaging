open System


open ExchangeConsumer
open ExchangeConsumer.Messaging

// exchange * routing key
let endpoints = [
    ("vehicle-repository.events", "vehicle-event")
]

[<EntryPoint>]
let main argv =
    
    //TODO read from settings
    let config = {
           Host = "localhost"
           Port = 5672
           VHost = "/"
           Username = "guest"
           Password = "guest"
       }
    
    App.run config endpoints
    0 
