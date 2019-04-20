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
    * after done - show solution and discuss various aspects


### Failures in Publishing

* Acknowledgements and Confirms
    * Ack - allows the client to confirm to the server that he processed the message (received and acted upon)
    * Confirm - server confirms to client that he processed the message            
    * guarantees *at-least-once* delivery
    * https://www.rabbitmq.com/confirms.html#when

* when designing, THINK ABOUT: for every new feature, communication channel - what happens if the message gets lost? What if it gets delivered late? what if it is delivered more than once?
    * and design accordingly     

#### Demo - Publishing Trouble

* TODO where and how to show

' `mandatory` attribute + basic.return https://www.rabbitmq.com/amqp-0-9-1-quickref.html
        - show what happens if there's nowhere to route (basic return)
        - show what happens if there's no exchange (channel disconnect! but async!)

' "Lost Send" - message not sent even when transaction is commited
' "Premature Send" - sending message before transaction is commited
' broker stops transaction - if unavailable, this is a reason for rollback 

#### Exercise 2 - Outbox

* publisher confirms
    * https://www.rabbitmq.com/confirms.html#when-publishes-are-confirmed
    * for unroutable - confirmed after basic return
    * for routable - accepted by all queues. durable messages persisted to disk (on all mirrors)
* stop broker so everyone can fill their outboxes
* start broker and see recovery

#### Exercise 3 - Message Consumption

* show what will be sent
* show IRoutingTable - in command producer is necessary, show why

#### Message Consumption

* acknowledge - https://www.rabbitmq.com/confirms.html - ACK needs to be on the same channel where the message was received, because of the delivery tag
    * ack, nack (non standard, allows multiple reject), reject. multiple - all tags up to specified number
* consumer - QOS, prefetch count https://www.rabbitmq.com/consumer-prefetch.html (only with manual acknowledgement)        
    * Values in the 100 through 300 range usually offer optimal throughput and do not run significant risk of overwhelming consumers. 
* mention idempotency
    * if the message is sent again, RabbitMQ sets the `redelivered` flag. 
        * The consumer may have seen the message before (or not - it might have been lost in transit on the first try)
        * if `redelivered` is false, then it's not a duplicate (for sure? what if it's duplicate on send?)

#### Exercise 4 - Retry

* transient vs nontransient failures. when to retry and when not.
* options. retry with delay - short discussion, or just info, do not go that far
* https://www.rabbitmq.com/confirms.html watch out for requeue/redelivery loop. consumers should track number of redeliveries    
* nontransient failure - do not retry, reject


## Sources
