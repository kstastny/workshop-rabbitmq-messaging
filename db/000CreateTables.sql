/*

use master
GO 

CREATE LOGIN [messaging] WITH PASSWORD = 'Vo60&8cV7erE';
CREATE USER [messaging] FOR LOGIN [messaging];
exec sp_addrolemember 'db_owner', 'messaging'

*/

 
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


CREATE TABLE [dbo].[Vehicles](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[RegistrationPlate] [varchar](10) NOT NULL,
	[Created] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Vehicles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


ALTER TABLE [dbo].[Vehicles] ADD CONSTRAINT FD_Vehicles_Created DEFAULT GETUTCDATE() FOR Created