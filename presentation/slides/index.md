- title : Advanced Messaging Patterns
- description : Support presentation for Advanced Messaging workshop
- author : Karel Šťastný
- theme : night 
- transition : none

***

# Advanced Messaging Patterns

TODO setup instructions in readme.md

***

## Outline

TODO

- plus info that the workshop will be a little bit different than planned, more deeply going into the basics, not much into patterns 
    

***

### Review of Basics

TODO *Command*, *Query*, *Event* (all in CQRS sense) and related queue patterns (images)

***

> Exercise 1 - Warmup - Message Publishing

TODO prepare templates
 - DB has to be setup
 - TODO setup template solution, including serializer, DTOs, DB access - CSharp - exercise1 - just gutted final solution
 - TODO setup template solution, including serializer, DTOs, DB access - FSharp
 - point of the exercise is to connect ot Rabbit and send a message to specified exchange when new ORC WARRIOR is born. Including the "sender" header
        proof: listener in presenters computer

***

### Publishing Messages

TODO important stuff when publishing - create slides
    * info - one connection per app. multiple channels (separate for producer and consumer, currently I would say one per "logical unit")    
    * declare exchange, queue - could be on both sides, I would recommend creation on consumer side only. Producer does not need to care about exchange, queues and such. OTH maybe it depends - publish-subscribe producer or admin creates, query and command consumer or admin creates. 
    * `mandatory` attribute + basic.return https://www.rabbitmq.com/amqp-0-9-1-quickref.html
            - show what happens if there's nowhere to route (basic return)
            - show what happens if there's no exchange (channel disconnect! but async!)
    * basicProperties.Type - type of message, e.g. specific event. might help with deserialization or deciding how to handle the message
 
 - definovat problém, motivaci k řešení. Ukázku řešení, výhody, nevýhody. Příklad - implementace
        toto vede k Outboxu

***

### Failures in Publishing

 * TODO prepare slides and short talk
 * TODO demo - send to nonexistent queue

     * tell something about it - what can go wrong
        * problems
            * network fail, firewall interrupts idle connection
            * broker failure
            * client application failure
            * logic errors in client application cause connection or channel closing
        * Connection Failure
            * `IConnection.ConnectionShutdown`, `IModel.ModelShutdown` for reconnect
            * Producer should resend the message (possibility of duplication)
        * Acknowledgements and Confirms
            * Ack - allows the client to confirm to the server that he processed the message (received and acted upon)
            * Confirm - server confirms to client that he processed the message            
            * guarantees *at-least-once* delivery
            * https://www.rabbitmq.com/confirms.html#when
        * heartbeat - detects dead TCP connections, see https://www.rabbitmq.com/heartbeats.html
        * durable messages - to survive broker restart (queue and message have to be durable)            
        * mandatory flag - to ensure that message has been routed
        * consumer handling - if the message is sent again, RabbitMQ sets the `redelivered` flag. 
            * The consumer may have seen the message before (or not - it might have been lost in transit on the first try)
            * if `redelivered` is false, then it's not a duplicate (for sure? what if it's duplicate on send?)
        * https://www.rabbitmq.com/reliability.html
        * every new feature, communication channel - what happens if the message gets lost? What if it gets delivered late? what if it is delivered more than once?
                * and design accordingly 

***

### ???

 - zminit distribuovane  transakce, two phase commit

    * how to fix the problems when sending data (and when this can be needed)
        * Command, Query - not necessary, the error can be displayed immediately and probably should (imo)
        * Event - necessary when we absolutely have to inform the others (architectural decistion - might not be needed when just clearing caches that expire anyway)
            * especially for Integration Events
                * Integration Event - informs about something that happened in a bounded context that may be of interest to other bounded contexts 
                    * other words: event that is used to synchronize information about domain state between different microservices (if one service = one bounded context)
                * see https://medium.com/@arleypadua/domain-events-vs-integration-events-5eb29a34fdbc
                * and https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/integration-event-based-microservice-communications#integration-events

***                 

###  Outbox     

TODO prepare slides

DONE publisher confirms - ASI POTŘEBA! jinak jde odeslat a pokud neexistuje exchange tak spadne. Ukázat

show the problematic example first (vehicle repository, use case of adding new vehicle, where it can fail - all three options)
        > simulates the reliability of distributed transactions without requiring use of the Distributed Transaction Coordinator (DTC).
            https://docs.particular.net/nservicebus/outbox/
                seems a bit different to me?
        * explain the problem (example that can fail)            
        * avoids the "Lost Send"
        * can minimize "Premature Send" (for both see http://gistlabs.com/2014/05/the-outbox/)
        * atomicity of business operation (operations *all* occur or *nothing* occurs)
        * possible with whatever queueing technology
        * alternatives
            * transactional queue (MSMQ) - not recommended today, legacy (two phase commit?)
                * see https://en.wikipedia.org/wiki/Two-phase_commit_protocol (problems: blocking)
            * transaction log mining - https://microservices.io/patterns/data/transaction-log-tailing.html
            * full event sourcing
            * see https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/architect-microservice-container-applications/asynchronous-message-based-communication
        * Outbox sources
            * http://www.kamilgrzybek.com/design/the-outbox-pattern/ nice description
            * http://gistlabs.com/2014/05/the-outbox/ small note
            * https://microservices.io/patterns/data/application-events.html high level desc, not much
            * https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/architect-microservice-container-applications/asynchronous-message-based-communication
                * alternatives
            * https://jimmybogard.com/refactoring-towards-resilience-evaluating-coupling/        
            * TODO maybe https://ronanmoriarty.com/tag/outbox-pattern/ (not working atm)        
    * Outbox in NServiceBus - https://docs.particular.net/nservicebus/outbox/ (Inbox?) - just mention

TODO demo - stop broker, see recovery



***

> Exercise 2 - Outbox

DONE implement example in Csharp
TODO implement example in FSharp
TODO prepare template in CSharp
TODO prepare template in FSharp

*** 

> Exercise 3 - Message Consumption

 - every tribe wants information about warriors in other tribes to see who is stronger
 - store data to DB, received in specified exchange
 - TODO command and event consumption differences. Competing Consumers
 - plus: execute commands by the supreme chieftain (Saruman?) - Exercise 3a, 3b

TODO implement example in CSharp - ONLY COMMAND HANDLER?
TODO implement example in FSharp - only command handler?
TODO prepare template in CSharp
TODO prepare template in FSharp

***

### Message Consumption

- definovat problém, motivaci k řešení. Ukázku řešení, výhody, nevýhody. Příklad - implementace
        toto vede k Retry, Retry with Delay, DLX, deduplication        

TODO talk - what to think about? considerations
    - TODO see Consumer in Rad
    - TODO manual vs automatic ack
    - TODO consuming events (own queue) vs commands
    * acknowledge - https://www.rabbitmq.com/confirms.html - ACK needs to be on the same channel where the message was received, because of the delivery tag
            * ack, nack (non standard, allows multiple reject), reject. multiple - all tags up to specified number
    * consumer - QOS, prefetch count https://www.rabbitmq.com/consumer-prefetch.html (only with manual acknowledgement)        
        * Values in the 100 through 300 range usually offer optimal throughput and do not run significant risk of overwhelming consumers. 

* Consumer side. how to differentiate?
    * Retry
        * options. retry with delay - short discussion, or just info, do not go that far
        * https://www.rabbitmq.com/confirms.html watch out for requeue/redelivery loop. consumers should track number of redeliveries
    * poison message, DLX - show how it works. exercise how? policy settings? DLX setting at queue?
        * TODO https://www.rabbitmq.com/dlx.html consumer that will write all the reasons, problems etc.
    * maybe - resequencer (delete before add) - need to create some data destroyer
            * info - how can the messages get out of order? example 

    * idempotent consumption
***

> Exercise 4 - Dead Letter Exchange

- will be sent by presenter

TODO prepare examples CSharp
TODO prepare examples FSharp
TODO prepare templates CSharp
TODO prepare templates FSharp

***


### Monitoring

* e.g. what the heck is happening in the system?
        * NOTE: we don't care about monitoring the messaging system itself 
* TODO prepare slides
    who is connected
    who is sending what messages + history
    who is processing what messages + history

    TODO pattern examples - WireTap? Message History? Smart Proxy? TODO TODO TODO!!! 
      MOŽNÁ - PÁR NAZNAČIT A UKÁZAT s tím že je proberem a zkusíme implementovat? uvidíme dle času
     workshop - patterny vždy stejným stylem, jako u messaging talku - definovat problém, motivaci k řešení. Ukázku řešení, výhody, nevýhody. Příklad - implementace
kde půjde, ukázat i implementovaný neřešený problém (např. nestabilitu, chybný výpočet a tak) a pak vyřešit

    ZKUSIT WIRE TAP + info, že tyhle patterny jsou důvod proč by exchange and durable queues měly být deklarovány mimo consumery, v nějaké další službě. teoreticky občas někdy asi :D