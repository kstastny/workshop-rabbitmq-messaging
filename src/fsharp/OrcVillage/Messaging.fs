namespace OrcVillage.Messaging

open System

open System.Collections.Generic
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events


open OrcVillage
open OrcVillage.Domain

type Configuration = {
    Host: string
    Port: int
    VHost: string
    Username: string
    Password: string
 }


type IPublisher =
    inherit IDisposable

    //TODO return result instead
    abstract PublishEvent: OrcEvent -> unit



module RabbiMq =

    //TODO separate to "Core" and business related
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

        factory.CreateConnection (sprintf "fs-orc-village @ %s" Environment.MachineName)


    let private createChannel (conn: IConnection) =
        let channel = conn.CreateModel()
        channel.BasicReturn |> Event.add (fun e -> printfn "Basic return - %i: %s" e.ReplyCode e.ReplyText)
        channel


    
    type OrcEventDto = {
        Type: string
        EventType: string
        OrcId: Guid
        Name: string
        Profession: string
    }
    
    let fromDomain = function
        | Born x -> {
            Type = "event.orc"
            EventType  = "born"
            OrcId = x.Id
            Name = x.Name
            Profession = x.Profession
        }
    
    
    let private publishEvent connectionName (channel: IModel) (addr: Address) (evnt: OrcEvent) =
        let body =
            evnt
            |> fromDomain
            |> Serialization.serialize
            |> (fun x -> printfn "Sending %A" x; x)
            |> Encoding.UTF8.GetBytes
        

        let requestProperties = channel.CreateBasicProperties();
        requestProperties.Type <- "event.orc"
        
        requestProperties.Headers <- Dictionary<string, obj>()
        requestProperties.Headers.["x-sender"] <- connectionName
        


        channel.BasicPublish(
                addr.ExchangeName,
                addr.RoutingKey,
                body = body,
                basicProperties = requestProperties
                )


    let createPublisher conn () =

        //TODO declare exchanges and queues
        let eventExchange = "orcvillage.events"
        let routingKey = "orcevent"

        let channel = createChannel conn
//        channel.ExchangeDeclare (
//                eventExchange,
//                "direct",
//                //NOTE: normally the exchange would be durable
//                durable = false,
//                autoDelete = false);


//        channel.QueueDeclare(vehiclesQueue, false, false, false, null) |> ignore
//        channel.QueueBind(vehiclesQueue, eventExchange, routingKey)

        //TODO: should be determined dynamically, based on settings, based on sent event etc.
        let address = { ExchangeName = eventExchange; RoutingKey = routingKey }

        { new IPublisher
             with
                member this.PublishEvent evnt = publishEvent conn.ClientProvidedName channel address evnt
          interface IDisposable
            with
                member this.Dispose() = channel.Dispose()
             }
