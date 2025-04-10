# Muflone.Persistence.Sql
Muflone extension to implement IRepository using Microsoft SQL Server.

## Use
dotnet add package Muflone.Persistence.Sql --version 8.0.0

## SQL
You need to create these tables in your SQL Database:

### EventStore
```
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EventStore](
	[MessageId] [nvarchar](50) NOT NULL,
	[AggregateId] [nvarchar](50) NOT NULL,
	[AggregateName] [nvarchar](250) NOT NULL,
	[AggregateType] [nvarchar](100) NOT NULL,
	[EventType] [nvarchar](100) NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[Metadata] [varbinary](max) NOT NULL,
	[Version] [int] NOT NULL,
	[CommitPosition] [bigint] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_EventStore] PRIMARY KEY CLUSTERED 
(
	[MessageId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```

### EventStorePosition
```
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EventStorePosition](
	[Id] [int] NOT NULL,
	[CommitPosition] [bigint] NOT NULL,
	[PreparePosition] [bigint] NOT NULL,
 CONSTRAINT [PK_EventStorePosition_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
```

## Azure
You need these resources on your Azure Subscription

> - Azure EventHub  
> - Azure ServiceBus  
> - Azure StoreAccount  