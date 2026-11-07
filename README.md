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
	[MessageId] [nvarchar](36) NOT NULL,
	[AggregateId] [nvarchar](36) NOT NULL,
	[AggregateName] [nvarchar](250) NOT NULL,
	[AggregateType] [nvarchar](250) NOT NULL,
	[EventType] [nvarchar](250) NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[Metadata] [varbinary](max) NOT NULL,
	[Version] [int] NOT NULL,
	[CommitPosition] [bigint] IDENTITY(1,1) NOT NULL,
	[ledger_start_transaction_id] [bigint] GENERATED ALWAYS AS transaction_id START HIDDEN NOT NULL,
	[ledger_start_sequence_number] [bigint] GENERATED ALWAYS AS sequence_number START HIDDEN NOT NULL,
 CONSTRAINT [PK_EventStore] PRIMARY KEY CLUSTERED 
(
	[MessageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
WITH
(
LEDGER = ON (APPEND_ONLY = ON, LEDGER_VIEW = [dbo].[EventStore_Ledger] (TRANSACTION_ID_COLUMN_NAME = [ledger_transaction_id], SEQUENCE_NUMBER_COLUMN_NAME = [ledger_sequence_number], OPERATION_TYPE_COLUMN_NAME = [ledger_operation_type], OPERATION_TYPE_DESC_COLUMN_NAME = [ledger_operation_type_desc]))
)
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