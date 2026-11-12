# Muflone.Persistence.Sql
Muflone extension to implement IRepository using Microsoft SQL Server.

## Use
dotnet add package Muflone.Persistence.Sql --version 8.0.0

## SQL
The package needs two tables, `EventStore` and `EventStorePosition`. You can let the package
create them for you, or create them by hand.

### Automatic creation (opt-in)
Pass `ensureSchema: true` when registering the persistence. At startup the package runs an
idempotent bootstrapper that creates the tables **only if they do not already exist**, so it is
safe to run against a database that was provisioned another way.

```csharp
builder.Services.AddEventstoreSqlPersistence(builder.Configuration, ensureSchema: true);
```

It is **off by default** (`ensureSchema: false`): teams that manage their schema through their own
migration/deployment pipeline — or that run with least-privilege database accounts that cannot
issue DDL — keep full control. When enabled, the account in the connection string must have
permission to create tables (and ledger objects) in the `dbo` schema.

### Manual creation
Alternatively, create these tables in your SQL Database yourself:

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
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CommitPosition] [bigint] NOT NULL,
	[PreparePosition] [bigint] NOT NULL,
 CONSTRAINT [PK_EventStorePosition_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
```

### Migrating an existing database
Earlier versions created `EventStorePosition.Id` as a plain `[int] NOT NULL` (no `IDENTITY`).
The column is now expected to be `IDENTITY(1,1)`, and SQL Server cannot add `IDENTITY` to an
existing column in place — the table must be rebuilt.

If the table is empty (or you don't need to keep its single row), just drop and recreate it with
the DDL above:

```
DROP TABLE [dbo].[EventStorePosition];
GO
-- then run the EventStorePosition CREATE TABLE from the section above
```

If you need to preserve the existing row, copy it across with `IDENTITY_INSERT`:

```
EXEC sp_rename N'[dbo].[EventStorePosition]', N'EventStorePosition_old';
GO
-- run the EventStorePosition CREATE TABLE from the section above, then:
SET IDENTITY_INSERT [dbo].[EventStorePosition] ON;
INSERT INTO [dbo].[EventStorePosition] ([Id], [CommitPosition], [PreparePosition])
SELECT [Id], [CommitPosition], [PreparePosition] FROM [dbo].[EventStorePosition_old];
SET IDENTITY_INSERT [dbo].[EventStorePosition] OFF;
GO
DROP TABLE [dbo].[EventStorePosition_old];
GO
```

## Azure
You need these resources on your Azure Subscription

> - Azure EventHub  
> - Azure ServiceBus  
> - Azure StoreAccount  