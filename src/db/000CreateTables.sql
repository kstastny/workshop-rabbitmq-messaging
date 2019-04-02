 
 -- based on example in http://www.kamilgrzybek.com/design/the-outbox-pattern/
CREATE TABLE Outbox
(
	[MessageId] UNIQUEIDENTIFIER NOT NULL,
	[PublishDateTime] DATETIME2 NOT NULL,
	[Exchange] VARCHAR(255) NOT NULL,
	[RoutingKey] VARCHAR(255) NOT NULL,
	-- note: in real life, this could be varbinary
	[Body] NVARCHAR(MAX) NOT NULL,
	[SentDateTime] DATETIME2 NULL,
	CONSTRAINT [PK_Outbox_MessageId] PRIMARY KEY ([MessageId] ASC)
)