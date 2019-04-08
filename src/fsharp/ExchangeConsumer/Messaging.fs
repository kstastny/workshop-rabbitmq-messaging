module ExchangeConsumer.Messaging

open System

open System.Collections.Generic
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events

type Configuration = {
    Host: string
    Port: int
    VHost: string
    Username: string
    Password: string
 }

//TODO refactor helpers (RabbitMq.Core)

module RabbiMq =

    type private Address = {
        ExchangeName: string
        RoutingKey: string
    }


    let connect (config: Configuration) =
        let factory = new ConnectionFactory()
        factory.HostName <- config.Host
        factory.Port <- config.Port
        factory.VirtualHost <- config.VHost
        factory.UserName <- config.Username
        factory.Password <- config.Password

        factory.CreateConnection (sprintf "exchange-consumer @ %s" Environment.MachineName)


    let createChannel (conn: IConnection) =
        let channel = conn.CreateModel()
        channel.BasicReturn |> Event.add (fun e -> printfn "Basic return - %i: %s" e.ReplyCode e.ReplyText)
        channel
        
        
    let consume f (channel: IModel) exchangeAndKey =
        
        let consumer = EventingBasicConsumer(channel)
        consumer.Received |> Event.add f
        
        //create a queue for messages
        let queueName = channel.QueueDeclare("", durable = false, exclusive = true, autoDelete = false).QueueName;
        
        //bind the consumer to the queue. ignore returned consumer tag
        channel.BasicConsume(consumer, queueName, autoAck = true) |> ignore
        
        //and bind all the exchanges and routing keys to the queue
        exchangeAndKey
        |> List.iter (fun (ex, k) ->
            channel.QueueBind(queueName, ex, k) |> ignore)