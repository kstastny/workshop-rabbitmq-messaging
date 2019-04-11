namespace VehicleRepository.Messaging

open System

open System.Collections.Generic
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events


open VehicleRepository
open VehicleRepository.Domain

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
    abstract PublishEvent: VehicleEvent -> unit



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

        factory.CreateConnection (sprintf "vehicle-repository @ %s" Environment.MachineName)


    let private createChannel (conn: IConnection) =
        let channel = conn.CreateModel()
        channel.BasicReturn |> Event.add (fun e -> printfn "Basic return - %i: %s" e.ReplyCode e.ReplyText)
        channel


    
    type VehicleEventDto = {
        Type: string
        VehicleId: Guid
        Name: string
        RegistrationPlate: string
    }
    
    let toDto = function
        | VehicleAddedEvent v -> {
            Type = "Added"
            VehicleId = v.Id
            Name = v.Name
            RegistrationPlate = v.RegistrationPlate
        }
    
    
    let private publishEvent (channel: IModel) (addr: Address) (evnt: VehicleEvent) =
        let body =
            evnt
            |> toDto
            |> Serialization.serialize
            |> (fun x -> printfn "Sending %A" x; x)
            |> Encoding.UTF8.GetBytes
        

        let requestProperties = channel.CreateBasicProperties();
        //TODO set request properties - TTL, sender identifier etc.
        requestProperties.Headers <- Dictionary<string, obj>()


        channel.BasicPublish(
                addr.ExchangeName,
                addr.RoutingKey,
                body = body,
                basicProperties = requestProperties
                )


    let createPublisher conn () =

        //TODO queue and exchange declarations should be moved elsewhere
        let vehiclesExchange = "vehicle-repository.events"
        let vehiclesQueue = "vehicle-repository.events.queue" //TODO remove, part of CONSUMER!
        let routingKey = "vehicle-event"

        let channel = createChannel conn
        channel.ExchangeDeclare (
                vehiclesExchange,
                "direct",
                //NOTE: normally the exchange would be durable
                durable = false,
                autoDelete = false);


        channel.QueueDeclare(vehiclesQueue, false, false, false, null) |> ignore
        channel.QueueBind(vehiclesQueue, vehiclesExchange, routingKey)

        //NOTE: could be determined dynamically, based on settings, based on sent event etc.
        let address = { ExchangeName = vehiclesExchange; RoutingKey = routingKey }

        { new IPublisher
             with
                member this.PublishEvent evnt = publishEvent channel address evnt
          interface IDisposable
            with
                member this.Dispose() = channel.Dispose()
             }
