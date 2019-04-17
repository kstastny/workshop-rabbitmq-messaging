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
	[ContentType] VARCHAR(255) NOT NULL,
	-- note: in real life, this could be varbinary
	[Body] NVARCHAR(MAX) NOT NULL,
	[SentDateTime] DATETIME2 NULL,
	CONSTRAINT [PK_Outbox_MessageId] PRIMARY KEY ([MessageId] ASC)
)


/****** Object:  Table [dbo].[Orcs]    Script Date: 17.04.2019 13:19:52 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Orcs](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Profession] [nvarchar](50) NOT NULL,
	[Born] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Orcs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Orcs] ADD CONSTRAINT FD_Orcs_Born DEFAULT GETUTCDATE() FOR Born