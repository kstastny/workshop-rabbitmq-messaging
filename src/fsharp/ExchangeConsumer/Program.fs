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
           Host = "stastnyk"
           Port = 5672
           VHost = "/"
           Username = "workshop"
           Password = "inasproboate"
       }
    
    App.run config endpoints
    0 
