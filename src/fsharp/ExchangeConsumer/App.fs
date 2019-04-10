module ExchangeConsumer.App

open System

open System
open System.Text
open ExchangeConsumer.Messaging


let run (config: Configuration) endpoints =
    
    printfn "Connecting to RabbitMQ server at %s:%i" config.Host config.Port
    use conn = RabbiMq.connect config
    use channel = RabbiMq.createChannel conn
    
    //start consuming
    RabbiMq.consume
        (fun x ->
            printfn "Received from exchange '%s', routing key '%s':%s %A"
                x.Exchange
                x.RoutingKey
                Environment.NewLine
                (x.Body |> Encoding.UTF8.GetString)
        )
        channel
        endpoints
    
    printfn "Press Enter to exit"
    Console.ReadLine() |> ignore

    