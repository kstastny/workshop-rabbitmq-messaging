module ExchangeConsumer.App

open System

open System
open System.Text
open ExchangeConsumer.Messaging


let run config endpoints =
    
    use conn = RabbiMq.connect config
    use channel = RabbiMq.createChannel conn
    
    //start consuming
    RabbiMq.consume
        (fun x ->
            printfn "Received from exchange '%s', routing key '%s':%s %A"
                Environment.NewLine
                x.Exchange
                x.RoutingKey
                (x.Body |> Encoding.UTF8.GetString)
        )
        channel
        endpoints
    
    printfn "Press Enter to exit"
    Console.ReadLine() |> ignore

    