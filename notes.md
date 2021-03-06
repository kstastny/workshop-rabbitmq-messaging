# RabbitMQ Workshop - Advanced Messaging Patterns

We will investigate messaging patterns and problems that go beyond the basics.
I will present several messaging problems and together we will discuss possible solutions, their advantages and drawbacks. We can implement select patterns.
I will select the specific patterns based on interest and applicability, if you have anything you would like to discuss, please let me know. Main focus of the workshop should be on reliability scenarios and monitoring but other areas may be included.
We will use RabbitMQ as a message broker, with examples in .NET languages.


## Presentation notes

 * for each messaging pattern
    * show motivation, what problem does it solve
    * explain how that part works and explain terms
    * show examples
    * show demo
 * for each exercise
    * show what needs to be implemented and the environment around
    * after done - show solution and discuss various aspects

* talk, demos, exercises are basics for discussion, do not be afraid to ask or point out errors. 
* I want to use this workshop as an opportunity to exchange experience and knowledge, discuss problems and find possible solutions
* the examples are just that, examples. Often duplicate code is kept for sake of clarity and easy reference

### Exercise 1 - Message Publishing

* Task: send a message to everyone when new orc warrior is born 
* show around the application
* Console.WriteLine is meant as CLI, logger is just logger
* Solution
    * notice one connection, mention recovery
    * lock when sending - channel is not thread safe
    * set content type
    * set message type
    * set sender in custom header - helps in monitoring
* Command - mandatory; event - not mandatory


### Declaring Exchanges and Queues

* my opinion - declaring on both makes sure that both sides' expectations are correct
* but - needs to correctly sync and some decisions are not meant to be done by that side
* earlier I used to do on both sides. Now I'd say either define queues completely elsewhere, or for commands and RPC - consumer only. For events - publisher only
* very much depends on the design of the queues and necessary flexibility


### Failures in Publishing

* Acknowledgements and Confirms
    * Ack - allows the client to confirm to the server that he processed the message (received and acted upon)
    * Confirm - server confirms to client that he processed the message            
    * guarantees *at-least-once* delivery
    * https://www.rabbitmq.com/confirms.html#when

* when designing, THINK ABOUT: for every new feature, communication channel - what happens if the message gets lost? What if it gets delivered late? what if it is delivered more than once?
    * and design accordingly   

* publisher confirms
    * https://www.rabbitmq.com/confirms.html#when-publishes-are-confirmed
    * for unroutable - confirmed after basic return
    * for routable - accepted by all queues. durable messages persisted to disk (on all mirrors)


#### Demo - Publishing Trouble

* show Lost Send, Premature Send, use default MessagePublisher

' `mandatory` attribute + basic.return https://www.rabbitmq.com/amqp-0-9-1-quickref.html
    - show what happens if there's nowhere to route (basic return)
    - show what happens if there's no exchange (channel disconnect! but async! but still confirms :( )

' "Lost Send" - message not sent even when transaction is commited
' "Premature Send" - sending message before transaction is commited
' broker stops transaction - if unavailable, this is a reason for rollback 

#### Exercise 2 - Outbox

* show how it should work first - TODO draw and image (just draw on whiteboard)
* Task: send a message reliably to everyone when new orc warrior is born 
* demo - stop broker, check recovery (after restart of app :))


* stop broker so everyone can fill their outboxes
* start broker and see recovery

#### Exercise 3 - Message Consumption

* show what will be sent
* show IRoutingTable - in command producer is necessary, show why
* show what happens when autoAck is true and we try to ACK

#### Message Consumption

* acknowledge - https://www.rabbitmq.com/confirms.html - ACK needs to be on the same channel where the message was received, because of the delivery tag
    * ack, nack (non standard, allows multiple reject), reject. multiple - all tags up to specified number
* consumer - QOS, prefetch count https://www.rabbitmq.com/consumer-prefetch.html (only with manual acknowledgement)        
    * Values in the 100 through 300 range usually offer optimal throughput and do not run significant risk of overwhelming consumers. 
    * “The goal is to keep the consumers saturated with work, but to minimise the client's buffer size so that more messages stay in Rabbit's queue and are thus available for new consumers or to just be sent out to consumers as they become free.”
* mention idempotency
    * if the message is sent again, RabbitMQ sets the `redelivered` flag. 
        * The consumer may have seen the message before (or not - it might have been lost in transit on the first try)
        * if `redelivered` is false, then it's not a duplicate (for sure? what if it's duplicate on send?)

#### Exercise 4 - Retry

* transient vs nontransient failures. when to retry and when not.
* options. retry with delay - short discussion, or just info, do not go that far
* https://www.rabbitmq.com/confirms.html watch out for requeue/redelivery loop. consumers should track number of redeliveries    
* nontransient failure - do not retry, reject
* immediate retry - puts message at the head of the queue, will be processed again. we need some delay

options, aka https://jack-vanlightly.com/blog/2017/3/24/rabbitmq-delayed-retry-approaches-that-work
    * Simple Wait Exchange and Queue Pattern - only works if the retry time is always the same (messages with longer TTL will block those with shorter!)
    * multiple wait queues per app
    * multiple wait queues - shared
    * NServiceBus advanced https://jack-vanlightly.com/blog/2017/3/19/reliability-default-retries-nservicebus-with-rabbitmq-part-5

TODO draw an image of solution
Retry - original message id, use new because of duplicate detection in Rabbit? example with DLX. show existing plugin
talk about retry with delay, deduplication

#### Exercise 5 - ???

* if time, we can do/start one of the following
    * WireTap
    * RPC + Smart Proxy
    * Message History

### Discussion

* ask for feedback
* anything else they would be interested in?


## Sources

### RabbitMQ

* https://www.rabbitmq.com/queues.html
    - queue properties - name, durable, exclusive, autodelete, arguments (TTL, limits, mirroring, max priorities, consumer priorities, ...)
* https://www.rabbitmq.com/firehose.html + tracing plugin (UI).
* https://www.rabbitmq.com/ae.html - alternate exchange
* https://www.rabbitmq.com/consumer-priority.html high priority consumers first. not sure why
* https://www.rabbitmq.com/production-checklist.html
* https://www.rabbitmq.com/priority.html priority queues
* https://www.rabbitmq.com/parameters.html#policies 
* https://asafdav2.github.io/2017/rabbit-mq-persistentcy-vs-durability/         

https://www.cloudamqp.com/blog/2017-12-29-part1-rabbitmq-best-practice.html
  how to set correct prefetch - different than 100-300, depends on 
    * number of consumers and processing time
    * few consumers with quick processing - high prefetch
    * many consumers with short processing - lower prefetch than above
    * many consumers and/or long processing time - prefetch 1, distribute messages evenly (not sure about this, really depends on how much concurrency is possible imo)

https://www.cloudamqp.com/blog/2018-01-08-part2-rabbitmq-best-practice-for-high-performance.html
https://www.cloudamqp.com/blog/2018-01-09-part3-rabbitmq-best-practice-for-high-availability.html
https://www.cloudamqp.com/blog/2018-01-19-part4-rabbitmq-13-common-errors.html

https://stackoverflow.com/questions/25070042/rabbitmq-consuming-and-publishing-on-same-channel 
https://www.rabbitmq.com/api-guide.html#concurrency

### Retry and Delayed Delivery

* retry and delayed delivery
    * https://jack-vanlightly.com/blog/2017/3/24/rabbitmq-delayed-retry-approaches-that-work
            * https://jack-vanlightly.com/blog/2017/3/19/reliability-default-retries-nservicebus-with-rabbitmq-part-5 - 
            * https://stackoverflow.com/questions/23158310/how-do-i-set-a-number-of-retry-attempts-in-rabbitmq ! approaches
    * https://github.com/rabbitmq/rabbitmq-delayed-message-exchange

    * https://gagnechris.wordpress.com/2015/09/19/easy-retries-with-rabbitmq/ - but ignores different delay times

  * delayed delivery
     * Azure Service Bus - has support, see https://amido.com/blog/azure-service-bus-how-to-delay-a-message-being-sent-to-the-queue/
     * NServiceBus https://docs.particular.net/transports/rabbitmq/delayed-delivery


### Message Buses

 * https://particular.net/nservicebus
 * https://masstransit-project.com/
 * https://www.goparamore.io/

### Other important knowledge

https://blogs.msdn.microsoft.com/seteplia/2018//the-danger-of-taskcompletionsourcet-class/  - SetResult calls continuations asynchronously by default
